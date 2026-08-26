namespace FishingLog.Domain.Entities;

/// <summary>
/// Represents private storage metadata for the single photo attached to a catch.
/// </summary>
public sealed class CatchPhoto
{
    /// <summary>Gets or sets the externally visible photo identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the catch that owns the photo.</summary>
    public Guid CatchId { get; set; }

    /// <summary>Gets or sets the opaque key used by the configured object storage.</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the validated media content type.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the stored size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets when the photo was created in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the owning catch navigation property.</summary>
    public Catch Catch { get; set; } = null!;
}
