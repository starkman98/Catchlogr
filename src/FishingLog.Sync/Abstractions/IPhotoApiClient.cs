namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Uploads, downloads and deletes private catch photos through the FishingLog API.
/// </summary>
public interface IPhotoApiClient
{
    /// <summary>Uploads a local image for an existing server catch and returns its protected API URL.</summary>
    Task<string> UploadAsync(Guid catchId, string localFilePath, CancellationToken ct = default);

    /// <summary>Downloads a protected photo to private account storage and returns its local path.</summary>
    Task<string> DownloadAsync(string photoUrl, CancellationToken ct = default);

    /// <summary>Deletes the server photo represented by its protected API URL.</summary>
    Task DeleteAsync(string photoUrl, CancellationToken ct = default);
}
