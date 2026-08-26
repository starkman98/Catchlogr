using FishingLog.Application.Interfaces;

namespace FishingLog.Infrastructure.Photos;

/// <summary>
/// Stores catch photos in a dedicated directory on the API host.
/// </summary>
public sealed class LocalPhotoStorage : IPhotoObjectStorage
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
    public async Task SaveAsync(
        string storageKey,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ValidateStorageKey(storageKey);

        if (contentLength <= 0 || contentLength > MaxPhotoSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(contentLength), "Photo size must be between 1 byte and 10 MiB.");

        if (!AllowedContentTypes.ContainsKey(contentType))
            throw new ArgumentException("Only JPEG, PNG, HEIC, and HEIF images are supported.", nameof(contentType));

        var destinationPath = Path.Combine(_storageDirectory, storageKey);

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
        }
        catch
        {
            await destination.DisposeAsync();
            File.Delete(destinationPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateStorageKey(storageKey);
        var path = Path.Combine(_storageDirectory, storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ValidateStorageKey(storageKey);
        var path = Path.Combine(_storageDirectory, storageKey);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private static void ValidateStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey != Path.GetFileName(storageKey))
        {
            throw new ArgumentException(
                "A valid opaque storage key is required.",
                nameof(storageKey));
        }
    }
}
