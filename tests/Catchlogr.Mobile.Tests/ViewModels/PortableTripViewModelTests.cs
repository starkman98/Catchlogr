using Catchlogr.Mobile.Core.Services;
using Catchlogr.Mobile.Core.Services.Navigation;
using Catchlogr.Mobile.Core.ViewModels;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.ViewModels;

/// <summary>Exercises trip editing through the portable host boundaries.</summary>
public sealed class PortableTripViewModelTests
{
    /// <summary>Saving a new trip persists UTC values before navigating back.</summary>
    [Fact]
    public async Task SaveCommand_NewTrip_PersistsUtcAndNavigatesBack()
    {
        var repository = Substitute.For<IFishingTripLocalRepository>();
        var navigator = Substitute.For<IAppNavigator>();
        var sut = Create(repository, navigator);
        sut.Name = "Evening trip";
        sut.StartDate = new DateTime(2026, 11, 8);
        sut.StartTimeOfDay = TimeSpan.FromHours(17);
        var expectedTime = DateTime.SpecifyKind(sut.StartDate + sut.StartTimeOfDay, DateTimeKind.Local).ToUniversalTime();

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).AddAsync(Arg.Is<FishingTripLocalEntity>(trip =>
            trip.Name == "Evening trip" && trip.StartTime == expectedTime && trip.StartTime.Kind == DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
        await navigator.Received(1).GoToAsync("..", Arg.Any<CancellationToken>());
    }

    /// <summary>Invalid input stays on the form and displays the existing validation message.</summary>
    [Fact]
    public async Task SaveCommand_EmptyName_ShowsValidationWithoutSaving()
    {
        var repository = Substitute.For<IFishingTripLocalRepository>();
        var navigator = Substitute.For<IAppNavigator>();
        var interaction = Substitute.For<IAppInteraction>();
        var sut = Create(repository, navigator, interaction);

        await sut.SaveCommand.ExecuteAsync(null);

        await interaction.Received(1).DisplayAlertAsync("Validation", "Name is required.", "OK");
        await repository.DidNotReceive().AddAsync(Arg.Any<FishingTripLocalEntity>(), Arg.Any<CancellationToken>());
        await navigator.DidNotReceive().GoToAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Platform permission failures retain their user-facing meaning in Core.</summary>
    [Fact]
    public async Task CaptureLocation_PermissionDenied_PreservesExistingCoordinates()
    {
        var location = Substitute.For<IDeviceLocationService>();
        location.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DeviceLocationCapture?>(new DevicePermissionException("Denied")));
        var sut = Create(Substitute.For<IFishingTripLocalRepository>(), Substitute.For<IAppNavigator>(), location: location);
        sut.Latitude = 60;
        sut.Longitude = 15;

        await sut.CaptureCurrentLocationCommand.ExecuteAsync(null);

        sut.LocationStatus.Should().Be("Location permission was denied. You can still save the trip.");
        sut.Latitude.Should().Be(60);
        sut.Longitude.Should().Be(15);
        sut.IsLocating.Should().BeFalse();
    }

    private static AddEditFishingTripViewModel Create(
        IFishingTripLocalRepository repository,
        IAppNavigator navigator,
        IAppInteraction? interaction = null,
        IDeviceLocationService? location = null)
        => new(interaction ?? Substitute.For<IAppInteraction>(), navigator, repository,
            location ?? Substitute.For<IDeviceLocationService>(), Substitute.For<ILocationSearchApiClient>(),
            Substitute.For<ILogger<AddEditFishingTripViewModel>>());
}
