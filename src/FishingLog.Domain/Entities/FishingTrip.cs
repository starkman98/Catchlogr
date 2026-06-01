using System.ComponentModel.DataAnnotations;

namespace FishingLog.Domain.Entities;

/// <summary>
/// Represents a single fishing trip.
/// This is the server-side system-of-record entity stored in PostgreSQL.
/// </summary>
public class FishingTrip
{
    /// <summary>Unique identifier for the trip.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name for the trip (required, max 200 chars).</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable location name, e.g. "Lake Vänern".</summary>
    public string? LocationName { get; set; }

    /// <summary>Water temperature in Celsius.</summary>
    public double? WaterTemp { get; set; }

    /// <summary>Free-text weather description, e.g. "Sunny, light wind".</summary>
    public string? WeatherDescription { get; set; }

    /// <summary>GPS latitude of the fishing location.</summary>
    public double? Latitude { get; set; }

    /// <summary>GPS longitude of the fishing location.</summary>
    public double? Longitude { get; set; }

    /// <summary>When the trip started (UTC).</summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>When the trip ended (UTC). Null if still ongoing.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Free-text notes about the trip.</summary>
    public string? Note { get; set; }

    /// <summary>When this record was first created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last modified (UTC).
    /// Indexed in the database — used as the sync cursor by the mobile app.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>The catches recorded during this trip.</summary>
    public ICollection<Catch> Catches { get; set; } = [];
}
