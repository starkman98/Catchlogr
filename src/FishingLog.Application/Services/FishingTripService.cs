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
        return trips.Select(MapFromTripToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Trip {id} not found.");
        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        if (since.Kind != DateTimeKind.Utc)
            since = DateTime.SpecifyKind(since, DateTimeKind.Utc);

        var trips = await _repository.GetModifiedSinceAsync(since, ct);
        return trips.Select(MapFromTripToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> CreateAsync(CreateFishingTripRequest request, CancellationToken ct = default)
    {
        if (request.EndTime.HasValue && request.EndTime.Value <= request.StartTime)
            throw new BusinessRuleException("EndTime must be after StartTime.");

        var trip = MapFromCreateToTrip(request);

        await _repository.AddAsync(trip, ct);
        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> UpdateAsync(Guid id, UpdateFishingTripRequest request, CancellationToken ct = default)
    {
        if (request.EndTime.HasValue && request.EndTime.Value <= request.StartTime)
            throw new BusinessRuleException("EndTime must be after StartTime.");

        var trip = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Trip {id} not found");

        ApplyUpdate(trip, request);

        await _repository.UpdateAsync(trip, ct);
        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Trip {id} not found.");

        await _repository.DeleteAsync(id, ct);
    }

    // -------------------------------------------------------------------------
    // Private mapping — keeps the service methods readable
    // -------------------------------------------------------------------------

    private static FishingTripResponse MapFromTripToResponse(FishingTrip t) => new(
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
        t.LastModified,
        t.AirTemperatureC,
        t.WeatherCode,
        t.WindSpeedMps,
        t.WindDirectionDegrees,
        t.PressureHpa,
        t.WeatherSampleTimeUtc,
        t.WeatherProvider);

    private static FishingTrip MapFromCreateToTrip(CreateFishingTripRequest request) => new()
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
            ? DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc)
                : null,
        Note = request.Note,
        CreatedAt = DateTime.UtcNow,
        LastModified = DateTime.UtcNow
    };

    private static void ApplyUpdate(FishingTrip existing, UpdateFishingTripRequest request)
    {
        existing.Name = request.Name;
        existing.LocationName = request.LocationName;
        existing.WaterTemp = request.WaterTemp;
        existing.WeatherDescription = request.WeatherDescription;
        existing.Latitude = request.Latitude;
        existing.Longitude = request.Longitude;
        existing.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        existing.EndTime = request.EndTime.HasValue
            ? DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc)
            : null;
        existing.Note = request.Note;
        existing.LastModified = DateTime.UtcNow;
    }
}
