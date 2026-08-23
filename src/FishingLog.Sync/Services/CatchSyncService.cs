using FishingLog.Contracts.CatchDTOs;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using Microsoft.Extensions.Logging;

namespace FishingLog.Sync.Services;

/// <summary>
/// Two-way sync between the local SQLite database and the FishingLog REST API.
/// <para>
/// Conflict resolution: last-write-wins based on <c>LastModified</c> timestamp.
/// If a local record is dirty and newer than the server version, local wins.
/// If the server version is newer, the server wins.
/// </para>
/// </summary>
public class CatchSyncService : ICatchSyncService
{
    private readonly ICatchLocalRepository _localRepository;
    private readonly IFishingTripLocalRepository _tripRepository;
    private readonly ISyncMetadataRepository _syncMetadata;
    private readonly ICatchApiClient _apiClient;
    private readonly IPhotoApiClient _photoApiClient;
    private readonly ILogger<CatchSyncService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CatchSyncService"/>.
    /// </summary>
    public CatchSyncService(
        ICatchLocalRepository localRepository,
        IFishingTripLocalRepository tripRepository,
        ISyncMetadataRepository syncMetadata,
        ICatchApiClient apiClient,
        IPhotoApiClient photoApiClient,
        ILogger<CatchSyncService> logger)
    {
        _localRepository = localRepository;
        _tripRepository = tripRepository;
        _syncMetadata = syncMetadata;
        _apiClient = apiClient;
        _photoApiClient = photoApiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[Sync] Starting catch sync using {ApiClient}.", _apiClient.GetType().Name);
        await UploadDirtyCatchesAsync(ct);
        await DownloadRemoteChangesAsync(ct);
        _logger.LogInformation("[Sync] Catch sync complete.");
    }

