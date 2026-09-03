namespace Catchlogr.Contracts.CatchDTOs;

/// <summary>
/// Read model returned by the API for a catch.
/// Used by both the API responses and the mobile API client.
/// </summary>
public record CatchResponse(
    Guid Id,
    Guid FishingTripId,
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