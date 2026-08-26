using FishingLog.Infrastructure.Photos;
using FluentAssertions;

namespace FishingLog.Tests.Photos;

/// <summary>
/// Tests validation, storage, and deletion for local API photo storage.
/// </summary>
public sealed class LocalPhotoStorageTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "FishingLog.Tests",
        Guid.NewGuid().ToString("N"));

    /// <summary>Deletes test files created by an individual test run.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    /// <summary>Verifies that supported image bytes are stored and read by opaque key.</summary>
    [Fact]
    public async Task SaveAsync_Jpeg_StoresBytesWithGeneratedFileName()
    {
        var sut = new LocalPhotoStorage(_directory);
        byte[] bytes = [1, 2, 3, 4];
        await using var content = new MemoryStream(bytes);

        const string storageKey = "7a4ea1ef074d4ca18f36fb7996bca188";
        await sut.SaveAsync(
            storageKey,
            content,
            "image/jpeg",
            bytes.Length,
            TestContext.Current.CancellationToken);

        await using var stored = await sut.OpenReadAsync(
            storageKey,
            TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        using var buffer = new MemoryStream();
        await stored!.CopyToAsync(
            buffer,
            TestContext.Current.CancellationToken);
        var storedBytes = buffer.ToArray();
        storedBytes.Should().Equal(bytes);
    }

    /// <summary>Verifies that non-image content types are rejected.</summary>
    [Fact]
    public async Task SaveAsync_UnsupportedContentType_ThrowsArgumentException()
    {
        var sut = new LocalPhotoStorage(_directory);
        await using var content = new MemoryStream([1]);

        var action = () => sut.SaveAsync(
            "safe-storage-key",
            content,
            "text/plain",
            1,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Verifies that a stored photo can be deleted idempotently.</summary>
    [Fact]
    public async Task DeleteAsync_StoredPhoto_RemovesFile()
    {
        var sut = new LocalPhotoStorage(_directory);
        await using var content = new MemoryStream([1]);
        const string storageKey = "private-photo-key";
        await sut.SaveAsync(
            storageKey,
            content,
            "image/png",
            1,
            TestContext.Current.CancellationToken);

        await sut.DeleteAsync(storageKey, TestContext.Current.CancellationToken);
        await sut.DeleteAsync(storageKey, TestContext.Current.CancellationToken);

        File.Exists(Path.Combine(_directory, storageKey)).Should().BeFalse();
    }
}
