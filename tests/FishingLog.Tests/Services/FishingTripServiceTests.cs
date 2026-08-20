using FishingLog.Application.Exceptions;
using FishingLog.Application.Interfaces;
using FishingLog.Application.Services;
using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Domain.Entities;
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
    private readonly ILogger<FishingTripService> _logger;
    private readonly FishingTripService _sut; // sut = System Under Test

    public FishingTripServiceTests()
    {
        _repository = Substitute.For<IFishingTripRepository>();
        _weatherService = Substitute.For<IWeatherService>();
        _logger = Substitute.For<ILogger<FishingTripService>>();
        _sut = new FishingTripService(_repository, _weatherService, _logger);
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

        // Verify the repo's AddAsync was actually called once
        await _repository.Received(1).AddAsync(Arg.Any<FishingTrip>(), TestContext.Current.CancellationToken);
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
