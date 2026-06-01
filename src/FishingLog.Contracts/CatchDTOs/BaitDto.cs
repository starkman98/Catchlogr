namespace FishingLog.Contracts.CatchDTOs;

/// <summary>
/// Represents bait details within a catch request or response.
/// </summary>
public record BaitDto
{
    public string Name { get; init; } = string.Empty;
    public BaitType? Type { get; init; }
    public string? Color { get; init; }
    public int? WeightGrams { get; init; }
    public int? LengthMm { get; init; }
}
