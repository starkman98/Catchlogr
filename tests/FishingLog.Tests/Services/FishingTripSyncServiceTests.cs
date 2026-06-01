using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Sync;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using FishingLog.Sync.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;

namespace FishingLog.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FishingTripSyncService"/>.
/// The repository is replaced with a fake (NSubstitute) so no database is needed.
/// </summary>
public class FishingTripSyncServiceTests
{
    private readonly IFishingTripLocalRepository _localRepo;
    private readonly ISyncMetadataRepository _syncRepo;
    private readonly IFishingTripApiClient _apiClient;

    private readonly FishingTripSyncService _sut;

    public FishingTripSyncServiceTests()
    {
        _localRepo = Substitute.For<IFishingTripLocalRepository>();
        _syncRepo = Substitute.For<ISyncMetadataRepository>();
        _apiClient = Substitute.For<IFishingTripApiClient>();

        _sut = new FishingTripSyncService(
            _localRepo,
            _syncRepo,
            _apiClient,
            NullLogger<FishingTripSyncService>.Instance
            );

        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity>());

        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripResponse>());

        _syncRepo.GetLastSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DateTime?)null);
    }

    [Fact]
    public async Task SyncAsync_NewLocalTrip_CallsCreateAsync()
    {
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { BuildNewLocalTrip() });

        _apiClient.CreateAsync(Arg.Any<CreateFishingTripRequest>(), Arg.Any<CancellationToken>())
            .Returns(BuildServerTrip());

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1)
            .CreateAsync(Arg.Any<CreateFishingTripRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_NewLocalTrip_MarksAsSyncedAfterCreate()
    {
        var serverTrip = BuildServerTrip();
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { BuildNewLocalTrip() });

        _apiClient.CreateAsync(Arg.Any<CreateFishingTripRequest>(), Arg.Any<CancellationToken>())
            .Returns(serverTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepo.Received(1)
            .MarkAsSyncedAsync(1, serverTrip.Id, serverTrip.LastModified, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_ExistingDirtyTrip_CallsUpdateAsync()
    {
        var serverId = Guid.NewGuid();
        var localTrip = BuildExistingLocalTrip(serverId);

        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { localTrip });

        _apiClient.UpdateAsync(serverId, Arg.Any<UpdateFishingTripRequest>(), Arg.Any<CancellationToken>())
            .Returns(BuildServerTrip(serverId));

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1)
            .UpdateAsync(serverId, Arg.Any<UpdateFishingTripRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_DeletedLocalTrip_CallsDeletedAndPermanentlyDeletes()
    {
        var serverId = Guid.NewGuid();
        var deletedTrip = BuildExistingLocalTrip(serverId);

        deletedTrip.IsDeleted = true;
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { deletedTrip });

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1).DeleteAsync(serverId, Arg.Any<CancellationToken>());
        await _localRepo.Received(1).PermanentlyDeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_NetworkErrorOnUpload_DoesNotThrow()
    {
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { BuildNewLocalTrip() });

        _apiClient.CreateAsync(Arg.Any<CreateFishingTripRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Network down"));

        await _sut.Invoking(s => s.SyncAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncAsync_FirstSync_UsesYearTwoThousandAsDefaultCursor()
    {
        _syncRepo.GetLastSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DateTime?)null);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1)
            .GetModifiedSinceAsync(Arg.Is<DateTime>(d => d.Year == 2000), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_RemoteTripNotInLocal_CallsSaveFromServer()
    {
        var serverTrip = BuildServerTrip();
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripResponse> { serverTrip });

        _localRepo.GetByServerIdAsync(serverTrip.Id, Arg.Any<CancellationToken>())
            .Returns((FishingTripLocalEntity?)null);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepo.Received(1)
            .SaveFromServerAsync(Arg.Any<FishingTripLocalEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_LocalDirtyAndNewer_DoesNotOverwriteLocal()
    {
        var serverId = Guid.NewGuid();
        var serverTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var localTime = serverTime.AddHours(1);

        var serverTrip = BuildServerTrip(serverId, serverTime);
        var localTrip = BuildExistingLocalTrip(serverId, localTime);

        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripResponse> { serverTrip });
        _localRepo.GetByServerIdAsync(serverId, Arg.Any<CancellationToken>())
            .Returns(localTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepo.DidNotReceive()
            .SaveFromServerAsync(Arg.Any<FishingTripLocalEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_ServerNewer_OverwriteLocal()
    {
        var serverId = Guid.NewGuid();
        var localTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var serverTime = localTime.AddHours(1);

        var serverTrip = BuildServerTrip(serverId, serverTime);
        var localTrip = BuildExistingLocalTrip(serverId, localTime);

        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripResponse> { serverTrip });

        _localRepo.GetByServerIdAsync(serverId, Arg.Any<CancellationToken>())
            .Returns(localTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepo.Received(1)
            .SaveFromServerAsync(Arg.Any<FishingTripLocalEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_DownloadSucceeds_AdvancesSyncCursor()
    {
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripResponse> { BuildServerTrip() });

        _localRepo.GetByServerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((FishingTripLocalEntity?)null);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _syncRepo.Received(1)
            .SetLastSyncAsync(SyncEntityType.FishingTrip, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_DownloadReturnsEmpty_DoesNotAdvanceCursor()
    {
        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _syncRepo.DidNotReceive()
            .SetLastSyncAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_NetworkErrorOnDownload_DoesNotThrow()
    {
        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Network down"));

        await _sut.Invoking(s => s.SyncAsync(TestContext.Current.CancellationToken))
            .Should().NotThrowAsync();
    }

    private static FishingTripLocalEntity BuildNewLocalTrip(string name = "Test trip") => new()
    {
        Id = 1,
        ServerId = null,
        Name = name,
        StartTime = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow,
        IsDirty = true,
        IsDeleted = false
    };

    private static FishingTripLocalEntity BuildExistingLocalTrip(Guid serverId, DateTime? lastModified = null) => new()
    {
        Id = 1,
        ServerId = serverId.ToString(),
        Name = "Existing Trip",
        StartTime = DateTime.UtcNow,
        LastModifiedUtc = lastModified ?? DateTime.UtcNow,
        IsDirty = true,
        IsDeleted = false
    };

    private static FishingTripResponse BuildServerTrip(Guid? id = null, DateTime? lastModified = null) => new(
        Id: id ?? Guid.NewGuid(),
        Name: "Server trip",
        LocationName: null,
        WaterTemp: null,
        WeatherDescription: null,
        Latitude: null,
        Longitude: null,
        StartTime: DateTime.UtcNow,
        EndTime: null,
        Note: null,
        CreatedAt: DateTime.UtcNow,
        LastModified: lastModified ?? DateTime.UtcNow
        );
}
