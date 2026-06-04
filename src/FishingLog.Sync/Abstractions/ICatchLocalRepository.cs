using FishingLog.Sync.Entities;

namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Repository interface for local SQLite catch data access.
/// </summary>
public interface ICatchLocalRepository
{
    /// <summary>Returns all non-deleted catches from the local database.</summary>
    Task<List<CatchLocalEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single catch by its local integer ID, or null if not found.</summary>
    Task<CatchLocalEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns a single catch by its server GUID, or null if not found.
    /// Used by the sync service to match downloaded records to existing local records.
    /// </summary>
    Task<CatchLocalEntity?> GetByServerIdAsync(Guid serverId, CancellationToken ct = default);

    /// <summary>Returns catches by its local tripId</summary>
    Task<List<CatchLocalEntity>> GetByTripIdAsync(int localTripId, CancellationToken ct = default);

    /// <summary>Returns all catches that have unsynchronised local changes.</summary>
    Task<List<CatchLocalEntity>> GetDirtyAsync(CancellationToken ct = default);

    /// <summary>Inserts a new catch and marks it dirty. Returns the generated local ID.</summary>
    Task<int> AddAsync(CatchLocalEntity localCatch, CancellationToken ct = default);

    /// <summary>Updates an existing catch and marks it as dirty.</summary>
    Task UpdateAsync(CatchLocalEntity localCatch, CancellationToken ct = default);

    /// <summary>Soft-deletes a catch by setting IsDeleted = true and IsDirty = true.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Permanently deletes a catch from local database</summary>
    Task PermanentlyDeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Called by the sync service after a successful upload.
    /// Stamps the record with the server's GUID and clears the dirty flag.
    /// </summary>
    Task MarkAsSyncedAsync(int id, Guid serverId, DateTime lastModifiedUtc, CancellationToken ct = default);

    /// <summary>
    /// Inserts or updates a record that came from the server.
    /// Does NOT touch IsDirty or LastModifiedUtc — the entity's values are saved as-is.
    /// Used during the download step of sync.
    /// </summary>
    Task SaveFromServerAsync(CatchLocalEntity localCatch, CancellationToken ct = default);
}
