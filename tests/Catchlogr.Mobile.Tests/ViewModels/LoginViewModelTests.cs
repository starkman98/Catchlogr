using System.Net;
using Catchlogr.Contracts.AuthenticationDTOs;
using Catchlogr.Mobile.Data;
using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.ViewModels;

/// <summary>Tests login validation, session restoration, and navigation.</summary>
public sealed class LoginViewModelTests
{
    /// <summary>Verifies that a locally known account can open cached trips without network.</summary>
    [Fact]
    public async Task InitializeAsync_StoredUser_NavigatesToFishingTrips()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetCurrentUserIdAsync().Returns(Guid.NewGuid());
        var navigator = Substitute.For<IAppNavigator>();
        var localDatabase = Substitute.For<ILocalDatabase>();
        var sut = CreateViewModel(
            tokenStore: tokenStore,
            localDatabase: localDatabase,
            navigator: navigator);

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        await localDatabase.Received(1).ActivateAsync(
            Arg.Any<Guid>(),
            TestContext.Current.CancellationToken);
        await navigator.Received(1).GoToAsync(
            AppRoutes.FishingTrips,
            TestContext.Current.CancellationToken);
    }

    /// <summary>Verifies that missing credentials are rejected before calling the API.</summary>
    [Fact]
    public async Task LoginCommand_MissingCredentials_ShowsValidationError()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var sut = CreateViewModel(authenticationService);
        sut.Email = "angler@example.com";

        await sut.LoginCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("Enter your email address and password.");
        await authenticationService.DidNotReceive().LoginAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that successful login clears the password and opens the trips page.</summary>
    [Fact]
    public async Task LoginCommand_ValidCredentials_NavigatesToFishingTrips()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CurrentUserResponse(
                Guid.NewGuid(), "angler@example.com", DateTime.UtcNow, null));
        var navigator = Substitute.For<IAppNavigator>();
        var localDatabase = Substitute.For<ILocalDatabase>();
        var sut = CreateViewModel(
            authenticationService,
            localDatabase: localDatabase,
            navigator: navigator);
        sut.Email = "angler@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        sut.Password.Should().BeEmpty();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsBusy.Should().BeFalse();
        await localDatabase.Received(1).ActivateAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await navigator.Received(1).GoToAsync(
            AppRoutes.FishingTrips,
            Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that rejected credentials produce a safe error message.</summary>
    [Fact]
    public async Task LoginCommand_Unauthorized_ShowsCredentialError()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CurrentUserResponse>(
                new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized)));
        var sut = CreateViewModel(authenticationService);
        sut.Email = "angler@example.com";
        sut.Password = "wrong-password";

        await sut.LoginCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("The email address or password is incorrect.");
        sut.IsBusy.Should().BeFalse();
    }

    private static LoginViewModel CreateViewModel(
        IAuthenticationService? authenticationService = null,
        ITokenStore? tokenStore = null,
        ILocalDatabase? localDatabase = null,
        IAppNavigator? navigator = null)
    {
        return new LoginViewModel(
            authenticationService ?? Substitute.For<IAuthenticationService>(),
            tokenStore ?? Substitute.For<ITokenStore>(),
            localDatabase ?? Substitute.For<ILocalDatabase>(),
            navigator ?? Substitute.For<IAppNavigator>(),
            Substitute.For<ILogger<LoginViewModel>>());
    }
}
