using FishingLog.Sync.Abstractions;
using Microsoft.Extensions.Logging;

namespace FishingLog.Sync.Services;

/// <summary>
/// Runs entity sync services in dependency order.
/// </summary>
public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IFishingTripSyncService _fishingTripSyncService;
    private readonly ICatchSyncService _catchSyncService;
    private readonly ILogger<SyncOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncOrchestrator"/>.
    /// </summary>
    public SyncOrchestrator(
        IFishingTripSyncService fishingTripSyncService,
        ICatchSyncService catchSyncService,
        ILogger<SyncOrchestrator> logger)
    {
        _fishingTripSyncService = fishingTripSyncService;
        _catchSyncService = catchSyncService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[Sync] Starting full sync.");

        await _fishingTripSyncService.SyncAsync(ct);
        await _catchSyncService.SyncAsync(ct);

        _logger.LogInformation("[Sync] Full sync complete.");
    }
}
