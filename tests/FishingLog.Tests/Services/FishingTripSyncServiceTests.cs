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

        _localRepo.GetAllAsync(Arg.Any<CancellationToken>())
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
    public async Task SyncAsync_NewLocalTrip_AppliesServerResponseAfterCreate()
    {
        var serverTrip = BuildServerTrip();
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { BuildNewLocalTrip() });

        _apiClient.CreateAsync(Arg.Any<CreateFishingTripRequest>(), Arg.Any<CancellationToken>())
            .Returns(serverTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepo.Received(1).SaveFromServerAsync(
            Arg.Is<FishingTripLocalEntity>(trip =>
                trip.Id == 1
                && trip.ServerId == serverTrip.Id.ToString()
                && trip.Name == serverTrip.Name
                && trip.LastModifiedUtc == serverTrip.LastModified
                && !trip.IsDirty
                && !trip.IsDeleted
                && HasWeatherFrom(trip, serverTrip)),
            Arg.Any<CancellationToken>());
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
    public async Task SyncAsync_ExistingDirtyTrip_AppliesWeatherFromUpdateResponse()
    {
        var serverId = Guid.NewGuid();
        var localTrip = BuildExistingLocalTrip(serverId);
        var serverTrip = BuildServerTrip(serverId);
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { localTrip });
        _apiClient.UpdateAsync(
                serverId,
                Arg.Any<UpdateFishingTripRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(serverTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _localRepo.Received(1).SaveFromServerAsync(
            Arg.Is<FishingTripLocalEntity>(trip =>
                trip.Id == localTrip.Id
                && HasWeatherFrom(trip, serverTrip)),
            Arg.Any<CancellationToken>());
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
            .SaveFromServerAsync(
                Arg.Is<FishingTripLocalEntity>(trip =>
                    HasWeatherFrom(trip, serverTrip)),
                Arg.Any<CancellationToken>());
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
            .SaveFromServerAsync(
                Arg.Is<FishingTripLocalEntity>(trip =>
                    HasWeatherFrom(trip, serverTrip)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_DownloadSucceeds_UsesNewestServerTimestampAsCursor()
    {
        var olderTimestamp = new DateTime(
            2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);
        var newerTimestamp = olderTimestamp.AddMinutes(10);

        _apiClient.GetModifiedSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripResponse>
            {
                BuildServerTrip(lastModified: olderTimestamp),
                BuildServerTrip(lastModified: newerTimestamp)
            });

        _localRepo.GetByServerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((FishingTripLocalEntity?)null);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _syncRepo.Received(1)
            .SetLastSyncAsync(
                SyncEntityType.FishingTrip,
                newerTimestamp,
                Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task SyncAsync_CleanTripWithMissingWeather_RetriesAndAppliesResponse()
    {
        var candidate = BuildWeatherRetryCandidate();
        var serverId = Guid.Parse(candidate.ServerId!);
        var response = BuildServerTrip(serverId);
        _localRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { candidate });
        _localRepo.GetByServerIdAsync(serverId, Arg.Any<CancellationToken>())
            .Returns(candidate);
        _apiClient.RetryWeatherEnrichmentAsync(
                serverId,
                Arg.Any<CancellationToken>())
            .Returns(response);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.Received(1).RetryWeatherEnrichmentAsync(
            serverId,
            TestContext.Current.CancellationToken);
        await _localRepo.Received(1).SaveFromServerAsync(
            Arg.Is<FishingTripLocalEntity>(trip =>
                trip.Id == candidate.Id
                && HasWeatherFrom(trip, response)),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SyncAsync_NewTrip_DoesNotRetryWeatherDuringSameSync()
    {
        var newTrip = BuildNewLocalTrip();
        var serverResponse = BuildServerTripWithoutWeather();
        _localRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { newTrip });
        _localRepo.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { newTrip });
        _apiClient.CreateAsync(
                Arg.Any<CreateFishingTripRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(serverResponse);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.DidNotReceive().RetryWeatherEnrichmentAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_IneligibleTrips_DoNotRetryWeather()
    {
        var hasWeather = BuildWeatherRetryCandidate();
        hasWeather.WeatherSampleTimeUtc = DateTime.UtcNow;
        var isDirty = BuildWeatherRetryCandidate();
        isDirty.IsDirty = true;
        var missingCoordinates = BuildWeatherRetryCandidate();
        missingCoordinates.Latitude = null;

        _localRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity>
            {
                hasWeather,
                isDirty,
                missingCoordinates
            });

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.DidNotReceive().RetryWeatherEnrichmentAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_CandidateBecomesDirtyBeforeRetry_SkipsRetry()
    {
        var candidate = BuildWeatherRetryCandidate();
        var serverId = Guid.Parse(candidate.ServerId!);
        var dirtyTrip = BuildWeatherRetryCandidate(serverId);
        dirtyTrip.IsDirty = true;
        _localRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { candidate });
        _localRepo.GetByServerIdAsync(serverId, Arg.Any<CancellationToken>())
            .Returns(dirtyTrip);

        await _sut.SyncAsync(TestContext.Current.CancellationToken);

        await _apiClient.DidNotReceive().RetryWeatherEnrichmentAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_NetworkErrorDuringWeatherRetry_DoesNotFailSync()
    {
        var candidate = BuildWeatherRetryCandidate();
        var serverId = Guid.Parse(candidate.ServerId!);
        _localRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FishingTripLocalEntity> { candidate });
        _localRepo.GetByServerIdAsync(serverId, Arg.Any<CancellationToken>())
            .Returns(candidate);
        _apiClient.RetryWeatherEnrichmentAsync(
                serverId,
                Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Network unavailable"));

        var action = () => _sut.SyncAsync(TestContext.Current.CancellationToken);

        await action.Should().NotThrowAsync();
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

    private static FishingTripLocalEntity BuildWeatherRetryCandidate(Guid? serverId = null) => new()
    {
        Id = 2,
        ServerId = (serverId ?? Guid.NewGuid()).ToString(),
        Name = "Missing weather",
        Latitude = 58.9,
        Longitude = 13.5,
        StartTime = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow,
        IsDirty = false,
        IsDeleted = false,
        WeatherSampleTimeUtc = null
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
        LastModified: lastModified ?? DateTime.UtcNow,
        AirTemperatureC: 14.2,
        WeatherCode: 2,
        WindSpeedMps: 3.1,
        WindDirectionDegrees: 225,
        PressureHpa: 1012.4,
        WeatherSampleTimeUtc: DateTime.UtcNow,
        WeatherProvider: "Open-Meteo",
        MoonPhase: "WaxingGibbous"
        );

    private static FishingTripResponse BuildServerTripWithoutWeather(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        Name: "Server trip without weather",
        LocationName: "Lake",
        WaterTemp: null,
        WeatherDescription: null,
        Latitude: 58.9,
        Longitude: 13.5,
        StartTime: DateTime.UtcNow,
        EndTime: null,
        Note: null,
        CreatedAt: DateTime.UtcNow,
        LastModified: DateTime.UtcNow,
        AirTemperatureC: null,
        WeatherCode: null,
        WindSpeedMps: null,
        WindDirectionDegrees: null,
        PressureHpa: null,
        WeatherSampleTimeUtc: null,
        WeatherProvider: null,
        MoonPhase: "WaxingGibbous");

    private static bool HasWeatherFrom(
        FishingTripLocalEntity local,
        FishingTripResponse remote) =>
        local.AirTemperatureC == remote.AirTemperatureC
        && local.WeatherCode == remote.WeatherCode
        && local.WindSpeedMps == remote.WindSpeedMps
        && local.WindDirectionDegrees == remote.WindDirectionDegrees
        && local.PressureHpa == remote.PressureHpa
        && local.WeatherSampleTimeUtc == remote.WeatherSampleTimeUtc
        && local.WeatherProvider == remote.WeatherProvider
        && local.MoonPhase == remote.MoonPhase;
}
