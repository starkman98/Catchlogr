using FishingLog.Domain.Entities;

namespace FishingLog.Domain.Interfaces;

/// <summary>
/// Repository interface for fishing trip data access.
/// Defined in Domain — implemented in Infrastructure (Dependency Inversion).
/// </summary>
public interface IFishingTripRepository
{
    /// <summary>Returns all trips ordered by StartTime descending.</summary>
    Task<List<FishingTrip>> GetAllAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns a single trip by GUID, or null if not found.</summary>
    Task<FishingTrip?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all trips modified after the given UTC timestamp, ordered by LastModified ascending.
    /// Used by the mobile sync service to download only incremental changes.
    /// </summary>
    Task<List<FishingTrip>> GetModifiedSinceAsync(Guid userId, DateTime since, CancellationToken ct = default);

    /// <summary>Persists a new trip to the database.</summary>
    Task AddAsync(FishingTrip trip, CancellationToken ct = default);

    /// <summary>Saves changes to an existing trip.</summary>
    Task UpdateAsync(FishingTrip trip, CancellationToken ct = default);

    /// <summary>Deletes a trip by GUID. No-op if not found.</summary>
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
