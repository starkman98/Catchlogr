namespace Catchlogr.Sync.Abstractions;

/// <summary>
/// Repository for reading and writing sync timestamps.
/// Used by the sync service to track what has already been downloaded from the server.
/// </summary>
public interface ISyncMetadataRepository
{
    /// <summary>
    /// Returns the last sync timestamp for the given entity type.
    /// Returns null if this entity type has never been synced.
    /// </summary>
    Task<DateTime?> GetLastSyncAsync(string entityType, CancellationToken ct = default);

    /// <summary>
    /// Saves (or updates) the last sync timestamp for the given entity type.
    /// Call this after a successful sync to advance the cursor.
    /// </summary>
    Task SetLastSyncAsync(string entityType, DateTime syncTime, CancellationToken ct = default);
}
