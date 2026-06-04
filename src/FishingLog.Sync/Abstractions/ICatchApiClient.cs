using FishingLog.Contracts.CatchDTOs;
using FishingLog.Contracts.FishingTripDTOs;

namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Abstraction for calling the FishingLog REST API.
/// ViewModels and the sync service use this — never HttpClient directly.
/// </summary>
public interface ICatchApiClient
{
    /// <summary>Returns all catches from the server.</summary>
    Task<List<CatchResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns only catches modified after the given UTC timestamp.
    /// This is what the sync service calls on every sync — pass the last sync cursor.
    /// </summary>
    Task<List<CatchResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default);

    /// <summary>Returns a single catch by server GUID, or null if not found (404).</summary>
    Task<CatchResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    ///<summary>Retruns catches by server tripId from the server.</summary>
    Task<List<CatchResponse>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);

    /// <summary>Creates a new catch on the server. Returns null on failure.</summary>
    Task<CatchResponse?> CreateAsync(Guid tripId, CreateCatchRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing catch. Returns null if not found (404) or on failure.</summary>
    Task<CatchResponse?> UpdateAsync(Guid id, UpdateCatchRequest request, CancellationToken ct = default);

    /// <summary>Deletes a catch. Returns false if not found (404).</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
