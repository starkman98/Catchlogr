namespace FishingLog.Contracts.CatchDTOs;

/// <summary>
/// Request model for creating a new catch.
/// Sent from the mobile app (or any API client) to POST /api/fishing-trips/{tripId}/catches.
/// </summary>
public record CreateCatchRequest(
    string Species,
    int? Length,
    int? Weight,
    string? PhotoUrl,
    string? Note,
    DateTime CaughtAt,
    DateTime LastModifiedAt,
    double? Depth,
    double? Latitude,
    double? Longitude,
    BaitDto? Bait
    );
