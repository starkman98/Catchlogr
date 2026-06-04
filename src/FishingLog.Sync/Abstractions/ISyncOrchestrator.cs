namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Coordinates sync services that must run in a specific order.
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Runs a complete application sync.
    /// </summary>
    Task SyncAsync(CancellationToken ct = default);
}
