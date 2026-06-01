using FishingLog.Domain.Entities;

namespace FishingLog.Domain.Interfaces;

public interface ICatchRepository
{
    /// <summary>Returns all Catches ordered by CaughtAt descending.</summary>
    Task<List<Catch>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single Catch by GUID, or null if not found.</summary>
    Task<Catch?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all Catches of a single trip by its fishingTripId.</summary>
    Task<List<Catch>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);

    /// <summary>
    /// Returns all Catches modified after the given UTC timestamp, ordered by LastModified ascending.
    /// Used by the mobile sync service to download only incremental changes.
    /// </summary>
    Task<List<Catch>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default);

    /// <summary>Persists a new Catch to the database.</summary>
    Task AddAsync(Catch catchToAdd, CancellationToken ct = default);

    /// <summary>Saves changes to an existing Catch.</summary>
    Task UpdateAsync(Catch catchToUpdate, CancellationToken ct = default);

    /// <summary>Deletes a Catch by GUID. No-op if not found.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
