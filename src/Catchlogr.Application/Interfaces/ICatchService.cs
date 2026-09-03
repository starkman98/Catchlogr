using Catchlogr.Contracts.CatchDTOs;
using Catchlogr.Contracts.FishingTripDTOs;

namespace Catchlogr.Application.Interfaces;

public interface ICatchService
{
    /// <summary>Returns all catches as response DTOs.</summary>
    Task<List<CatchResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single catch as response DTO. Throws <see cref="Catchlogr.Application.Exceptions.NotFoundException"/> if not found.</summary>
    Task<CatchResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all Catches of a single trip by its fishingTripId.</summary>
    Task<List<CatchResponse>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);

    /// <summary>
    /// Returns all catches modified after the given UTC timestamp.
    /// Supports the mobile sync download step.
    /// </summary>
    Task<List<CatchResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default);

    /// <summary>Creates a new catch and returns the persisted record.</summary>
    Task<CatchResponse> CreateAsync(Guid tripId, CreateCatchRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing catch. Throws <see cref="Catchlogr.Application.Exceptions.NotFoundException"/> if not found.</summary>
    Task<CatchResponse> UpdateAsync(Guid id, UpdateCatchRequest request, CancellationToken ct = default);

    /// <summary>Deletes a catch. Returns false if not found.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
