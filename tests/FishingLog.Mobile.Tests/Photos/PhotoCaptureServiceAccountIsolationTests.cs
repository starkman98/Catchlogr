using FishingLog.Mobile.Data;
using FishingLog.Mobile.Services.Photos;
using FluentAssertions;
using Microsoft.Maui.Media;
using NSubstitute;

namespace FishingLog.Mobile.Tests.Photos;

/// <summary>Tests account boundaries for private locally captured photos.</summary>
public sealed class PhotoCaptureServiceAccountIsolationTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(
        Path.GetTempPath(),
        "FishingLog.Mobile.Tests",
        Guid.NewGuid().ToString("N"));

    /// <summary>Deletes temporary photo files created by a test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    /// <summary>Verifies that a photo belonging to the active account can be deleted.</summary>
    [Fact]
    public async Task DeleteAsync_ActiveAccountPhoto_DeletesFile()
    {
        var accountDirectory = Path.Combine(_rootDirectory, "first-account");
        var photoDirectory = Path.Combine(accountDirectory, "photos");
        Directory.CreateDirectory(photoDirectory);
        var photoPath = Path.Combine(photoDirectory, "catch.jpg");
        await File.WriteAllBytesAsync(
            photoPath,
            [1, 2, 3],
            TestContext.Current.CancellationToken);
        var sut = CreateService(accountDirectory);

        await sut.DeleteAsync(
            photoPath,
            TestContext.Current.CancellationToken);

        File.Exists(photoPath).Should().BeFalse();
    }

    /// <summary>Verifies that the active account cannot delete another account's photo.</summary>
    [Fact]
    public async Task DeleteAsync_OtherAccountPhoto_ThrowsAndPreservesFile()
    {
        var activeAccountDirectory = Path.Combine(_rootDirectory, "first-account");
        var otherPhotoDirectory = Path.Combine(
            _rootDirectory,
            "second-account",
            "photos");
        Directory.CreateDirectory(activeAccountDirectory);
        Directory.CreateDirectory(otherPhotoDirectory);
        var otherPhotoPath = Path.Combine(otherPhotoDirectory, "catch.jpg");
        await File.WriteAllBytesAsync(
            otherPhotoPath,
            [1, 2, 3],
            TestContext.Current.CancellationToken);
        var sut = CreateService(activeAccountDirectory);

        var action = () => sut.DeleteAsync(
            otherPhotoPath,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(otherPhotoPath).Should().BeTrue();
    }

    private static PhotoCaptureService CreateService(
        string activeAccountDirectory)
    {
        var accountStorage = Substitute.For<IAccountStorageContext>();
        accountStorage.ActiveAccountDirectory.Returns(activeAccountDirectory);

        return new PhotoCaptureService(
            Substitute.For<IMediaPicker>(),
            accountStorage);
    }
}
