using Catchlogr.Contracts.CatchDTOs;
using Catchlogr.Sync;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Entities;
using Catchlogr.Sync.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catchlogr.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CatchSyncService"/>.
/// </summary>
public class CatchSyncServiceTests
{
    private readonly ICatchLocalRepository _localRepository;
    private readonly IFishingTripLocalRepository _tripRepository;
    private readonly ISyncMetadataRepository _syncMetadata;
    private readonly ICatchApiClient _apiClient;
    private readonly IPhotoApiClient _photoApiClient;
    private readonly CatchSyncService _sut;

    /// <summary>
    /// Initializes isolated substitutes and default empty sync results for each test.
    /// </summary>
    public CatchSyncServiceTests()
    {
        _localRepository = Substitute.For<ICatchLocalRepository>();
        _tripRepository = Substitute.For<IFishingTripLocalRepository>();
        _syncMetadata = Substitute.For<ISyncMetadataRepository>();
        _apiClient = Substitute.For<ICatchApiClient>();
        _photoApiClient = Substitute.For<IPhotoApiClient>();

        _sut = new CatchSyncService(
            _localRepository,
            _tripRepository,
            _syncMetadata,
            _apiClient,
            _photoApiClient,
            NullLogger<CatchSyncService>.Instance);

        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([]);
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        _syncMetadata.GetLastSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DateTime?)null);
    }

    /// <summary>
    /// Verifies that a new local catch is created remotely and replaced with the complete server response.
    /// </summary>
    [Fact]
    public async Task SyncAsync_NewLocalCatch_CreatesAndSavesServerResponse()
    {
        var tripServerId = Guid.NewGuid();
        var localTrip = BuildLocalTrip(tripServerId);
        var localCatch = BuildNewLocalCatch(tripServerId);
        var serverCatch = BuildServerCatch(tripServerId);
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);
        _apiClient.CreateAsync(
                tripServerId,
                Arg.Any<CreateCatchRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(serverCatch);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(localTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1).CreateAsync(
            tripServerId,
            Arg.Is<CreateCatchRequest>(request =>
                request.Species == "Local perch"
                && request.Bait != null
                && request.Bait.Name == "Green jig"
                && request.Bait.Type == BaitType.Jig),
            Arg.Any<CancellationToken>());
        await _localRepository.Received(1).SaveFromServerAsync(
            Arg.Is<CatchLocalEntity>(saved =>
                saved.Id == localCatch.Id
                && saved.ServerId == serverCatch.Id.ToString()
                && saved.FishingTripLocalId == localTrip.Id
                && saved.Species == serverCatch.Species
                && saved.LastModifiedUtc == serverCatch.LastModifiedAt
                && !saved.IsDirty
                && !saved.IsDeleted),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a catch waits until its parent trip has a server identifier.
    /// </summary>
    [Fact]
    public async Task SyncAsync_NewLocalCatchWithUnsyncedTrip_DoesNotCreate()
    {
        var localCatch = BuildNewLocalCatch(null);
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);
        _tripRepository.GetByIdAsync(localCatch.FishingTripLocalId, Arg.Any<CancellationToken>())
            .Returns(new FishingTripLocalEntity { Id = localCatch.FishingTripLocalId });

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(),
            Arg.Any<CreateCatchRequest>(),
            Arg.Any<CancellationToken>());
        await _localRepository.DidNotReceive().SaveFromServerAsync(
            Arg.Any<CatchLocalEntity>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a pending local photo is uploaded and its URL is included when the catch is created.
    /// </summary>
    [Fact]
    public async Task SyncAsync_NewCatchWithPendingPhoto_CreatesCatchBeforeUploadingPhoto()
    {
        var tripServerId = Guid.NewGuid();
        var localTrip = BuildLocalTrip(tripServerId);
        var localCatch = BuildNewLocalCatch(tripServerId);
        localCatch.LocalPhotoPath = "local-catch.jpg";
        localCatch.IsPhotoUploadPending = true;
        const string uploadedUrl = "https://example.test/api/photos/10000000-0000-0000-0000-000000000001";
        var serverCatch = BuildServerCatch(tripServerId, photoUrl: uploadedUrl);

        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);
        _apiClient.CreateAsync(
                tripServerId,
                Arg.Is<CreateCatchRequest>(request => request.PhotoUrl == null),
                Arg.Any<CancellationToken>())
            .Returns(serverCatch);
        _photoApiClient.UploadAsync(
                serverCatch.Id,
                localCatch.LocalPhotoPath,
                Arg.Any<CancellationToken>())
            .Returns(uploadedUrl);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(localTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _photoApiClient.Received(1)
            .UploadAsync(
                serverCatch.Id,
                localCatch.LocalPhotoPath,
                Arg.Any<CancellationToken>());
        await _localRepository.Received().SaveFromServerAsync(
            Arg.Is<CatchLocalEntity>(saved =>
                saved.PhotoUrl == uploadedUrl
                && saved.LocalPhotoPath == "local-catch.jpg"
                && !saved.IsPhotoUploadPending
                && !saved.IsDirty),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an unowned photo is not uploaded when catch creation fails.
    /// </summary>
    [Fact]
    public async Task SyncAsync_CatchCreateFails_DoesNotUploadPhoto()
    {
        var tripServerId = Guid.NewGuid();
        var localCatch = BuildNewLocalCatch(tripServerId);
        localCatch.LocalPhotoPath = "local-catch.jpg";
        localCatch.IsPhotoUploadPending = true;
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);
        _apiClient.CreateAsync(
                tripServerId,
                Arg.Any<CreateCatchRequest>(),
                Arg.Any<CancellationToken>())
            .Returns((CatchResponse?)null);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);
        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _photoApiClient.DidNotReceive()
            .UploadAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        localCatch.PhotoUrl.Should().BeNull();
        localCatch.IsPhotoUploadPending.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an existing dirty catch is updated and the server response is saved locally.
    /// </summary>
    [Fact]
    public async Task SyncAsync_ExistingDirtyCatch_UpdatesAndSavesServerResponse()
    {
        var tripServerId = Guid.NewGuid();
        var catchServerId = Guid.NewGuid();
        var localTrip = BuildLocalTrip(tripServerId);
        var localCatch = BuildExistingLocalCatch(catchServerId, tripServerId);
        var serverCatch = BuildServerCatch(tripServerId, catchServerId, species: "Server pike");
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);
        _apiClient.UpdateAsync(
                catchServerId,
                Arg.Any<UpdateCatchRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(serverCatch);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(localTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1).UpdateAsync(
            catchServerId,
            Arg.Any<UpdateCatchRequest>(),
            Arg.Any<CancellationToken>());
        await _localRepository.Received(1).SaveFromServerAsync(
            Arg.Is<CatchLocalEntity>(saved =>
                saved.Id == localCatch.Id
                && saved.Species == "Server pike"
                && !saved.IsDirty),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a synced soft-deleted catch is deleted remotely and then locally.
    /// </summary>
    [Fact]
    public async Task SyncAsync_DeletedSyncedCatch_DeletesRemoteAndLocal()
    {
        var catchServerId = Guid.NewGuid();
        var localCatch = BuildExistingLocalCatch(catchServerId, Guid.NewGuid());
        localCatch.IsDeleted = true;
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1).DeleteAsync(catchServerId, Arg.Any<CancellationToken>());
        await _localRepository.Received(1)
            .PermanentlyDeleteAsync(localCatch.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that deleting a synced catch also deletes its uploaded server photo.
    /// </summary>
    [Fact]
    public async Task SyncAsync_DeletedSyncedCatch_DeletesServerPhoto()
    {
        var localCatch = BuildExistingLocalCatch(Guid.NewGuid(), Guid.NewGuid());
        localCatch.IsDeleted = true;
        localCatch.PhotoUrl = "https://example.test/api/photos/10000000-0000-0000-0000-000000000002";
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _photoApiClient.Received(1)
            .DeleteAsync(localCatch.PhotoUrl, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a malformed server identifier is removed locally without an API call.
    /// </summary>
    [Fact]
    public async Task SyncAsync_DeletedCatchWithInvalidServerId_DeletesOnlyLocal()
    {
        var localCatch = BuildExistingLocalCatch(Guid.NewGuid(), Guid.NewGuid());
        localCatch.ServerId = "invalid";
        localCatch.IsDeleted = true;
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>()).Returns([localCatch]);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _localRepository.Received(1)
            .PermanentlyDeleteAsync(localCatch.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an upload network error is deferred to a future sync.
    /// </summary>
    [Fact]
    public async Task SyncAsync_UploadNetworkError_DoesNotThrow()
    {
        var tripServerId = Guid.NewGuid();
        _localRepository.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns([BuildNewLocalCatch(tripServerId)]);
        _apiClient.CreateAsync(
                tripServerId,
                Arg.Any<CreateCatchRequest>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unavailable"));

        var action = () => _sut.SyncAsync(TestContext.Current.CancellationToken);

        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies the safe initial cursor used before any catch sync has completed.
    /// </summary>
    [Fact]
    public async Task SyncAsync_FirstSync_UsesYearTwoThousandCursor()
    {
        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1).GetModifiedSinceAsync(
            Arg.Is<DateTime>(date =>
                date == new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a new remote catch is mapped to its local parent trip and saved.
    /// </summary>
    [Fact]
    public async Task SyncAsync_NewRemoteCatch_SavesMappedLocalCatch()
    {
        var tripServerId = Guid.NewGuid();
        var localTrip = BuildLocalTrip(tripServerId);
        var serverCatch = BuildServerCatch(tripServerId);
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([serverCatch]);
        _localRepository.GetByServerIdAsync(serverCatch.Id, Arg.Any<CancellationToken>())
            .Returns((CatchLocalEntity?)null);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(localTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepository.Received(1).SaveFromServerAsync(
            Arg.Is<CatchLocalEntity>(saved =>
                saved.ServerId == serverCatch.Id.ToString()
                && saved.FishingTripLocalId == localTrip.Id
                && saved.FishingTripServerId == tripServerId.ToString()
                && saved.Species == serverCatch.Species
                && saved.BaitName == serverCatch.Bait!.Name
                && !saved.IsDirty),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a remote catch is skipped when its parent trip is unavailable locally.
    /// </summary>
    [Fact]
    public async Task SyncAsync_RemoteCatchWithoutLocalTrip_DoesNotSave()
    {
        var serverCatch = BuildServerCatch(Guid.NewGuid());
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([serverCatch]);
        _tripRepository.GetByServerIdAsync(serverCatch.FishingTripId, Arg.Any<CancellationToken>())
            .Returns((FishingTripLocalEntity?)null);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepository.DidNotReceive().SaveFromServerAsync(
            Arg.Any<CatchLocalEntity>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a newer dirty local catch wins a last-write-wins conflict.
    /// </summary>
    [Fact]
    public async Task SyncAsync_LocalDirtyAndNewer_DoesNotOverwriteLocal()
    {
        var tripServerId = Guid.NewGuid();
        var catchServerId = Guid.NewGuid();
        var serverTime = Utc(2026, 1, 1);
        var localCatch = BuildExistingLocalCatch(catchServerId, tripServerId, serverTime.AddHours(1));
        var serverCatch = BuildServerCatch(tripServerId, catchServerId, serverTime);
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([serverCatch]);
        _localRepository.GetByServerIdAsync(catchServerId, Arg.Any<CancellationToken>())
            .Returns(localCatch);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(BuildLocalTrip(tripServerId));

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepository.DidNotReceive().SaveFromServerAsync(
            Arg.Any<CatchLocalEntity>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a newer server catch replaces an older local catch.
    /// </summary>
    [Fact]
    public async Task SyncAsync_ServerNewer_OverwritesLocal()
    {
        var tripServerId = Guid.NewGuid();
        var catchServerId = Guid.NewGuid();
        var localTime = Utc(2026, 1, 1);
        var localCatch = BuildExistingLocalCatch(catchServerId, tripServerId, localTime);
        var serverCatch = BuildServerCatch(
            tripServerId,
            catchServerId,
            localTime.AddHours(1),
            "New server species");
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([serverCatch]);
        _localRepository.GetByServerIdAsync(catchServerId, Arg.Any<CancellationToken>())
            .Returns(localCatch);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(BuildLocalTrip(tripServerId));

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepository.Received(1).SaveFromServerAsync(
            Arg.Is<CatchLocalEntity>(saved =>
                saved.Id == localCatch.Id
                && saved.Species == "New server species"
                && saved.LastModifiedUtc == serverCatch.LastModifiedAt
                && !saved.IsDirty),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the cursor advances to the newest returned server timestamp.
    /// </summary>
    [Fact]
    public async Task SyncAsync_DownloadWithResults_AdvancesCursorToNewestTimestamp()
    {
        var tripServerId = Guid.NewGuid();
        var earlier = Utc(2026, 1, 1);
        var latest = earlier.AddDays(1);
        var catches = new List<CatchResponse>
        {
            BuildServerCatch(tripServerId, lastModified: latest),
            BuildServerCatch(tripServerId, lastModified: earlier)
        };
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(catches);
        _tripRepository.GetByServerIdAsync(tripServerId, Arg.Any<CancellationToken>())
            .Returns(BuildLocalTrip(tripServerId));

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _syncMetadata.Received(1).SetLastSyncAsync(
            SyncEntityType.Catch,
            latest,
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an empty download does not advance the catch cursor.
    /// </summary>
    [Fact]
    public async Task SyncAsync_EmptyDownload_DoesNotAdvanceCursor()
    {
        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _syncMetadata.DidNotReceive().SetLastSyncAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a download network error is deferred without throwing.
    /// </summary>
    [Fact]
    public async Task SyncAsync_DownloadNetworkError_DoesNotThrow()
    {
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unavailable"));

        var action = () => _sut.SyncAsync(TestContext.Current.CancellationToken);

        await action.Should().NotThrowAsync();
        await _syncMetadata.DidNotReceive().SetLastSyncAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private static CatchLocalEntity BuildNewLocalCatch(Guid? tripServerId) => new()
    {
        Id = 7,
        FishingTripLocalId = 42,
        FishingTripServerId = tripServerId?.ToString(),
        Species = "Local perch",
        Length = 31,
        Weight = 450,
        CaughtAt = Utc(2026, 1, 1),
        LastModifiedUtc = Utc(2026, 1, 1),
        IsDirty = true,
        BaitName = "Green jig",
        BaitType = BaitType.Jig.ToString(),
        BaitColor = "Green"
    };

    private static CatchLocalEntity BuildExistingLocalCatch(
        Guid catchServerId,
        Guid tripServerId,
        DateTime? lastModified = null) => new()
    {
        Id = 7,
        ServerId = catchServerId.ToString(),
        FishingTripLocalId = 42,
        FishingTripServerId = tripServerId.ToString(),
        Species = "Local perch",
        CaughtAt = Utc(2026, 1, 1),
        LastModifiedUtc = lastModified ?? Utc(2026, 1, 1),
        IsDirty = true
    };

    private static FishingTripLocalEntity BuildLocalTrip(Guid tripServerId) => new()
    {
        Id = 42,
        ServerId = tripServerId.ToString(),
        Name = "Server-backed trip",
        StartTime = Utc(2026, 1, 1),
        LastModifiedUtc = Utc(2026, 1, 1),
        IsDirty = false
    };

    private static CatchResponse BuildServerCatch(
        Guid tripServerId,
        Guid? catchServerId = null,
        DateTime? lastModified = null,
        string species = "Server perch",
        string? photoUrl = "https://example.test/catch.jpg") => new(
        catchServerId ?? Guid.NewGuid(),
        tripServerId,
        species,
        32,
        475,
        photoUrl,
        "Server note",
        Utc(2026, 1, 1),
        lastModified ?? Utc(2026, 1, 2),
        2.5,
        59.3,
        18.1,
        new BaitDto("Green jig", BaitType.Jig, "Green", 12, 80));

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
