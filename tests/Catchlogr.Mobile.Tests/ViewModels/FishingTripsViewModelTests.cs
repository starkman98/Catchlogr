using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.ViewModels;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.ViewModels;

/// <summary>Tests account-session behavior exposed by the fishing-trips page.</summary>
public sealed class FishingTripsViewModelTests
{
    /// <summary>Verifies that a prepared logout completes and navigates to login.</summary>
    [Fact]
    public async Task LogoutCommand_ActiveAccount_ClosesStorageAndNavigatesToLogin()
    {
        var logoutService = Substitute.For<ILogoutService>();
        logoutService.PrepareAsync(Arg.Any<CancellationToken>())
            .Returns(new LogoutPreparationResult(
                LogoutPreparationStatus.Ready,
                0));
        var logoutDialog = Substitute.For<ILogoutDialogService>();
        var navigator = Substitute.For<IAppNavigator>();
        var sut = new FishingTripsViewModel(
            Substitute.For<IFishingTripLocalRepository>(),
            Substitute.For<ISyncOrchestrator>(),
            Substitute.For<IApiHealthClient>(),
            logoutService,
            logoutDialog,
            navigator,
            Substitute.For<ILogger<FishingTripsViewModel>>());
        sut.Trips.Add(new FishingTripLocalEntity { Name = "Cached trip" });

        await sut.LogoutCommand.ExecuteAsync(null);

        await logoutService.Received(1).CompleteAsync(
            Arg.Any<CancellationToken>());
        await logoutDialog.DidNotReceive().ConfirmAsync(
            Arg.Any<LogoutPreparationResult>(),
            Arg.Any<CancellationToken>());
        await navigator.Received(1).GoToAsync(
            AppRoutes.Login,
            Arg.Any<CancellationToken>());
        sut.Trips.Should().BeEmpty();
    }

    /// <summary>Verifies that cancelling a pending-change warning keeps the session active.</summary>
    [Fact]
    public async Task LogoutCommand_PendingChangesAndCancel_DoesNotSignOut()
    {
        var logoutService = Substitute.For<ILogoutService>();
        var preparation = new LogoutPreparationResult(
            LogoutPreparationStatus.PendingChangesOffline,
            2);
        logoutService.PrepareAsync(Arg.Any<CancellationToken>())
            .Returns(preparation);
        var logoutDialog = Substitute.For<ILogoutDialogService>();
        logoutDialog.ConfirmAsync(preparation, Arg.Any<CancellationToken>())
            .Returns(LogoutDecision.Cancel);
        var navigator = Substitute.For<IAppNavigator>();
        var sut = new FishingTripsViewModel(
            Substitute.For<IFishingTripLocalRepository>(),
            Substitute.For<ISyncOrchestrator>(),
            Substitute.For<IApiHealthClient>(),
            logoutService,
            logoutDialog,
            navigator,
            Substitute.For<ILogger<FishingTripsViewModel>>());

        await sut.LogoutCommand.ExecuteAsync(null);

        await logoutService.DidNotReceive().CompleteAsync(
            Arg.Any<CancellationToken>());
        await navigator.DidNotReceive().GoToAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
