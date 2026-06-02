using FishingLog.Contracts.FishingTripDTOs;

namespace FishingLog.Application.Interfaces;

/// <summary>
/// Service interface for fishing trip business logic.
/// Called by API endpoints — orchestrates the repository and maps to DTOs.
/// </summary>
public interface IFishingTripService
{
    /// <summary>Returns all trips as response DTOs.</summary>
    Task<List<FishingTripResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single trip, or null if not found.</summary>
    Task<FishingTripResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns all trips modified after the given UTC timestamp.
    /// Supports the mobile sync download step.
    /// </summary>
    Task<List<FishingTripResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default);

    /// <summary>Creates a new trip and returns the persisted record.</summary>
    Task<FishingTripResponse> CreateAsync(CreateFishingTripRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing trip. Returns null if not found.</summary>
    Task<FishingTripResponse> UpdateAsync(Guid id, UpdateFishingTripRequest request, CancellationToken ct = default);

    /// <summary>Deletes a trip. Returns false if not found.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
