namespace Catchlogr.Sync.Abstractions;

/// <summary>
/// Orchestrates the two-way sync between the local SQLite database and the remote API.
/// </summary>
public interface IFishingTripSyncService
{
    /// <summary>
    /// Runs a full sync cycle:
    /// 1. Uploads all dirty local catches to the server.
    /// 2. Downloads all catches modified since the last sync cursor.
    /// 3. Upserts downloaded catches into the local database.
    /// </summary>
    Task SyncAsync(CancellationToken ct = default);
}