using FishingLog.Application.Photos;

namespace FishingLog.Application.Interfaces;

/// <summary>
/// Applies catch ownership rules to private photo storage operations.
/// </summary>
public interface IPhotoService
{
    /// <summary>Stores or replaces the photo on an owned catch.</summary>
    Task<Guid> UploadAsync(Guid catchId, Stream content, string contentType, long contentLength, CancellationToken ct = default);

    /// <summary>Opens an owned photo for download.</summary>
    Task<PhotoContent> OpenReadAsync(Guid photoId, CancellationToken ct = default);

    /// <summary>Deletes an owned photo.</summary>
    Task DeleteAsync(Guid photoId, CancellationToken ct = default);

    /// <summary>Deletes the photo attached to an owned catch, when present.</summary>
    Task DeleteForCatchAsync(Guid catchId, CancellationToken ct = default);
}
