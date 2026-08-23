using Microsoft.Maui.Media;

namespace FishingLog.Mobile.Services.Photos;

/// <summary>
/// Uses .NET MAUI MediaPicker to capture photos into private app storage.
/// </summary>
public sealed class PhotoCaptureService : IPhotoCaptureService
{
    private readonly IMediaPicker _mediaPicker;

    /// <summary>Initializes a photo capture service with the platform media picker.</summary>
    public PhotoCaptureService(IMediaPicker mediaPicker)
    {
        _mediaPicker = mediaPicker;
    }

    /// <inheritdoc/>
    public async Task<string?> CaptureAsync(CancellationToken ct = default)
    {
        if (!_mediaPicker.IsCaptureSupported)
            throw new NotSupportedException("Photo capture is not supported on this device.");

        var photo = await _mediaPicker.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Photograph catch"
        });

        if (photo is null)
            return null;

        ct.ThrowIfCancellationRequested();

        var photoDirectory = Path.Combine(FileSystem.AppDataDirectory, "catch-photos");
        Directory.CreateDirectory(photoDirectory);

        var extension = NormalizeExtension(Path.GetExtension(photo.FileName));
        var localPath = Path.Combine(photoDirectory, $"{Guid.NewGuid():N}{extension}");

        await using var source = await photo.OpenReadAsync();
        await using var destination = new FileStream(
            localPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await source.CopyToAsync(destination, ct);

        return localPath;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string? localFilePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(localFilePath) && File.Exists(localFilePath))
            File.Delete(localFilePath);

        return Task.CompletedTask;
    }

    private static string NormalizeExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpeg" or ".jpg" => ".jpg",
        ".png" => ".png",
        ".heic" => ".heic",
        ".heif" => ".heif",
        _ => ".jpg"
    };
}
