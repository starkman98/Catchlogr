namespace Catchlogr.Application.Interfaces;

/// <summary>
/// Stores private photo objects behind opaque storage keys.
/// </summary>
public interface IPhotoObjectStorage
{
    /// <summary>Stores an image under the supplied opaque key.</summary>
    Task SaveAsync(string storageKey, Stream content, string contentType, long contentLength, CancellationToken ct = default);

    /// <summary>Opens a private object for reading, or returns null when absent.</summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);

    /// <summary>Deletes a private object when present.</summary>
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
