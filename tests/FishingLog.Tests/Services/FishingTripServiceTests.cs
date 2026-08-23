using FishingLog.Application.Exceptions;
using FishingLog.Application.Interfaces;
using FishingLog.Application.Services;
using FishingLog.Application.Weather;
using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Domain.Entities;
using FishingLog.Domain.Enums;
using FishingLog.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FishingLog.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FishingTripService"/>.
/// The repository is replaced with a fake (NSubstitute) so no database is needed.
/// </summary>
public class FishingTripServiceTests
{
    // -----------------------------------------------------------------------
    // These are created once per test class and shared across all tests.
    // The fake repository lets us control what it "returns" in each test.
    // -----------------------------------------------------------------------
    private readonly IFishingTripRepository _repository;
    private readonly IWeatherService _weatherService;
    private readonly IMoonPhaseService _moonPhaseService;
    private readonly ILogger<FishingTripService> _logger;
    private readonly FishingTripService _sut; // sut = System Under Test

    public FishingTripServiceTests()
    {
        _repository = Substitute.For<IFishingTripRepository>();
        _weatherService = Substitute.For<IWeatherService>();
        _moonPhaseService = Substitute.For<IMoonPhaseService>();
        _moonPhaseService.Calculate(Arg.Any<DateTime>()).Returns(MoonPhase.FullMoon);
        _logger = Substitute.For<ILogger<FishingTripService>>();
        _sut = new FishingTripService(
            _repository,
            _weatherService,
            _moonPhaseService,
            _logger);
    }

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_Should_Return_Mapped_Responses()
    {
        // Arrange — tell the fake repo what to return
        var fakeTrips = new List<FishingTrip>
        {
            BuildTrip("Morning bass session"),
            BuildTrip("Evening pike trip")
        };
        _repository.GetAllAsync(TestContext.Current.CancellationToken).Returns(fakeTrips);

        // Act — call the real service method
        var result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert — check the output is correct
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Morning bass session");
        result[1].Name.Should().Be("Evening pike trip");
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_Should_Return_Response_When_Trip_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var fakeTrip = BuildTrip("Solo trip", id);
        _repository.GetByIdAsync(id, TestContext.Current.CancellationToken).Returns(fakeTrip);

        // Act
        var result = await _sut.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Solo trip");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Throw_When_Trip_Not_Found()
    {
        // Arrange — repo returns null (trip doesn't exist)
        _repository.GetByIdAsync(Arg.Any<Guid>(), TestContext.Current.CancellationToken).ReturnsNull();

        // Act & Assert
        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();   
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Should_Return_Response_With_New_Id()
    {
        // Arrange
        var request = new CreateFishingTripRequest(
            Name: "Test trip",
            StartTime: DateTime.UtcNow,
            EndTime: null,
            LocationName: "Lake A",
            Latitude: null,
            Longitude: null,
            WaterTemp: null,
            WeatherDescription: null,
            Note: null);

        // Act
        var result = await _sut.CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test trip");
        result.MoonPhase.Should().Be(nameof(MoonPhase.FullMoon));

        // Verify the repo's AddAsync was actually called once
        await _repository.Received(1).AddAsync(Arg.Any<FishingTrip>(), TestContext.Current.CancellationToken);
        _moonPhaseService.Received(1).Calculate(request.StartTime);
        await _weatherService.DidNotReceive().GetWeatherAsync(
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithCoordinates_AppliesWeatherSnapshot()
    {
        var startTime = Utc(2026, 8, 20, 10);
        var snapshot = BuildWeatherSnapshot();
        var request = BuildCreateRequest(startTime, 58.9, 13.5);

        _weatherService.GetWeatherAsync(
                58.9,
                13.5,
                startTime,
                TestContext.Current.CancellationToken)
            .Returns(snapshot);

        var result = await _sut.CreateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.AirTemperatureC.Should().Be(snapshot.AirTemperatureC);
        result.WeatherCode.Should().Be(snapshot.WeatherCode);
        result.WindSpeedMps.Should().Be(snapshot.WindSpeedMps);
        result.WindDirectionDegrees.Should().Be(snapshot.WindDirectionDegrees);
        result.PressureHpa.Should().Be(snapshot.PressureHpa);
        result.WeatherSampleTimeUtc.Should().Be(snapshot.WeatherSampleTimeUtc);
        result.WeatherProvider.Should().Be(snapshot.WeatherProvider);

        await _repository.Received(1).AddAsync(
            Arg.Is<FishingTrip>(trip =>
                trip.AirTemperatureC == snapshot.AirTemperatureC
                && trip.WeatherSampleTimeUtc == snapshot.WeatherSampleTimeUtc),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateAsync_WhenWeatherProviderFails_StillSavesTrip()
    {
        var startTime = Utc(2026, 8, 20, 10);
        var request = BuildCreateRequest(startTime, 58.9, 13.5);

        _weatherService.GetWeatherAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<WeatherSnapshot?>(
                new HttpRequestException("Provider unavailable")));

        var result = await _sut.CreateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.WeatherSampleTimeUtc.Should().BeNull();
        await _repository.Received(1).AddAsync(
            Arg.Any<FishingTrip>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = BuildCreateRequest(Utc(2026, 8, 20, 10), 58.9, 13.5);

        _weatherService.GetWeatherAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<DateTime>(),
                cts.Token)
            .Returns(_ => Task.FromException<WeatherSnapshot?>(
                new OperationCanceledException(cts.Token)));

        var action = () => _sut.CreateAsync(request, cts.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<FishingTrip>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Should_Throw_When_EndTime_Before_StartTime()
    {
        // Arrange — EndTime is before StartTime, which is invalid
        var start = DateTime.UtcNow;
        var request = new CreateFishingTripRequest(
            Name: "Bad trip",
            StartTime: start,
            EndTime: start.AddHours(-1), // ← invalid!
            LocationName: null,
            Latitude: null,
            Longitude: null,
            WaterTemp: null,
            WeatherDescription: null,
            Note: null);

        // Act & Assert — expect a BusinessRuleException to be thrown
        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Trip_Not_Found()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), TestContext.Current.CancellationToken).ReturnsNull();
        var request = BuildUpdateRequest();

        // Act & Assert
        await _sut.Invoking(s => s.UpdateAsync(Guid.NewGuid(), request, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<FishingTrip>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_And_Return_Response()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, TestContext.Current.CancellationToken).Returns(BuildTrip("Old name", id));
        var request = BuildUpdateRequest("New name");

        // Act
        var result = await _sut.UpdateAsync(id, request, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New name");
        await _repository.Received(1).UpdateAsync(Arg.Any<FishingTrip>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_ChangingNotesOnly_DoesNotRefetchWeather()
    {
        var trip = BuildTripWithWeather();
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        var request = BuildMatchingUpdateRequest(trip, note: "Updated notes");

        await _sut.UpdateAsync(
            trip.Id,
            request,
            TestContext.Current.CancellationToken);

        await _weatherService.DidNotReceive().GetWeatherAsync(
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ChangingCoordinates_RefetchesWeather()
    {
        var trip = BuildTripWithWeather();
        var snapshot = BuildWeatherSnapshot();
        const double newLatitude = 59.1;
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        _weatherService.GetWeatherAsync(
                newLatitude,
                trip.Longitude!.Value,
                trip.StartTime,
                TestContext.Current.CancellationToken)
            .Returns(snapshot);
        var request = BuildMatchingUpdateRequest(trip) with
        {
            Latitude = newLatitude
        };

        var result = await _sut.UpdateAsync(
            trip.Id,
            request,
            TestContext.Current.CancellationToken);

        result.WeatherSampleTimeUtc.Should().Be(snapshot.WeatherSampleTimeUtc);
        await _weatherService.Received(1).GetWeatherAsync(
            newLatitude,
            trip.Longitude.Value,
            trip.StartTime,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_ChangingStartTime_RefetchesWeather()
    {
        var trip = BuildTripWithWeather();
        var snapshot = BuildWeatherSnapshot();
        var newStartTime = trip.StartTime.AddHours(1);
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        _weatherService.GetWeatherAsync(
                trip.Latitude!.Value,
                trip.Longitude!.Value,
                newStartTime,
                TestContext.Current.CancellationToken)
            .Returns(snapshot);
        var request = BuildMatchingUpdateRequest(trip) with
        {
            StartTime = newStartTime
        };

        await _sut.UpdateAsync(
            trip.Id,
            request,
            TestContext.Current.CancellationToken);

        await _weatherService.Received(1).GetWeatherAsync(
            trip.Latitude.Value,
            trip.Longitude.Value,
            newStartTime,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_ChangingStartTime_RecalculatesExistingMoonPhase()
    {
        var trip = BuildTripWithWeather();
        trip.MoonPhase = MoonPhase.FirstQuarter;
        var newStartTime = trip.StartTime.AddDays(4);
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        _moonPhaseService.Calculate(newStartTime).Returns(MoonPhase.FullMoon);
        var request = BuildMatchingUpdateRequest(trip) with
        {
            StartTime = newStartTime
        };

        var result = await _sut.UpdateAsync(
            trip.Id,
            request,
            TestContext.Current.CancellationToken);

        result.MoonPhase.Should().Be(nameof(MoonPhase.FullMoon));
        _moonPhaseService.Received(1).Calculate(newStartTime);
    }

    [Fact]
    public async Task UpdateAsync_LegacyTrip_DoesNotAddMoonPhase()
    {
        var trip = BuildTrip("Legacy trip");
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        var request = BuildMatchingUpdateRequest(trip) with
        {
            StartTime = trip.StartTime.AddDays(1)
        };

        var result = await _sut.UpdateAsync(
            trip.Id,
            request,
            TestContext.Current.CancellationToken);

        result.MoonPhase.Should().BeNull();
        _moonPhaseService.DidNotReceive().Calculate(Arg.Any<DateTime>());
    }

    // -----------------------------------------------------------------------
    // RetryWeatherEnrichmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RetryWeatherEnrichmentAsync_MissingWeather_EnrichesAndPersistsTrip()
    {
        var trip = BuildTripWithoutWeather();
        var previousLastModified = trip.LastModified;
        var snapshot = BuildWeatherSnapshot();
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        _weatherService.GetWeatherAsync(
                trip.Latitude!.Value,
                trip.Longitude!.Value,
                trip.StartTime,
                TestContext.Current.CancellationToken)
            .Returns(snapshot);

        var result = await _sut.RetryWeatherEnrichmentAsync(
            trip.Id,
            TestContext.Current.CancellationToken);

        result.WeatherSampleTimeUtc.Should().Be(snapshot.WeatherSampleTimeUtc);
        result.LastModified.Should().BeAfter(previousLastModified);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<FishingTrip>(savedTrip =>
                savedTrip.WeatherSampleTimeUtc == snapshot.WeatherSampleTimeUtc
                && savedTrip.LastModified > previousLastModified),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RetryWeatherEnrichmentAsync_ExistingWeather_DoesNotCallProviderOrPersist()
    {
        var trip = BuildTripWithWeather();
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);

        var result = await _sut.RetryWeatherEnrichmentAsync(
            trip.Id,
            TestContext.Current.CancellationToken);

        result.WeatherSampleTimeUtc.Should().Be(trip.WeatherSampleTimeUtc);
        await _weatherService.DidNotReceive().GetWeatherAsync(
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<FishingTrip>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryWeatherEnrichmentAsync_MissingCoordinates_DoesNotCallProviderOrPersist()
    {
        var trip = BuildTrip("No coordinates");
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);

        var result = await _sut.RetryWeatherEnrichmentAsync(
            trip.Id,
            TestContext.Current.CancellationToken);

        result.WeatherSampleTimeUtc.Should().BeNull();
        await _weatherService.DidNotReceive().GetWeatherAsync(
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<FishingTrip>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryWeatherEnrichmentAsync_ProviderFailure_RemainsNonFatalAndDoesNotPersist()
    {
        var trip = BuildTripWithoutWeather();
        _repository.GetByIdAsync(trip.Id, TestContext.Current.CancellationToken)
            .Returns(trip);
        _weatherService.GetWeatherAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<WeatherSnapshot?>(
                new HttpRequestException("Provider unavailable")));

        var result = await _sut.RetryWeatherEnrichmentAsync(
            trip.Id,
            TestContext.Current.CancellationToken);

        result.WeatherSampleTimeUtc.Should().BeNull();
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<FishingTrip>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryWeatherEnrichmentAsync_TripNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, TestContext.Current.CancellationToken)
            .ReturnsNull();

        var action = () => _sut.RetryWeatherEnrichmentAsync(
            id,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_Should_Throw_When_Trip_Not_Found()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), TestContext.Current.CancellationToken).ReturnsNull();

        // Act & Assert
        await _sut.Invoking(s => s.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_When_Trip_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, TestContext.Current.CancellationToken).Returns(BuildTrip("Trip to delete", id));

        // Act
        await _sut.DeleteAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).DeleteAsync(id, TestContext.Current.CancellationToken);
    }

    // -----------------------------------------------------------------------
    // Helpers — avoid repeating setup code in every test
    // -----------------------------------------------------------------------

    private static FishingTrip BuildTrip(string name, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        StartTime = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        LastModified = DateTime.UtcNow
    };

    private static FishingTrip BuildTripWithWeather() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Weather trip",
        Latitude = 58.9,
        Longitude = 13.5,
        StartTime = Utc(2026, 8, 20, 10),
        CreatedAt = Utc(2026, 8, 20, 9),
        LastModified = Utc(2026, 8, 20, 9),
        AirTemperatureC = 12.3,
        WeatherCode = 1,
        WindSpeedMps = 2.2,
        WindDirectionDegrees = 180,
        PressureHpa = 1010,
        WeatherSampleTimeUtc = Utc(2026, 8, 20, 10),
        WeatherProvider = "Open-Meteo"
    };

    private static FishingTrip BuildTripWithoutWeather() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Weather retry trip",
        Latitude = 58.9,
        Longitude = 13.5,
        StartTime = Utc(2026, 8, 20, 10),
        CreatedAt = Utc(2026, 8, 20, 9),
        LastModified = Utc(2026, 8, 20, 9)
    };

    private static CreateFishingTripRequest BuildCreateRequest(
        DateTime startTime,
        double? latitude,
        double? longitude) => new(
        Name: "Weather trip",
        LocationName: "Lake",
        WaterTemp: null,
        WeatherDescription: null,
        Latitude: latitude,
        Longitude: longitude,
        StartTime: startTime,
        EndTime: null,
        Note: null);

    private static UpdateFishingTripRequest BuildMatchingUpdateRequest(
        FishingTrip trip,
        string? note = null) => new(
        Name: trip.Name,
        LocationName: trip.LocationName,
        WaterTemp: trip.WaterTemp,
        WeatherDescription: trip.WeatherDescription,
        Latitude: trip.Latitude,
        Longitude: trip.Longitude,
        StartTime: trip.StartTime,
        EndTime: trip.EndTime,
        Note: note ?? trip.Note);

    private static WeatherSnapshot BuildWeatherSnapshot() => new(
        AirTemperatureC: 14.2,
        WeatherCode: 2,
        WindSpeedMps: 3.1,
        WindDirectionDegrees: 225,
        PressureHpa: 1012.4,
        WeatherSampleTimeUtc: Utc(2026, 8, 20, 10),
        WeatherProvider: "Open-Meteo");

    private static DateTime Utc(int year, int month, int day, int hour)
        => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static UpdateFishingTripRequest BuildUpdateRequest(string name = "Updated trip") => new(
        Name: name,
        StartTime: DateTime.UtcNow,
        EndTime: null,
        LocationName: null,
        Latitude: null,
        Longitude: null,
        WaterTemp: null,
        WeatherDescription: null,
        Note: null);
}
