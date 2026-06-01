namespace FishingLog.Contracts.FishingTripDTOs;

/// <summary>
/// Read model returned by the API for a fishing trip.
/// Used by both the API responses and the mobile API client.
/// </summary>
public record FishingTripResponse(
    Guid Id,
    string Name,
    string? LocationName,
    double? WaterTemp,
    string? WeatherDescription,
    double? Latitude,
    double? Longitude,
    DateTime StartTime,
    DateTime? EndTime,
    string? Note,
    DateTime CreatedAt,
    DateTime LastModified);
