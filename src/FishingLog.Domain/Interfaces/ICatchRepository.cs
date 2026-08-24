using FishingLog.Domain.Entities;

namespace FishingLog.Domain.Interfaces;

public interface ICatchRepository
{
    /// <summary>Returns all catches belonging to the specified user.</summary>
    Task<List<Catch>> GetAllAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns a catch when it belongs to the specified user. by GUID, or null if not found.</summary>
    Task<Catch?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>Returns catches for a trip belonging to the specified user.</summary>
    Task<List<Catch>> GetByTripIdAsync(Guid tripId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all Catches that belong to the specified user modified after the given UTC timestamp, ordered by LastModified ascending.
    /// Used by the mobile sync service to download only incremental changes.
    /// </summary>
    Task<List<Catch>> GetModifiedSinceAsync(Guid userId, DateTime since, CancellationToken ct = default);

    /// <summary>Persists a new Catch to the database.</summary>
    Task AddAsync(Catch catchToAdd, CancellationToken ct = default);

    /// <summary>Saves changes to an existing Catch.</summary>
    Task UpdateAsync(Catch catchToUpdate, CancellationToken ct = default);

    /// <summary>Deletes a catch when it belongs to the specified user.</summary>
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
