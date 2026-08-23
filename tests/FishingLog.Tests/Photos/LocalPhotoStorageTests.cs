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

    /// <summary>Verifies that supported image bytes are stored under a generated safe name.</summary>
    [Fact]
    public async Task SaveAsync_Jpeg_StoresBytesWithGeneratedFileName()
    {
        var sut = new LocalPhotoStorage(_directory);
        byte[] bytes = [1, 2, 3, 4];
        await using var content = new MemoryStream(bytes);

        var fileName = await sut.SaveAsync(
            content,
            "image/jpeg",
            bytes.Length,
            TestContext.Current.CancellationToken);

        fileName.Should().EndWith(".jpg");
        fileName.Should().NotContain("..");
        var storedBytes = await File.ReadAllBytesAsync(
            Path.Combine(_directory, fileName),
            TestContext.Current.CancellationToken);
        storedBytes.Should().Equal(bytes);
    }

    /// <summary>Verifies that non-image content types are rejected.</summary>
    [Fact]
    public async Task SaveAsync_UnsupportedContentType_ThrowsArgumentException()
    {
        var sut = new LocalPhotoStorage(_directory);
        await using var content = new MemoryStream([1]);

        var action = () => sut.SaveAsync(
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
        var fileName = await sut.SaveAsync(
            content,
            "image/png",
            1,
            TestContext.Current.CancellationToken);

        await sut.DeleteAsync(fileName, TestContext.Current.CancellationToken);
        await sut.DeleteAsync(fileName, TestContext.Current.CancellationToken);

        File.Exists(Path.Combine(_directory, fileName)).Should().BeFalse();
    }
}
