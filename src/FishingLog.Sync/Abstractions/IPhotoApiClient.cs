namespace FishingLog.Sync.Abstractions;

/// <summary>
/// Uploads and deletes catch photos through the FishingLog API.
/// </summary>
public interface IPhotoApiClient
{
    /// <summary>Uploads a local image file and returns its public server URL.</summary>
    Task<string> UploadAsync(string localFilePath, CancellationToken ct = default);

    /// <summary>Deletes the server photo represented by a public photo URL.</summary>
    Task DeleteAsync(string photoUrl, CancellationToken ct = default);
}
