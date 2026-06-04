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
    private readonly ILogger<CatchSyncService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CatchSyncService"/>.
    /// </summary>
    public CatchSyncService(
        ICatchLocalRepository localRepository,
        IFishingTripLocalRepository tripRepository,
        ISyncMetadataRepository syncMetadata,
        ICatchApiClient apiClient,
        ILogger<CatchSyncService> logger)
    {
        _localRepository = localRepository;
        _tripRepository = tripRepository;
        _syncMetadata = syncMetadata;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[Sync] Starting sync. BaseAddress={BaseAddress}",
            _apiClient.GetType().Name);
        await UploadDirtyCatchesAsync(ct);
        await DownloadRemoteChangesAsync(ct);
        _logger.LogInformation("[Sync] Sync complete.");
    }

    // -------------------------------------------------------------------------
    // Step 1 — Upload
    // -------------------------------------------------------------------------

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
                if (dirtyCatch.ServerId is null)
                {
                    _logger.LogInformation("[Sync] Uploading new trip LocalId={Id} Species={Species}", dirtyCatch.Id, dirtyCatch.Species);
                    await UploadNewCatchAsync(dirtyCatch, ct);
                    _logger.LogInformation("[Sync] Upload succeeded for LocalId={Id}", dirtyCatch.Id);
                }
                else if (dirtyCatch.IsDeleted)
                {
                    _logger.LogInformation("[Sync] Deleting trip ServerId={ServerId}", dirtyCatch.ServerId);
                    await DeleteCatchOnServerAsync(dirtyCatch, ct);
                }
                else
                {
                    _logger.LogInformation("[Sync] Updating trip ServerId={ServerId}", dirtyCatch.ServerId);
                    await UpdateCatchOnServerAsync(dirtyCatch, ct);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "[Sync] Network error uploading LocalId={Id} — will retry next sync.", dirtyCatch.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sync] Unexpected error uploading LocalId={Id}", dirtyCatch.Id);
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

        var response = await _apiClient.CreateAsync(fishingTripServerId.Value, MapToCreateRequest(localCatch), ct);

        if (response is not null)
        {
            localCatch.FishingTripServerId = fishingTripServerId.Value.ToString();
            await _localRepository.SaveFromServerAsync(localCatch, ct);
            await _localRepository.MarkAsSyncedAsync(localCatch.Id, response.Id, response.LastModifiedAt, ct);
        }
    }

    private async Task UpdateCatchOnServerAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        if (!Guid.TryParse(localCatch.ServerId, out var serverId))
            return;

        var response = await _apiClient.UpdateAsync(serverId, MapToUpdateRequest(localCatch), ct);

        if (response is not null)
            await _localRepository.MarkAsSyncedAsync(localCatch.Id, serverId, response.LastModifiedAt, ct);
    }

    private async Task DeleteCatchOnServerAsync(CatchLocalEntity localCatch, CancellationToken ct)
    {
        if (!Guid.TryParse(localCatch.ServerId, out var serverId))
        {
            await _localRepository.PermanentlyDeleteAsync(localCatch.Id, ct);
            return;
        }

        await _apiClient.DeleteAsync(serverId, ct);
        await _localRepository.PermanentlyDeleteAsync(localCatch.Id, ct);
    }

    // -------------------------------------------------------------------------
    // Step 2 — Download
    // -------------------------------------------------------------------------

    private async Task DownloadRemoteChangesAsync(CancellationToken ct)
    {
        var lastSync = await _syncMetadata.GetLastSyncAsync(SyncEntityType.Catch, ct);

        // Use a safe minimum date rather than DateTime.MinValue — some serialisers/APIs reject year 0001
        var syncFrom = lastSync ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _logger.LogInformation("[Sync] Download: fetching trips modified since {SyncFrom}", syncFrom);

        List<CatchResponse> remoteCatches;
        try
        {
            remoteCatches = await _apiClient.GetModifiedSinceAsync(syncFrom, ct);
            _logger.LogInformation("[Sync] Download: received {Count} remote trip(s).", remoteCatches.Count);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[Sync] Network error during download.");
            return;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "[Sync] Timeout during download.");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] Unexpected error during download.");
            return;
        }

        foreach (var remoteCatch in remoteCatches)
        {
            if (ct.IsCancellationRequested)
                break;

            await UpsertRemoteCatchAsync(remoteCatch, ct);
        }

        // Advance the sync cursor so the next sync only downloads new changes
        if (remoteCatches.Count > 0)
            await _syncMetadata.SetLastSyncAsync(SyncEntityType.Catch, DateTime.UtcNow, ct);
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
            // Not in local DB at all — insert as a clean record
            await _localRepository.SaveFromServerAsync(MapToLocalEntity(remoteCatch, localTrip), ct);
        }
        else if (existing.IsDirty && existing.LastModifiedUtc > remoteCatch.LastModifiedAt)
        {
            // Local is dirty and newer — local wins, skip
            // The upload step above will push local changes to the server
        }
        else
        {
            // Server is newer, or local is clean — apply server changes
            ApplyRemoteToLocal(existing, remoteCatch, localTrip);
            await _localRepository.SaveFromServerAsync(existing, ct);
        }
    }

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static CreateCatchRequest MapToCreateRequest(CatchLocalEntity c) => new(
        c.Species,
        c.Length,
        c.Weight,
        c.PhotoUrl,
        c.Note,
        c.CaughtAt,
        c.Depth,
        c.Latitude,
        c.Longitude,
        MapToBaitDto(c)
        );

    private static UpdateCatchRequest MapToUpdateRequest(CatchLocalEntity c) => new(
        c.Species,
        c.Length,
        c.Weight,
        c.PhotoUrl,
        c.Note,
        c.CaughtAt,
        c.Depth,
        c.Latitude,
        c.Longitude,
        MapToBaitDto(c)
        );

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
        BaitName = r?.Bait?.Name,
        BaitType = r?.Bait?.Type.ToString(),
        BaitColor = r?.Bait?.Color,
        BaitWeightGrams = r?.Bait?.WeightGrams,
        BaitLengthMm = r?.Bait?.LengthMm
    };

    private static void ApplyRemoteToLocal(
        CatchLocalEntity local,
        CatchResponse remote,
        FishingTripLocalEntity trip)
    {
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
        local.BaitName = remote?.Bait?.Name;
        local.BaitType = remote?.Bait?.Type.ToString();
        local.BaitColor = remote?.Bait?.Color;
        local.BaitWeightGrams = remote?.Bait?.WeightGrams;
        local.BaitLengthMm = remote?.Bait?.LengthMm;
        local.LastModifiedUtc = remote!.LastModifiedAt;
        local.IsDirty = false;
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
