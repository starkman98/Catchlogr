namespace FishingLog.Contracts.FishingTripDTOs;

/// <summary>
/// Request model for creating a new fishing trip.
/// Sent from the mobile app (or any API client) to POST /api/fishing-trips.
/// </summary>
public record CreateFishingTripRequest(
    string Name,
    string? LocationName,
    double? WaterTemp,
    string? WeatherDescription,
    double? Latitude,
    double? Longitude,
    DateTime StartTime,
    DateTime? EndTime,
    string? Note);
