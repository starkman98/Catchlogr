using FishingLog.Mobile.Data;
using FishingLog.Mobile.Services.Authentication;
using FishingLog.Mobile.Services.Navigation;
using FishingLog.Mobile.ViewModels;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FishingLog.Mobile.Tests.ViewModels;

/// <summary>Tests account-session behavior exposed by the fishing-trips page.</summary>
public sealed class FishingTripsViewModelTests
{
    /// <summary>Verifies that logout closes storage, clears authentication, and navigates to login.</summary>
    [Fact]
    public async Task LogoutCommand_ActiveAccount_ClosesStorageAndNavigatesToLogin()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var localDatabase = Substitute.For<ILocalDatabase>();
        var navigator = Substitute.For<IAppNavigator>();
        var sut = new FishingTripsViewModel(
            Substitute.For<IFishingTripLocalRepository>(),
            Substitute.For<ISyncOrchestrator>(),
            Substitute.For<IApiHealthClient>(),
            authenticationService,
            localDatabase,
            navigator,
            Substitute.For<ILogger<FishingTripsViewModel>>());
        sut.Trips.Add(new FishingTripLocalEntity { Name = "Cached trip" });

        await sut.LogoutCommand.ExecuteAsync(null);

        await localDatabase.Received(1).CloseAsync(
            Arg.Any<CancellationToken>());
        authenticationService.Received(1).Logout();
        await navigator.Received(1).GoToAsync(
            AppRoutes.Login,
            Arg.Any<CancellationToken>());
        sut.Trips.Should().BeEmpty();
    }
}
