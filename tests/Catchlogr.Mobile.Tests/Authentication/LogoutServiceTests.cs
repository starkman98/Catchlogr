using Catchlogr.Mobile.Data;
using Catchlogr.Mobile.Services;
using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.Authentication;

/// <summary>
/// Tests pending-change handling and local cleanup during sign-out.
/// </summary>
public sealed class LogoutServiceTests
{
    /// <summary>Verifies that an account without pending changes can sign out immediately.</summary>
    [Fact]
    public async Task PrepareAsync_NoPendingChanges_ReturnsReadyWithoutSync()
    {
        var dependencies = CreateDependencies();
        var sut = dependencies.CreateService();

        var result = await sut.PrepareAsync(
            TestContext.Current.CancellationToken);

        result.Should().Be(new LogoutPreparationResult(
            LogoutPreparationStatus.Ready,
            0));
        await dependencies.SyncOrchestrator.DidNotReceive()
            .SyncAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that online pending changes are synchronized silently.</summary>
    [Fact]
    public async Task PrepareAsync_PendingChangesOnline_SynchronizesAndReturnsReady()
    {
        var dependencies = CreateDependencies();
        dependencies.Connectivity.NetworkAccess.Returns(NetworkAccess.Internet);
        dependencies.HealthClient.IsHealthyAsync(Arg.Any<CancellationToken>())
            .Returns(true);
        var readCount = 0;
        dependencies.TripRepository.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns(_ => readCount++ == 0
                ? [new FishingTripLocalEntity { IsDirty = true }]
                : []);
        var sut = dependencies.CreateService();

        var result = await sut.PrepareAsync(
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogoutPreparationStatus.Ready);
        await dependencies.SyncOrchestrator.Received(1)
            .SyncAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Verifies that offline pending changes produce a user-warning result.</summary>
    [Fact]
    public async Task PrepareAsync_PendingChangesOffline_PreservesPendingCount()
    {
        var dependencies = CreateDependencies();
        dependencies.Connectivity.NetworkAccess.Returns(NetworkAccess.None);
        dependencies.CatchRepository.GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns([new CatchLocalEntity { IsDirty = true }]);
        var sut = dependencies.CreateService();

        var result = await sut.PrepareAsync(
            TestContext.Current.CancellationToken);

        result.Should().Be(new LogoutPreparationResult(
            LogoutPreparationStatus.PendingChangesOffline,
            1));
        await dependencies.SyncOrchestrator.DidNotReceive()
            .SyncAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that token metadata is cleared even when closing storage fails.</summary>
    [Fact]
    public async Task CompleteAsync_DatabaseCloseFails_StillClearsAuthentication()
    {
        var dependencies = CreateDependencies();
        dependencies.LocalDatabase
            .CloseAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(
                new IOException("Unable to close database.")));
        var sut = dependencies.CreateService();

        var action = () => sut.CompleteAsync(
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<IOException>();
        dependencies.AuthenticationService.Received(1).Logout();
    }

    private static LogoutDependencies CreateDependencies()
    {
        var dependencies = new LogoutDependencies(
            Substitute.For<IFishingTripLocalRepository>(),
            Substitute.For<ICatchLocalRepository>(),
            Substitute.For<ISyncOrchestrator>(),
            Substitute.For<IConnectivity>(),
            Substitute.For<IApiHealthClient>(),
            Substitute.For<ILocalDatabase>(),
            Substitute.For<IAuthenticationService>(),
            Substitute.For<ILogger<LogoutService>>());
        dependencies.TripRepository
            .GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        dependencies.CatchRepository
            .GetDirtyAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        return dependencies;
    }

    private sealed record LogoutDependencies(
        IFishingTripLocalRepository TripRepository,
        ICatchLocalRepository CatchRepository,
        ISyncOrchestrator SyncOrchestrator,
        IConnectivity Connectivity,
        IApiHealthClient HealthClient,
        ILocalDatabase LocalDatabase,
        IAuthenticationService AuthenticationService,
        ILogger<LogoutService> Logger)
    {
        public LogoutService CreateService()
            => new(
                TripRepository,
                CatchRepository,
                SyncOrchestrator,
                Connectivity,
                HealthClient,
                LocalDatabase,
                AuthenticationService,
                Logger);
    }
}

