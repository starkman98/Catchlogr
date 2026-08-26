using FishingLog.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace FishingLog.Domain.Entities;

/// <summary>
/// Represents a single catch.
/// This is the server-side system-of-record entity stored in PostgreSQL.
/// </summary>
public class Catch
{
    /// <summary>Unique identifier for the catch.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Unique identifier for the trip of the catch.</summary>
    [Required]
    public Guid TripId { get; set; }

    /// <summary>The name of the species (Required).</summary>
    [Required]
    public string Species { get; set; } = string.Empty;

    /// <summary>Length in centimeters of the catch.</summary>
    public int? Length { get; set; }

    /// <summary>Weight in grams of the catch.</summary>
    public int? Weight { get; set; }

    /// <summary>An URL to a photo of the catch.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Private photo metadata associated with this catch.</summary>
    public CatchPhoto? Photo { get; set; }

    /// <summary>Free-text note about the catch.</summary>
    public string? Note { get; set; }

    /// <summary>When this was caught (UTC).</summary>
    public DateTime CaughtAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last modified (UTC).
    /// Indexed in the database — used as the sync cursor by the mobile app.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>The water depth at the catch position.</summary>
    public double? Depth { get; set; }

    /// <summary>GPS latitude of the fishing location.</summary>
    public double? Latitude { get; set; }

    /// <summary>GPS longitude of the fishing location.</summary>
    public double? Longitude { get; set; }

    /// <summary>The bait of the catch.</summary>
    public Bait? Bait { get; set; }
}
