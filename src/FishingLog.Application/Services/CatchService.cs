using FishingLog.Application.Exceptions;
using FishingLog.Application.Interfaces;
using FishingLog.Contracts.CatchDTOs;
using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Domain.Entities;
using FishingLog.Domain.Enums;
using FishingLog.Domain.Interfaces;
using FishingLog.Domain.ValueObjects;

namespace FishingLog.Application.Services;

public class CatchService : ICatchService
{
    private readonly ICatchRepository _repo;
    private readonly IFishingTripRepository _tripRepo;

    public CatchService(ICatchRepository repo, IFishingTripRepository tripRepo)
    {
        _repo = repo;
        _tripRepo = tripRepo;
    }

    /// <inheritdoc/>
    public async Task<CatchResponse> CreateAsync(Guid tripId, CreateCatchRequest request, CancellationToken ct = default)
    {
        // TODO: verify ownership
        var trip = await _tripRepo.GetByIdAsync(tripId, ct)
            ?? throw new NotFoundException($"Trip {tripId} not found");

        var newTrip = MapFromRequest(request, tripId);

        await _repo.AddAsync(newTrip, ct);

        return MapToResponse(newTrip);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<List<CatchResponse>> GetAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<CatchResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<List<CatchResponse>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<List<CatchResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<CatchResponse?> UpdateAsync(Guid id, UpdateCatchRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    // -------------------------------------------------------------------------
    // Private mapping — keeps the service methods readable
    // -------------------------------------------------------------------------

    private static CatchResponse MapToResponse(Catch c) => new(
        c.Id,
        c.TripId,
        c.Species,
        c.Length,
        c.Weight,
        c.PhotoUrl,
        c.Note,
        c.CaughtAt,
        c.LastModified,
        c.Depth,
        c.Latitude,
        c.Longitude,
        MapToBaitDto(c.Bait)
        );

    private static BaitDto? MapToBaitDto(Bait? b)
        => b is null ? null : new(
        b.Name,
        (Contracts.CatchDTOs.BaitType?)b.Type,
        b.Color,
        b.WeightGrams,
        b.LengthMm
        );

    private static Bait? MapFromBaitDto(BaitDto? dto) =>
        dto is null ? null : new(
        dto.Name,
        (Domain.Enums.BaitType?)dto.Type,
        dto.Color,
        dto.WeightGrams,
        dto.LengthMm
        );

    private static Catch MapFromRequest(CreateCatchRequest r, Guid tripId) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        Species = r.Species,
        Length = r.Length,
        Weight = r.Weight,
        PhotoUrl = r.PhotoUrl,
        Note = r.Note,
        CaughtAt = r.CaughtAt,
        LastModified = r.LastModifiedAt,
        Depth = r.Depth,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        Bait = MapFromBaitDto(r.Bait)
    };
}
