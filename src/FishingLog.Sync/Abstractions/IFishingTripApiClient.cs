using FishingLog.Contracts.FishingTripDTOs;

namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Abstraction for calling the FishingLog REST API.
/// ViewModels and the sync service use this — never HttpClient directly.
/// </summary>
public interface IFishingTripApiClient
{
    /// <summary>Returns all trips from the server.</summary>
    Task<List<FishingTripResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns only trips modified after the given UTC timestamp.
    /// This is what the sync service calls on every sync — pass the last sync cursor.
    /// </summary>
    Task<List<FishingTripResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default);

    /// <summary>Returns a single trip by server GUID, or null if not found (404).</summary>
    Task<FishingTripResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new trip on the server. Returns null on failure.</summary>
    Task<FishingTripResponse?> CreateAsync(CreateFishingTripRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing trip. Returns null if not found (404) or on failure.</summary>
    Task<FishingTripResponse?> UpdateAsync(Guid id, UpdateFishingTripRequest request, CancellationToken ct = default);

    /// <summary>Deletes a trip. Returns false if not found (404).</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
