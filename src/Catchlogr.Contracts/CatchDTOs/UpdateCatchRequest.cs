namespace Catchlogr.Contracts.CatchDTOs;

/// <summary>
/// Request model for updating an existing catch.
/// Sent to PUT /api/catches/{catchId} — full replacement (all fields required).
/// </summary>
public record UpdateCatchRequest(
    string Species,
    int? Length,
    int? Weight,
    string? PhotoUrl,
    string? Note,
    DateTime CaughtAt,
    double? Depth,
    double? Latitude,
    double? Longitude,
    BaitDto? Bait
    );
