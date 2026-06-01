using SQLite;

namespace FishingLog.Sync.Entities;

/// <summary>
/// Tracks the last successful sync timestamp per entity type.
/// The sync service reads this to know what to download from the server
/// (i.e. everything modified after <see cref="LastSyncUtc"/>).
/// One row per entity type — use <see cref="SyncEntityType"/> constants for the key.
/// </summary>
public class SyncMetadataEntity
{
    /// <summary>Local auto-increment primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// The entity type key, e.g. "FishingTrip".
    /// Always use <see cref="SyncEntityType"/> constants — never raw strings.
    /// </summary>
    [Indexed]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last successful sync for this entity type.
    /// Null means this entity type has never been synced — a full download will run.
    /// </summary>
    public DateTime? LastSyncUtc { get; set; }
}
