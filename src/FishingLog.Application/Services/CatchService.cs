using FishingLog.Application.Exceptions;
using FishingLog.Application.Interfaces;
using FishingLog.Contracts.CatchDTOs;
using FishingLog.Domain.Entities;
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

        ValidateCatchTime(request.CaughtAt, trip);

        var newCatch = MapFromCreateToCatch(request, tripId);

        await _repo.AddAsync(newCatch, ct);

        return MapFromCatchToResponse(newCatch);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existingCatch = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Catch {id} not found");

        await _repo.DeleteAsync(id, ct);
    }

    /// <inheritdoc/>
    public async Task<List<CatchResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var catches = await _repo.GetAllAsync(ct);

        return catches.Select(MapFromCatchToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<CatchResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var existingCatch = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Catch {id} not found");

        return MapFromCatchToResponse(existingCatch);
    }

    /// <inheritdoc/>
    public async Task<List<CatchResponse>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _tripRepo.GetByIdAsync(tripId, ct)
            ?? throw new NotFoundException($"Trip {tripId} not Found");

        var catches = await _repo.GetByTripIdAsync(tripId, ct);

        return catches.Select(MapFromCatchToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<CatchResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        if (since.Kind != DateTimeKind.Utc)
            since = DateTime.SpecifyKind(since, DateTimeKind.Utc);

        var modifiedCatches = await _repo.GetModifiedSinceAsync(since, ct);

        return modifiedCatches.Select(MapFromCatchToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<CatchResponse> UpdateAsync(Guid id, UpdateCatchRequest request, CancellationToken ct = default)
    {
        var existingCatch = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Catch {id} not found");

        var trip = await _tripRepo.GetByIdAsync(existingCatch.TripId, ct)
            ?? throw new NotFoundException($"Trip {existingCatch.TripId} not found.");

        ValidateCatchTime(request.CaughtAt, trip);

        ApplyUpdate(existingCatch, request);
        await _repo.UpdateAsync(existingCatch, ct);

        return MapFromCatchToResponse(existingCatch);
    }

    // -------------------------------------------------------------------------
    // Private mapping — keeps the service methods readable
    // -------------------------------------------------------------------------

    private static CatchResponse MapFromCatchToResponse(Catch c) => new(
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
        MapFromBaitToBaitDto(c.Bait)
        );

    private static BaitDto? MapFromBaitToBaitDto(Bait? b)
        => b is null ? null : new(
        b.Name,
        (Contracts.CatchDTOs.BaitType?)b.Type,
        b.Color,
        b.WeightGrams,
        b.LengthMm
        );

    private static Bait? MapFromBaitDtoToBait(BaitDto? dto) =>
        dto is null ? null : new(
        dto.Name,
        (Domain.Enums.BaitType?)dto.Type,
        dto.Color,
        dto.WeightGrams,
        dto.LengthMm
        );

    private static Catch MapFromCreateToCatch(CreateCatchRequest r, Guid tripId) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        Species = r.Species,
        Length = r.Length,
        Weight = r.Weight,
        PhotoUrl = r.PhotoUrl,
        Note = r.Note,
        CaughtAt = DateTime.SpecifyKind(r.CaughtAt, DateTimeKind.Utc),
        LastModified = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        Depth = r.Depth,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        Bait = MapFromBaitDtoToBait(r.Bait)
    };

    private static void ApplyUpdate(Catch existing, UpdateCatchRequest r)
    {
        existing.Species = r.Species;
        existing.Length = r.Length;
        existing.Weight = r.Weight;
        existing.PhotoUrl = r.PhotoUrl;
        existing.Note = r.Note;
        existing.CaughtAt = DateTime.SpecifyKind(r.CaughtAt, DateTimeKind.Utc);
        existing.LastModified = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        existing.Depth = r.Depth;
        existing.Latitude = r.Latitude;
        existing.Longitude = r.Longitude;
        existing.Bait = MapFromBaitDtoToBait(r.Bait);
    }

    private static void ValidateCatchTime(DateTime catchTime, FishingTrip trip)
    {
        if (catchTime < trip.StartTime)
            throw new BusinessRuleException("Catch time cannot be before the trip started.");

        if (trip.EndTime.HasValue && catchTime > trip.EndTime.Value)
            throw new BusinessRuleException("Catch time cannot be after the trip ended.");
    }
}
