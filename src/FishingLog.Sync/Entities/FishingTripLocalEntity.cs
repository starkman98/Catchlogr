using SQLite;

namespace FishingLog.Sync.Entities;

public class FishingTripLocalEntity
{
    // -------------------------
    // Local database identity
    // -------------------------

    /// <summary>Local auto-increment primary key. Never sent to the server.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // -------------------------
    // Sync metadata columns
    // -------------------------

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

    // -------------------------
    // Fishing trip data
    // Mirrors FishingLog.Domain.Entities.FishingTrip
    // -------------------------

    /// <summary>Display name of the trip.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable location name.</summary>
    public string? LocationName { get; set; }

    /// <summary>Water temperature at the time of the trip.</summary>
    public double? WaterTemp { get; set; }

    /// <summary>Free-text weather description.</summary>
    public string? WeatherDescription { get; set; }

    /// <summary>GPS latitude.</summary>
    public double? Latitude { get; set; }

    /// <summary>GPS longitude.</summary>
    public double? Longitude { get; set; }

    /// <summary>When the trip started (UTC).</summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>When the trip ended (UTC). Null if still ongoing.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Free-text notes about the trip.</summary>
    public string? Note { get; set; }

    /// <summary>When this record was first created locally (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
