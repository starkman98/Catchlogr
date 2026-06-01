using FishingLog.Domain.Enums;

namespace FishingLog.Domain.ValueObjects;

/// <summary>
/// Represents the bait used during a catch, including optional details such as type, color, weight, and length.
/// </summary>
public record Bait(
    string Name,
    BaitType? Type,
    string? Color,
    int? WeightGrams,
    int? LengthMm
);
