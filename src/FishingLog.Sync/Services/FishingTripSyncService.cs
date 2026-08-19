using FishingLog.Contracts.FishingTripDTOs;
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
public class FishingTripSyncService : IFishingTripSyncService
{
    private readonly IFishingTripLocalRepository _localRepository;
    private readonly ISyncMetadataRepository _syncMetadata;
    private readonly IFishingTripApiClient _apiClient;
    private readonly ILogger<FishingTripSyncService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripSyncService"/>.
    /// </summary>
    public FishingTripSyncService(
        IFishingTripLocalRepository localRepository,
        ISyncMetadataRepository syncMetadata,
        IFishingTripApiClient apiClient,
        ILogger<FishingTripSyncService> logger)
    {
        _localRepository = localRepository;
        _syncMetadata = syncMetadata;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[Sync] Starting sync. BaseAddress={BaseAddress}",
            _apiClient.GetType().Name);
        await UploadDirtyTripsAsync(ct);
        await DownloadRemoteChangesAsync(ct);
        _logger.LogInformation("[Sync] Sync complete.");
    }

    // -------------------------------------------------------------------------
    // Step 1 — Upload
    // -------------------------------------------------------------------------

    private async Task UploadDirtyTripsAsync(CancellationToken ct)
    {
        var dirtyTrips = await _localRepository.GetDirtyAsync(ct);
        _logger.LogInformation("[Sync] Upload: {Count} dirty trip(s) found.", dirtyTrips.Count);

        foreach (var trip in dirtyTrips)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                if (trip.ServerId is null)
                {
                    _logger.LogInformation("[Sync] Uploading new trip LocalId={Id} Name={Name}", trip.Id, trip.Name);
                    await UploadNewTripAsync(trip, ct);
                    _logger.LogInformation("[Sync] Upload succeeded for LocalId={Id}", trip.Id);
                }
                else if (trip.IsDeleted)
                {
                    _logger.LogInformation("[Sync] Deleting trip ServerId={ServerId}", trip.ServerId);
                    await DeleteTripOnServerAsync(trip, ct);
                }
                else
                {
                    _logger.LogInformation("[Sync] Updating trip ServerId={ServerId}", trip.ServerId);
                    await UpdateTripOnServerAsync(trip, ct);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "[Sync] Network error uploading LocalId={Id} — will retry next sync.", trip.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sync] Unexpected error uploading LocalId={Id}", trip.Id);
            }
        }
    }

    private async Task UploadNewTripAsync(FishingTripLocalEntity trip, CancellationToken ct)
    {
        var response = await _apiClient.CreateAsync(MapToCreateRequest(trip), ct);

        if (response is null)
            return;

        ApplyRemoteToLocal(trip, response);
        await _localRepository.SaveFromServerAsync(trip, ct);
    }

    private async Task UpdateTripOnServerAsync(FishingTripLocalEntity trip, CancellationToken ct)
    {
        if (!Guid.TryParse(trip.ServerId, out var serverId))
            return;

        var response = await _apiClient.UpdateAsync(serverId, MapToUpdateRequest(trip), ct);

        if (response is null)
            return;

        ApplyRemoteToLocal(trip, response);
        await _localRepository.SaveFromServerAsync(trip, ct);
    }

    private async Task DeleteTripOnServerAsync(FishingTripLocalEntity trip, CancellationToken ct)
    {
        if (!Guid.TryParse(trip.ServerId, out var serverId))
        {
            await _localRepository.PermanentlyDeleteAsync(trip.Id, ct);
            return;
        }

            await _apiClient.DeleteAsync(serverId, ct);
            await _localRepository.PermanentlyDeleteAsync(trip.Id, ct);
    }

    // -------------------------------------------------------------------------
    // Step 2 — Download
    // -------------------------------------------------------------------------

    private async Task DownloadRemoteChangesAsync(CancellationToken ct)
    {
        var lastSync = await _syncMetadata.GetLastSyncAsync(SyncEntityType.FishingTrip, ct);

        // Use a safe minimum date rather than DateTime.MinValue — some serialisers/APIs reject year 0001
        var syncFrom = lastSync ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _logger.LogInformation("[Sync] Download: fetching trips modified since {SyncFrom}", syncFrom);

        List<FishingTripResponse> remoteTrips;
        try
        {
            remoteTrips = await _apiClient.GetModifiedSinceAsync(syncFrom, ct);
            _logger.LogInformation("[Sync] Download: received {Count} remote trip(s).", remoteTrips.Count);
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

        foreach (var remoteTrip in remoteTrips)
        {
            if (ct.IsCancellationRequested)
                break;

            await UpsertRemoteTripAsync(remoteTrip, ct);
        }

        // Advance the sync cursor so the next sync only downloads new changes
        if (remoteTrips.Count > 0)
            await _syncMetadata.SetLastSyncAsync(SyncEntityType.FishingTrip, DateTime.UtcNow, ct);
    }

    private async Task UpsertRemoteTripAsync(FishingTripResponse remoteTrip, CancellationToken ct)
    {
        var existing = await _localRepository.GetByServerIdAsync(remoteTrip.Id, ct);

        if (existing is null)
        {
            // Not in local DB at all — insert as a clean record
            await _localRepository.SaveFromServerAsync(MapToLocalEntity(remoteTrip), ct);
        }
        else if (existing.IsDirty && existing.LastModifiedUtc > remoteTrip.LastModified)
        {
            // Local is dirty and newer — local wins, skip
            // The upload step above will push local changes to the server
        }
        else
        {
            // Server is newer, or local is clean — apply server changes
            ApplyRemoteToLocal(existing, remoteTrip);
            await _localRepository.SaveFromServerAsync(existing, ct);
        }
    }

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static CreateFishingTripRequest MapToCreateRequest(FishingTripLocalEntity t) => new(
        t.Name,
        t.LocationName,
        t.WaterTemp,
        t.WeatherDescription,
        t.Latitude,
        t.Longitude,
        t.StartTime,
        t.EndTime,
        t.Note);

    private static UpdateFishingTripRequest MapToUpdateRequest(FishingTripLocalEntity t) => new(
        t.Name,
        t.LocationName,
        t.WaterTemp,
        t.WeatherDescription,
        t.Latitude,
        t.Longitude,
        t.StartTime,
        t.EndTime,
        t.Note);

    private static FishingTripLocalEntity MapToLocalEntity(FishingTripResponse r) => new()
    {
        ServerId = r.Id.ToString(),
        LastModifiedUtc = r.LastModified,
        IsDirty = false,
        IsDeleted = false,
        Name = r.Name,
        LocationName = r.LocationName,
        WaterTemp = r.WaterTemp,
        WeatherDescription = r.WeatherDescription,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        StartTime = r.StartTime,
        EndTime = r.EndTime,
        Note = r.Note,
        CreatedAt = r.CreatedAt
    };

    private static void ApplyRemoteToLocal(FishingTripLocalEntity local, FishingTripResponse remote)
    {
        local.ServerId = remote.Id.ToString();
        local.Name = remote.Name;
        local.LocationName = remote.LocationName;
        local.WaterTemp = remote.WaterTemp;
        local.WeatherDescription = remote.WeatherDescription;
        local.Latitude = remote.Latitude;
        local.Longitude = remote.Longitude;
        local.StartTime = remote.StartTime;
        local.EndTime = remote.EndTime;
        local.Note = remote.Note;
        local.LastModifiedUtc = remote.LastModified;
        local.IsDirty = false;
        local.IsDeleted = false;
    }
}
