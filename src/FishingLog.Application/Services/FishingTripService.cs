using FishingLog.Application.Exceptions;
using FishingLog.Application.Interfaces;
using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Domain.Entities;
using FishingLog.Domain.Interfaces;

namespace FishingLog.Application.Services;

/// <summary>
/// Business logic service for fishing trips.
/// Maps between <see cref="FishingTrip"/> domain entities and response/request DTOs.
/// </summary>
public class FishingTripService : IFishingTripService
{
    private readonly IFishingTripRepository _repository;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripService"/>.
    /// </summary>
    public FishingTripService(IFishingTripRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var trips = await _repository.GetAllAsync(ct);
        return trips.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, ct);
        return trip is null ? null : MapToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        if (since.Kind != DateTimeKind.Utc)
            since = DateTime.SpecifyKind(since, DateTimeKind.Utc);

        var trips = await _repository.GetModifiedSinceAsync(since, ct);
        return trips.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> CreateAsync(CreateFishingTripRequest request, CancellationToken ct = default)
    {
        if (request.EndTime.HasValue && request.EndTime.Value <= request.StartTime)
            throw new BusinessRuleException("EndTime must be after StartTime.");

        var trip = new FishingTrip
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            LocationName = request.LocationName,
            WaterTemp = request.WaterTemp,
            WeatherDescription = request.WeatherDescription,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            EndTime = request.EndTime.HasValue
            ?   DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc)
                : null,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        await _repository.AddAsync(trip, ct);
        return MapToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse?> UpdateAsync(Guid id, UpdateFishingTripRequest request, CancellationToken ct = default)
    {
        if (request.EndTime.HasValue && request.EndTime.Value <= request.StartTime)
            throw new BusinessRuleException("EndTime must be after StartTime.");

        var trip = await _repository.GetByIdAsync(id, ct);
        if (trip is null)
            return null;

        trip.Name = request.Name;
        trip.LocationName = request.LocationName;
        trip.WaterTemp = request.WaterTemp;
        trip.WeatherDescription = request.WeatherDescription;
        trip.Latitude = request.Latitude;
        trip.Longitude = request.Longitude;
        trip.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        trip.EndTime = request.EndTime.HasValue
            ? DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc)
            : null;
        trip.Note = request.Note;
        trip.LastModified = DateTime.UtcNow;

        await _repository.UpdateAsync(trip, ct);
        return MapToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, ct);
        if (trip is null)
            return false;

        await _repository.DeleteAsync(id, ct);
        return true;
    }

    // -------------------------------------------------------------------------
    // Private mapping — keeps the service methods readable
    // -------------------------------------------------------------------------

    private static FishingTripResponse MapToResponse(FishingTrip t) => new(
        t.Id,
        t.Name,
        t.LocationName,
        t.WaterTemp,
        t.WeatherDescription,
        t.Latitude,
        t.Longitude,
        t.StartTime,
        t.EndTime,
        t.Note,
        t.CreatedAt,
        t.LastModified);
}
