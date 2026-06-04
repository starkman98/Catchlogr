using SQLite;

namespace FishingLog.Sync.Entities;

public class CatchLocalEntity
{
    /// <summary>Local auto-increment primary key. Never sent to the server.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// The server's GUID for this record.
    /// Null until the record has been synced at least once.
    /// Stored as a string because sqlite-net-pcl handles Guid as string internally.
    /// </summary>
    [Indexed]
    public string? ServerId { get; set; }

    /// <summary>UTC timestamp of the last modification. Used as the sync cursor.</summary>
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>True when this record has local changes that have not been uploaded yet.</summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>True when this record has been soft-deleted locally.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Unique local identifier for the trip of the catch.</summary>
    [Indexed]
    public int FishingTripLocalId { get; set; }

    /// <summary>
    /// Unique server identifier for the trip of the catch.
    /// Null until the parent trip has been synced to the server.
    /// </summary>
    [Indexed]
    public string? FishingTripServerId { get; set; }

    /// <summary>The name of the species (Required).</summary>
    public string Species { get; set; } = string.Empty;

    /// <summary>Length in centimeters of the catch.</summary>
    public int? Length { get; set; }

    /// <summary>Weight in grams of the catch.</summary>
    public int? Weight { get; set; }

    /// <summary>An URL to a photo of the catch.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Free-text note about the catch.</summary>
    public string? Note { get; set; }

    /// <summary>When this was caught (UTC).</summary>
    public DateTime CaughtAt { get; set; } = DateTime.UtcNow;

    /// <summary>The water depth at the catch position.</summary>
    public double? Depth { get; set; }

    /// <summary>GPS latitude of the fishing location.</summary>
    public double? Latitude { get; set; }

    /// <summary>GPS longitude of the fishing location.</summary>
    public double? Longitude { get; set; }

    /// <summary>The bait name of the catch.</summary>
    public string? BaitName { get; set; }

    /// <summary>The bait type of the catch.</summary>
    public string? BaitType { get; set; }

    /// <summary>The bait color of the catch.</summary>
    public string? BaitColor { get; set; }

    /// <summary>The bait weight of the catch.</summary>
    public int? BaitWeightGrams { get; set; }

    /// <summary>The bait length of the catch.</summary>
    public int? BaitLengthMm { get; set; }
}
