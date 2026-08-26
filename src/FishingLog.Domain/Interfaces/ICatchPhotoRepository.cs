using FishingLog.Domain.Entities;

namespace FishingLog.Domain.Interfaces;

/// <summary>
/// Provides ownership-scoped persistence for private catch-photo metadata.
/// </summary>
public interface ICatchPhotoRepository
{
    /// <summary>Returns a photo only when its catch belongs to the supplied user.</summary>
    Task<CatchPhoto?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>Returns the photo attached to an owned catch, when present.</summary>
    Task<CatchPhoto?> GetByCatchIdAsync(Guid catchId, Guid userId, CancellationToken ct = default);

    /// <summary>Atomically replaces photo metadata and updates its owning catch.</summary>
    Task ReplaceAsync(
        CatchPhoto? existing,
        CatchPhoto replacement,
        Catch owner,
        CancellationToken ct = default);

    /// <summary>Atomically removes photo metadata and updates its owning catch.</summary>
    Task DeleteAsync(
        CatchPhoto photo,
        Catch owner,
        CancellationToken ct = default);
}
