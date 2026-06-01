using FishingLog.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FishingLog.Domain.ValueObjects;

/// <summary>
/// Represents the bait used during a catch, including optional details such as type, color, weight, and length.
/// </summary>
public class Bait
{
    /// <summary>Display name for the bait.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Type of the bait.</summary>
    public BaitType? Type { get; set; }

    /// <summary>Color of the bait.</summary>
    public string? Color { get; set; }

    /// <summary>Weight of the bait in grams.</summary>
    public int? WeightGrams { get; set; }

    /// <summary>Length of the bait in millimeters.</summary>
    public int? LengthMm { get; set; }
}
