namespace FishingLog.Contracts.CatchDTOs;

/// <summary>
/// Represents bait details within a catch request or response.
/// </summary>
public record BaitDto(
    string Name,
    BaitType? Type,
    string? Color,
    int? WeightGrams,
    int? LengthMm
    );
