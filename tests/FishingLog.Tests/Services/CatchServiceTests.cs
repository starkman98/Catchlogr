using FishingLog.Application.Exceptions;
using FishingLog.Application.Services;
using FishingLog.Application.Interfaces;
using FishingLog.Contracts.CatchDTOs;
using FishingLog.Domain.Entities;
using FishingLog.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FishingLog.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CatchService"/>.
/// The repository is replaced with a fake (NSubstitute) so no database is needed.
/// </summary>
public class CatchServiceTests
{
    private readonly ICatchRepository _repo;
    private readonly IFishingTripRepository _tripRepo;
    private readonly CatchService _sut;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPhotoService _photoService;
    private readonly Guid _userId = Guid.NewGuid();

    public CatchServiceTests()
    {
        _repo = Substitute.For<ICatchRepository>();
        _tripRepo = Substitute.For<IFishingTripRepository>();
        _currentUserContext = Substitute.For<ICurrentUserContext>();
        _currentUserContext.UserId.Returns(_userId);
        _photoService = Substitute.For<IPhotoService>();
        _sut = new CatchService(
            _repo,
            _tripRepo,
            _currentUserContext,
            _photoService);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Mapped_Response()
    {
        var catches = new List<Catch>
        {
            BuildCatch("Species1"),
            BuildCatch("Species2")
        };

        _repo.GetAllAsync(_userId, TestContext.Current.CancellationToken).Returns(catches);

        var result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        result[0].Species.Should().Be("Species1");
        result[1].Species.Should().Be("Species2");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Response_When_Trip_Exists()
    {
        var id = Guid.NewGuid();
        var fakeCatch = BuildCatch("Species", id);
        _repo.GetByIdAsync(id, _userId, TestContext.Current.CancellationToken).Returns(fakeCatch);

        var result = await _sut.GetByIdAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Species.Should().Be("Species");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Throw_When_Trip_Not_Found()
    {
        await _repo.GetByIdAsync(Arg.Any<Guid>(), _userId, TestContext.Current.CancellationToken);

        await _sut.Invoking(x => x.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Response_With_New_Id()
    {
        var tripId = Guid.NewGuid();
        var trip = BuildTrip("Test trip", tripId);
        _tripRepo.GetByIdAsync(tripId, _userId, TestContext.Current.CancellationToken).Returns(trip);

        var request = BuildCreateRequest();

        var result = await _sut.CreateAsync(tripId, request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Species.Should().Be("Species");

        await _repo.Received(1).AddAsync(Arg.Any<Catch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateAsync_Should_Throw_When_Trip_Not_Found()
    {
        _tripRepo.GetByIdAsync(Arg.Any<Guid>(), _userId, TestContext.Current.CancellationToken).ReturnsNull();


        var request = BuildCreateRequest();
        await _sut.Invoking(x => x.CreateAsync(Arg.Any<Guid>(), request, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_And_Return_Response()
    {
        var id = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = BuildTrip("update catch trip", tripId);
        _tripRepo.GetByIdAsync(tripId, _userId, TestContext.Current.CancellationToken).Returns(trip);

        var updateCatch = BuildCatch("update catch", id, tripId);
        _repo.GetByIdAsync(id, _userId, TestContext.Current.CancellationToken).Returns(updateCatch);

        var request = BuildUpdateRequest();
        var result = await _sut.UpdateAsync(id, request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Species.Should().Be("Updated species");
        await _repo.Received(1).UpdateAsync(Arg.Any<Catch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Trip_Not_Found()
    {
        _tripRepo.GetByIdAsync(Arg.Any<Guid>(), _userId, TestContext.Current.CancellationToken).ReturnsNull();

        var request = BuildUpdateRequest();
        await _sut.Invoking(x => x.UpdateAsync(Arg.Any<Guid>(), request, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Catch_Not_Found()
    {
        var request = BuildUpdateRequest();
        await _sut.Invoking(x => x.UpdateAsync(Arg.Any<Guid>(), request, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_When_Catch_Exists()
    {
        var id = Guid.NewGuid();
        var deleteCatch = BuildCatch(id: id);

        _repo.GetByIdAsync(id, _userId, TestContext.Current.CancellationToken).Returns(deleteCatch);

        await _sut.DeleteAsync(id, TestContext.Current.CancellationToken);

        await _repo.Received(1).DeleteAsync(Arg.Any<Guid>(), _userId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_When_Catch_Not_Found()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), _userId, TestContext.Current.CancellationToken).ReturnsNull();

        await _sut.Invoking(x => x.DeleteAsync(Arg.Any<Guid>(), TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByTripIdAsync_Should_Return_Mapped_Response()
    {
        var tripId = Guid.NewGuid();
        var trip = BuildTrip("Test trip", tripId);
        _tripRepo.GetByIdAsync(tripId, _userId, TestContext.Current.CancellationToken).Returns(trip);

        var catches = new List<Catch>
        {
            BuildCatch("Species1"),
            BuildCatch("Species2")
        };

        _repo.GetByTripIdAsync(tripId, _userId, TestContext.Current.CancellationToken).Returns(catches);

        var result = await _sut.GetByTripIdAsync(tripId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        result[0].Species.Should().Be("Species1");
        result[1].Species.Should().Be("Species2");
    }

    [Fact]
    public async Task GetByTripIdAsync_Should_Throw_When_Trip_Not_Found()
    {
        _tripRepo.GetByIdAsync(Arg.Any<Guid>(), _userId, TestContext.Current.CancellationToken).ReturnsNull();

        await _sut.Invoking(x => x.GetByTripIdAsync(Arg.Any<Guid>(), TestContext.Current.CancellationToken))
            .Should().ThrowAsync<NotFoundException>();
    }

    private static Catch BuildCatch(string species = "Test species", Guid? id = null, Guid? tridId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        TripId = tridId ?? Guid.NewGuid(),
        Species = species,
        Length = null,
        Weight = null,
        PhotoUrl = null,
        Note = null,
        CaughtAt = DateTime.UtcNow,
        LastModified = DateTime.UtcNow,
        Depth = null,
        Latitude = null,
        Longitude = null,
        Bait = null
    };

    private static FishingTrip BuildTrip(string name, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        StartTime = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        LastModified = DateTime.UtcNow
    };

    private static CreateCatchRequest BuildCreateRequest(string species = "Species") => new(
        Species: species,
        Length: null,
        Weight: null,
        PhotoUrl: null,
        Note: null,
        CaughtAt: DateTime.UtcNow,
        Depth: null,
        Latitude: null,
        Longitude: null,
        Bait: null
        );

    private static UpdateCatchRequest BuildUpdateRequest(string species = "Updated species") => new(
        Species: species,
        Length: null,
        Weight: null,
        PhotoUrl: null,
        Note: null,
        CaughtAt: DateTime.UtcNow,
        Depth: null,
        Latitude: null,
        Longitude: null,
        Bait: null
        );
}
