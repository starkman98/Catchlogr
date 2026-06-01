namespace FishingLog.Contracts.FishingTripDTOs;

/// <summary>
/// Request model for updating an existing fishing trip.
/// Sent to PUT /api/fishing-trips/{id} — full replacement (all fields required).
/// </summary>
public record UpdateFishingTripRequest(
    string Name,
    string? LocationName,
    double? WaterTemp,
    string? WeatherDescription,
    double? Latitude,
    double? Longitude,
    DateTime StartTime,
    DateTime? EndTime,
    string? Note);
