using FishingLog.Mobile.Configuration;
using FishingLog.Mobile.Data;
using FishingLog.Mobile.Data.Repositories;
using FishingLog.Sync;
using FishingLog.Sync.Entities;
using FluentAssertions;

namespace FishingLog.Mobile.Tests.Data;

/// <summary>
/// Tests physical SQLite isolation between accounts using the same app installation.
/// </summary>
public sealed class LocalDatabaseAccountIsolationTests
{
    /// <summary>Verifies that local data and sync cursors remain private to each account.</summary>
    [Fact]
    public async Task ActivateAsync_DifferentAccounts_UsesIsolatedDataAndSyncMetadata()
    {
        var rootDirectory = CreateTemporaryRoot();
        var database = CreateDatabase(rootDirectory);
        var tripRepository = new FishingTripLocalRepository(database);
        var metadataRepository = new SyncMetadataRepository(database);
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var firstSyncUtc = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

        try
        {
            await database.ActivateAsync(
                firstUserId,
                TestContext.Current.CancellationToken);
            await tripRepository.AddAsync(
                new FishingTripLocalEntity { Name = "First user's trip" },
                TestContext.Current.CancellationToken);
            await metadataRepository.SetLastSyncAsync(
                SyncEntityType.FishingTrip,
                firstSyncUtc,
                TestContext.Current.CancellationToken);

            await database.ActivateAsync(
                secondUserId,
                TestContext.Current.CancellationToken);

            (await tripRepository.GetAllAsync(
                TestContext.Current.CancellationToken)).Should().BeEmpty();
            (await metadataRepository.GetLastSyncAsync(
                SyncEntityType.FishingTrip,
                TestContext.Current.CancellationToken)).Should().BeNull();

            await tripRepository.AddAsync(
                new FishingTripLocalEntity { Name = "Second user's trip" },
                TestContext.Current.CancellationToken);

            await database.ActivateAsync(
                firstUserId,
                TestContext.Current.CancellationToken);

            var firstUserTrips = await tripRepository.GetAllAsync(
                TestContext.Current.CancellationToken);
            firstUserTrips.Should().ContainSingle()
                .Which.Name.Should().Be("First user's trip");
            (await metadataRepository.GetLastSyncAsync(
                SyncEntityType.FishingTrip,
                TestContext.Current.CancellationToken)).Should().Be(firstSyncUtc);

            Directory.Exists(Path.Combine(
                rootDirectory,
                "accounts",
                firstUserId.ToString("N"))).Should().BeTrue();
            Directory.Exists(Path.Combine(
                rootDirectory,
                "accounts",
                secondUserId.ToString("N"))).Should().BeTrue();
        }
        finally
        {
            await database.CloseAsync(
                TestContext.Current.CancellationToken);
            DeleteTemporaryRoot(rootDirectory);
        }
    }

    /// <summary>Verifies that repositories cannot access storage while signed out.</summary>
    [Fact]
    public void Connection_NoActiveAccount_ThrowsInvalidOperationException()
    {
        var rootDirectory = CreateTemporaryRoot();
        var database = CreateDatabase(rootDirectory);

        try
        {
            var action = () => _ = database.Connection;

            action.Should().Throw<InvalidOperationException>();
            database.ActiveUserId.Should().BeNull();
        }
        finally
        {
            DeleteTemporaryRoot(rootDirectory);
        }
    }

    private static LocalDatabase CreateDatabase(string rootDirectory)
    {
        return new LocalDatabase(new DatabaseSettings
        {
            RootDirectory = rootDirectory,
            FileName = "fishinglog.db3"
        });
    }

    private static string CreateTemporaryRoot()
    {
        var rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "FishingLog.Mobile.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        return rootDirectory;
    }

    private static void DeleteTemporaryRoot(string rootDirectory)
    {
        if (Directory.Exists(rootDirectory))
            Directory.Delete(rootDirectory, recursive: true);
    }
}
