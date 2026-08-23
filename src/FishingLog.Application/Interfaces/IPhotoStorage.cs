namespace FishingLog.Application.Interfaces;

/// <summary>
/// Stores and deletes public catch photos without exposing a specific storage provider.
/// </summary>
public interface IPhotoStorage
{
    /// <summary>Stores an image stream and returns its generated file name.</summary>
    Task<string> SaveAsync(
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default);

    /// <summary>Deletes a stored photo by its generated file name.</summary>
    Task DeleteAsync(string fileName, CancellationToken ct = default);
}