    private async Task UploadDirtyCatchesAsync(CancellationToken ct)
    {
        var dirtyCatches = await _localRepository.GetDirtyAsync(ct);
        _logger.LogInformation("[Sync] Upload: {Count} dirty catch(es) found.", dirtyCatches.Count);

        foreach (var dirtyCatch in dirtyCatches)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                if (dirtyCatch.IsDeleted)
                {
                    _logger.LogInformation("[Sync] Deleting catch ServerId={ServerId}", dirtyCatch.ServerId);
                    await DeleteCatchOnServerAsync(dirtyCatch, ct);
                }
                else if (dirtyCatch.ServerId is null)
                {
                    _logger.LogInformation("[Sync] Uploading new catch LocalId={Id} Species={Species}", dirtyCatch.Id, dirtyCatch.Species);
                    await UploadNewCatchAsync(dirtyCatch, ct);
                }
                else
                {
                    _logger.LogInformation("[Sync] Updating catch ServerId={ServerId}", dirtyCatch.ServerId);
                    await UpdateCatchOnServerAsync(dirtyCatch, ct);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "[Sync] Network error uploading catch LocalId={Id}; will retry next sync.", dirtyCatch.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sync] Unexpected error uploading catch LocalId={Id}.", dirtyCatch.Id);
            }
        }
    }

    private async Task UploadNewCatchAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        var fishingTripServerId = await GetFishingTripServerIdAsync(localCatch, ct);
        if (fishingTripServerId is null)
        {
            _logger.LogInformation(
                "[Sync] Skipping catch LocalId={Id}; parent trip LocalId={FishingTripLocalId} has not synced yet.",
                localCatch.Id,
                localCatch.FishingTripLocalId);
            return;
        }

        var photoUrl = await UploadPhotoIfNeededAsync(localCatch, ct);
        var response = await _apiClient.CreateAsync(
            fishingTripServerId.Value,
            MapToCreateRequest(localCatch, photoUrl),
            ct);
        if (response is null)
            return;

        var trip = await _tripRepository.GetByServerIdAsync(response.FishingTripId, ct);
        if (trip is null)
            return;

        ApplyRemoteToLocal(localCatch, response, trip);
        await CompletePhotoSyncAsync(localCatch, ct);
    }

    private async Task UpdateCatchOnServerAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        if (!Guid.TryParse(localCatch.ServerId, out var serverId))
            return;

        var photoUrl = await UploadPhotoIfNeededAsync(localCatch, ct);
        var response = await _apiClient.UpdateAsync(
            serverId,
            MapToUpdateRequest(localCatch, photoUrl),
            ct);
        if (response is null)
            return;

        var trip = await _tripRepository.GetByServerIdAsync(response.FishingTripId, ct);
        if (trip is null)
            return;

        ApplyRemoteToLocal(localCatch, response, trip);
        await CompletePhotoSyncAsync(localCatch, ct);
    }

    private async Task DeleteCatchOnServerAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        if (!Guid.TryParse(localCatch.ServerId, out var serverId))
        {
            await DeleteRemotePhotosAsync(localCatch, ct);
            await _localRepository.PermanentlyDeleteAsync(localCatch.Id, ct);
            return;
        }

        await _apiClient.DeleteAsync(serverId, ct);
        await DeleteRemotePhotosAsync(localCatch, ct);
        await _localRepository.PermanentlyDeleteAsync(localCatch.Id, ct);
    }

    private async Task DownloadRemoteChangesAsync(CancellationToken ct)
    {
        var lastSync = await _syncMetadata.GetLastSyncAsync(SyncEntityType.Catch, ct);
        var syncFrom = lastSync ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _logger.LogInformation("[Sync] Download: fetching catches modified since {SyncFrom}.", syncFrom);

        List<CatchResponse> remoteCatches;
        try
        {
            remoteCatches = await _apiClient.GetModifiedSinceAsync(syncFrom, ct);
            _logger.LogInformation("[Sync] Download: received {Count} remote catch(es).", remoteCatches.Count);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[Sync] Network error during catch download.");
            return;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "[Sync] Timeout during catch download.");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] Unexpected error during catch download.");
            return;
        }

        foreach (var remoteCatch in remoteCatches)
        {
            if (ct.IsCancellationRequested)
                break;

            await UpsertRemoteCatchAsync(remoteCatch, ct);
        }

        if (remoteCatches.Count > 0)
            await _syncMetadata.SetLastSyncAsync(SyncEntityType.Catch, remoteCatches.Max(c => c.LastModifiedAt), ct);
    }

    private async Task UpsertRemoteCatchAsync(CatchResponse remoteCatch, CancellationToken ct)
    {
        var existing = await _localRepository.GetByServerIdAsync(remoteCatch.Id, ct);
        var localTrip = await _tripRepository.GetByServerIdAsync(remoteCatch.FishingTripId, ct);

        if (localTrip is null)
        {
            _logger.LogWarning(
                "[Sync] Skipping remote catch ServerId={ServerId}; parent trip ServerId={FishingTripServerId} is not in the local database.",
                remoteCatch.Id,
                remoteCatch.FishingTripId);
            return;
        }

        if (existing is null)
        {
            await _localRepository.SaveFromServerAsync(MapToLocalEntity(remoteCatch, localTrip), ct);
        }
        else if (existing.IsDirty && existing.LastModifiedUtc > remoteCatch.LastModifiedAt)
        {
            // Local is dirty and newer. Upload step will push it to the server.
        }
        else
        {
            ApplyRemoteToLocal(existing, remoteCatch, localTrip);
            existing.IsPhotoUploadPending = false;
            existing.PhotoUrlPendingDeletion = null;
            await _localRepository.SaveFromServerAsync(existing, ct);
        }
    }

    private static CreateCatchRequest MapToCreateRequest(CatchLocalEntity c, string? photoUrl) => new(
        c.Species,
        c.Length,
        c.Weight,
        photoUrl,
        c.Note,
        c.CaughtAt,
        c.Depth,
        c.Latitude,
        c.Longitude,
        MapToBaitDto(c));

    private static UpdateCatchRequest MapToUpdateRequest(CatchLocalEntity c, string? photoUrl) => new(
        c.Species,
        c.Length,
        c.Weight,
        photoUrl,
        c.Note,
        c.CaughtAt,
        c.Depth,
        c.Latitude,
        c.Longitude,
        MapToBaitDto(c));

    private static CatchLocalEntity MapToLocalEntity(CatchResponse r, FishingTripLocalEntity trip) => new()
    {
        ServerId = r.Id.ToString(),
        FishingTripLocalId = trip.Id,
        FishingTripServerId = r.FishingTripId.ToString(),
        LastModifiedUtc = r.LastModifiedAt,
        IsDirty = false,
        IsDeleted = false,
        Species = r.Species,
        Length = r.Length,
        Weight = r.Weight,
        PhotoUrl = r.PhotoUrl,
        Note = r.Note,
        CaughtAt = r.CaughtAt,
        Depth = r.Depth,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        BaitName = r.Bait?.Name,
        BaitType = r.Bait?.Type.ToString(),
        BaitColor = r.Bait?.Color,
        BaitWeightGrams = r.Bait?.WeightGrams,
        BaitLengthMm = r.Bait?.LengthMm
    };

    private static void ApplyRemoteToLocal(
        CatchLocalEntity local,
        CatchResponse remote,
        FishingTripLocalEntity trip)
    {
        if (!local.IsPhotoUploadPending
            && !string.Equals(local.PhotoUrl, remote.PhotoUrl, StringComparison.Ordinal))
        {
            local.LocalPhotoPath = null;
        }

        local.ServerId = remote.Id.ToString();
        local.FishingTripLocalId = trip.Id;
        local.FishingTripServerId = remote.FishingTripId.ToString();
        local.Species = remote.Species;
        local.Length = remote.Length;
        local.Weight = remote.Weight;
        local.PhotoUrl = remote.PhotoUrl;
        local.Note = remote.Note;
        local.CaughtAt = remote.CaughtAt;
        local.Depth = remote.Depth;
        local.Latitude = remote.Latitude;
        local.Longitude = remote.Longitude;
        local.BaitName = remote.Bait?.Name;
        local.BaitType = remote.Bait?.Type.ToString();
        local.BaitColor = remote.Bait?.Color;
        local.BaitWeightGrams = remote.Bait?.WeightGrams;
        local.BaitLengthMm = remote.Bait?.LengthMm;
        local.LastModifiedUtc = remote.LastModifiedAt;
        local.IsDirty = false;
        local.IsDeleted = false;
    }

    private async Task<string?> UploadPhotoIfNeededAsync(
        CatchLocalEntity localCatch,
        CancellationToken ct)
    {
        if (!localCatch.IsPhotoUploadPending)
            return localCatch.PhotoUrl;

        if (string.IsNullOrWhiteSpace(localCatch.LocalPhotoPath))
            return null;

        _logger.LogInformation("[Sync] Uploading photo for catch LocalId={Id}.", localCatch.Id);
        var uploadedUrl = await _photoApiClient.UploadAsync(localCatch.LocalPhotoPath, ct);

        // Persist the URL before syncing the catch. If the following catch request fails,
        // the next sync reuses this upload instead of creating an orphaned duplicate.
        localCatch.PhotoUrl = uploadedUrl;
        localCatch.IsPhotoUploadPending = false;
        await _localRepository.SaveFromServerAsync(localCatch, ct);

        return uploadedUrl;
    }

    private async Task CompletePhotoSyncAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        localCatch.IsPhotoUploadPending = false;
        localCatch.IsDirty = !string.IsNullOrWhiteSpace(localCatch.PhotoUrlPendingDeletion);
        await _localRepository.SaveFromServerAsync(localCatch, ct);

        if (string.IsNullOrWhiteSpace(localCatch.PhotoUrlPendingDeletion))
            return;

        await _photoApiClient.DeleteAsync(localCatch.PhotoUrlPendingDeletion, ct);
        localCatch.PhotoUrlPendingDeletion = null;
        localCatch.IsDirty = false;
        await _localRepository.SaveFromServerAsync(localCatch, ct);
    }

    private async Task DeleteRemotePhotosAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        var photoUrls = new[] { localCatch.PhotoUrl, localCatch.PhotoUrlPendingDeletion }
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal);

        foreach (var photoUrl in photoUrls)
            await _photoApiClient.DeleteAsync(photoUrl!, ct);
    }

    private async Task<Guid?> GetFishingTripServerIdAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        if (Guid.TryParse(localCatch.FishingTripServerId, out var existingServerId))
            return existingServerId;

        var trip = await _tripRepository.GetByIdAsync(localCatch.FishingTripLocalId, ct);
        if (trip is null || !Guid.TryParse(trip.ServerId, out var tripServerId))
            return null;

        return tripServerId;
    }

    private static BaitDto? MapToBaitDto(CatchLocalEntity c)
    {
        if (string.IsNullOrWhiteSpace(c.BaitName)
            && string.IsNullOrWhiteSpace(c.BaitType)
            && string.IsNullOrWhiteSpace(c.BaitColor)
            && c.BaitWeightGrams is null
            && c.BaitLengthMm is null)
        {
            return null;
        }

        BaitType? baitType = null;

        if (!string.IsNullOrWhiteSpace(c.BaitType)
            && Enum.TryParse<BaitType>(c.BaitType, ignoreCase: true, out var parsed))
        {
            baitType = parsed;
        }

        return new BaitDto(
            c.BaitName ?? string.Empty,
            baitType,
            c.BaitColor,
            c.BaitWeightGrams,
            c.BaitLengthMm);
    }
}
