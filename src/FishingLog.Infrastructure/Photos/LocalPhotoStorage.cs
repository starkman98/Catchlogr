using FishingLog.Application.Interfaces;

namespace FishingLog.Infrastructure.Photos;

/// <summary>
/// Stores catch photos in a dedicated directory on the API host.
/// </summary>
public sealed class LocalPhotoStorage : IPhotoStorage
{
    /// <summary>Maximum accepted photo size in bytes (10 MiB).</summary>
    public const long MaxPhotoSizeBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/heic"] = ".heic",
            ["image/heif"] = ".heif"
        };

    private readonly string _storageDirectory;

    /// <summary>
    /// Initializes local photo storage in the supplied absolute directory.
    /// </summary>
    public LocalPhotoStorage(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _storageDirectory = Path.GetFullPath(storageDirectory);
        Directory.CreateDirectory(_storageDirectory);
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (contentLength <= 0 || contentLength > MaxPhotoSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(contentLength), "Photo size must be between 1 byte and 10 MiB.");

        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
            throw new ArgumentException("Only JPEG, PNG, HEIC, and HEIF images are supported.", nameof(contentType));

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_storageDirectory, fileName);

        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        try
        {
            await content.CopyToAsync(destination, ct);
            return fileName;
        }
        catch
        {
            await destination.DisposeAsync();
            File.Delete(destinationPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return Task.CompletedTask;

        var path = Path.Combine(_storageDirectory, fileName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
