namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Orchestrates the two-way sync between the local SQLite database and the remote API.
/// </summary>
public interface ICatchSyncService
{
    /// <summary>
    /// Runs a full sync cycle:
    /// 1. Uploads all dirty local trips to the server.
    /// 2. Downloads all trips modified since the last sync cursor.
    /// 3. Upserts downloaded trips into the local database.
    /// </summary>
    Task SyncAsync(CancellationToken ct = default);
}
