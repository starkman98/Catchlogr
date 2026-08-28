using Catchlogr.Mobile.Data;
using Microsoft.Maui.Media;

namespace Catchlogr.Mobile.Services.Photos;

/// <summary>
/// Uses .NET MAUI MediaPicker to capture photos into private app storage.
/// </summary>
public sealed class PhotoCaptureService : IPhotoCaptureService
{
    private readonly IMediaPicker _mediaPicker;
    private readonly IAccountStorageContext _accountStorage;

    /// <summary>Initializes a photo capture service with the platform media picker.</summary>
    public PhotoCaptureService(
        IMediaPicker mediaPicker,
        IAccountStorageContext accountStorage)
    {
        _mediaPicker = mediaPicker;
        _accountStorage = accountStorage;
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

        var photoDirectory = GetPhotoDirectory();
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

        if (string.IsNullOrWhiteSpace(localFilePath))
            return Task.CompletedTask;

        var photoDirectory = Path.GetFullPath(
            GetPhotoDirectory())
            .TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(localFilePath);
        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(
                photoDirectory,
                pathComparison))
        {
            throw new InvalidOperationException(
                "The photo does not belong to the active account.");
        }

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private string GetPhotoDirectory()
        => Path.Combine(
            _accountStorage.ActiveAccountDirectory,
            "photos");

    private static string NormalizeExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpeg" or ".jpg" => ".jpg",
        ".png" => ".png",
        ".heic" => ".heic",
        ".heif" => ".heif",
        _ => ".jpg"
    };
}
