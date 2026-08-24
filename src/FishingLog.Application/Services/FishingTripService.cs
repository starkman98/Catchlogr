using FishingLog.Application.Exceptions;
using FishingLog.Application.Interfaces;
using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Domain.Entities;
using FishingLog.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FishingLog.Application.Services;

/// <summary>
/// Business logic service for fishing trips.
/// Maps between <see cref="FishingTrip"/> domain entities and response/request DTOs.
/// </summary>
public class FishingTripService : IFishingTripService
{
    private readonly IFishingTripRepository _repository;
    private readonly IWeatherService _weatherService;
    private readonly IMoonPhaseService _moonPhaseService;
    private readonly ILogger<FishingTripService> _logger;
    private readonly ICurrentUserContext _currentUserContext;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripService"/>.
    /// </summary>
    public FishingTripService(
        IFishingTripRepository repository,
        IWeatherService weatherService,
        IMoonPhaseService moonPhaseService,
        ILogger<FishingTripService> logger,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _weatherService = weatherService;
        _moonPhaseService = moonPhaseService;
        _logger = logger;
        _currentUserContext = currentUserContext;
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var trips = await _repository.GetAllAsync(_currentUserContext.UserId, ct);
        return trips.Select(MapFromTripToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, _currentUserContext.UserId, ct)
            ?? throw new NotFoundException($"Trip {id} not found.");
        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        if (since.Kind != DateTimeKind.Utc)
            since = DateTime.SpecifyKind(since, DateTimeKind.Utc);

        var trips = await _repository.GetModifiedSinceAsync(_currentUserContext.UserId, since, ct);
        return trips.Select(MapFromTripToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> CreateAsync(CreateFishingTripRequest request, CancellationToken ct = default)
    {
        if (request.EndTime.HasValue && request.EndTime.Value <= request.StartTime)
            throw new BusinessRuleException("EndTime must be after StartTime.");

        var trip = MapFromCreateToTrip(request);
        trip.UserId = _currentUserContext.UserId;
        trip.MoonPhase = _moonPhaseService.Calculate(trip.StartTime);

        await TryEnrichWeatherAsync(trip, ct);

        await _repository.AddAsync(trip, ct);
        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> UpdateAsync(Guid id, UpdateFishingTripRequest request, CancellationToken ct = default)
    {
        if (request.EndTime.HasValue && request.EndTime.Value <= request.StartTime)
            throw new BusinessRuleException("EndTime must be after StartTime.");

        var trip = await _repository.GetByIdAsync(id, _currentUserContext.UserId, ct)
            ?? throw new NotFoundException($"Trip {id} not found");

        var requestStartTimeUtc = DateTime.SpecifyKind(
            request.StartTime, DateTimeKind.Utc);

        var weatherInputsChanged =
            trip.Latitude != request.Latitude ||
            trip.Longitude != request.Longitude ||
            trip.StartTime != requestStartTimeUtc;
        var shouldRecalculateMoonPhase =
            trip.MoonPhase.HasValue &&
            trip.StartTime != requestStartTimeUtc;

        ApplyUpdate(trip, request);

        if (shouldRecalculateMoonPhase)
            trip.MoonPhase = _moonPhaseService.Calculate(trip.StartTime);

        if (weatherInputsChanged)
            ClearWeather(trip);

        if (trip.WeatherSampleTimeUtc is null)
            await TryEnrichWeatherAsync(trip, ct);
        
        await _repository.UpdateAsync(trip, ct);
        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse> RetryWeatherEnrichmentAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, _currentUserContext.UserId, ct)
            ?? throw new NotFoundException($"Trip {id} not found.");

        if (trip.WeatherSampleTimeUtc is not null)
            return MapFromTripToResponse(trip);

        var enriched = await TryEnrichWeatherAsync(trip, ct);
        if (enriched)
        {
            trip.LastModified = DateTime.UtcNow;
            await _repository.UpdateAsync(trip, ct);
        }

        return MapFromTripToResponse(trip);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await _repository.GetByIdAsync(id, _currentUserContext.UserId, ct)
            ?? throw new NotFoundException($"Trip {id} not found.");

        await _repository.DeleteAsync(id, _currentUserContext.UserId, ct);
    }

    private async Task<bool> TryEnrichWeatherAsync(
        FishingTrip trip,
        CancellationToken ct = default)
    {
        if (trip.Latitude is null || trip.Longitude is null)
            return false;

        try
        {
            var weather = await _weatherService.GetWeatherAsync(trip.Latitude.Value, trip.Longitude.Value, trip.StartTime, ct);
            if (weather is null)
                return false;

            trip.AirTemperatureC = weather.AirTemperatureC;
            trip.WeatherCode = weather.WeatherCode;
            trip.WindSpeedMps = weather.WindSpeedMps;
            trip.WindDirectionDegrees = weather.WindDirectionDegrees;
            trip.PressureHpa = weather.PressureHpa;
            trip.WeatherSampleTimeUtc = weather.WeatherSampleTimeUtc;
            trip.WeatherProvider = weather.WeatherProvider;
            return true;
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
            when (ex is HttpRequestException
                or OperationCanceledException
                or TimeoutException
                or JsonException
                or NotSupportedException)
        {
            _logger.LogWarning(
                "Failed to enrich weather for trip {TripId}. Error type {ErrorType}",
                trip.Id,
                ex.GetType().Name);
            return false;
        }
    }

    private static void ClearWeather(FishingTrip trip)
    {
        trip.AirTemperatureC = null;
        trip.WeatherCode = null;
        trip.WindSpeedMps = null;
        trip.WindDirectionDegrees = null;
        trip.PressureHpa = null;
        trip.WeatherSampleTimeUtc = null;
        trip.WeatherProvider = null;
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
        t.WeatherProvider,
        t.MoonPhase?.ToString());

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
