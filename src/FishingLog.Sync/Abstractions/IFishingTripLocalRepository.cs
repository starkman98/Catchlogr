using FishingLog.Sync.Entities;

namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Repository interface for local SQLite fishing trip data access.
/// </summary>
public interface IFishingTripLocalRepository
{
    /// <summary>Returns all non-deleted trips from the local database.</summary>
    Task<List<FishingTripLocalEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single trip by its local integer ID, or null if not found.</summary>
    Task<FishingTripLocalEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns a single trip by its server GUID, or null if not found.
    /// Used by the sync service to match downloaded records to existing local records.
    /// </summary>
    Task<FishingTripLocalEntity?> GetByServerIdAsync(Guid serverId, CancellationToken ct = default);

    /// <summary>Returns all trips that have unsynchronised local changes.</summary>
    Task<List<FishingTripLocalEntity>> GetDirtyAsync(CancellationToken ct = default);

    /// <summary>Inserts a new trip and marks it dirty. Returns the generated local ID.</summary>
    Task<int> AddAsync(FishingTripLocalEntity trip, CancellationToken ct = default);

    /// <summary>Updates an existing trip and marks it as dirty.</summary>
    Task UpdateAsync(FishingTripLocalEntity trip, CancellationToken ct = default);

    /// <summary>Soft-deletes a trip by setting IsDeleted = true and IsDirty = true.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Permanently deletes a trip from local database</summary>
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
    Task SaveFromServerAsync(FishingTripLocalEntity trip, CancellationToken ct = default);
}
