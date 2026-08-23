namespace FishingLog.Mobile.Services.Photos;

/// <summary>
/// Captures photos and manages the app's private local photo copies.
/// </summary>
public interface IPhotoCaptureService
{
    /// <summary>Opens the device camera and returns a private local file path, or null when cancelled.</summary>
    Task<string?> CaptureAsync(CancellationToken ct = default);

    /// <summary>Deletes a private local photo when it is no longer referenced.</summary>
    Task DeleteAsync(string? localFilePath, CancellationToken ct = default);
}
