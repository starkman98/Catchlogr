using FishingLog.Contracts.AuthenticationDTOs;
using FishingLog.Mobile.Services.Authentication;
using FishingLog.Mobile.Services.Navigation;
using FishingLog.Mobile.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FishingLog.Mobile.Tests.ViewModels;

/// <summary>Tests registration validation, automatic login, and navigation.</summary>
public sealed class RegisterViewModelTests
{
    /// <summary>Verifies that mismatched passwords are rejected before calling the API.</summary>
    [Fact]
    public async Task RegisterCommand_MismatchedPasswords_ShowsValidationError()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var sut = CreateViewModel(authenticationService);
        sut.Email = "angler@example.com";
        sut.Password = "Password1!";
        sut.ConfirmPassword = "Different1!";

        await sut.RegisterCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("The passwords do not match.");
        await authenticationService.DidNotReceive().RegisterAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that registration logs in and opens the trips page.</summary>
    [Fact]
    public async Task RegisterCommand_ValidInput_RegistersLogsInAndNavigates()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CurrentUserResponse(
                Guid.NewGuid(), "angler@example.com", DateTime.UtcNow, null));
        var navigator = Substitute.For<IAppNavigator>();
        var sut = CreateViewModel(authenticationService, navigator);
        sut.Email = "angler@example.com";
        sut.Password = "Password1!";
        sut.ConfirmPassword = "Password1!";

        await sut.RegisterCommand.ExecuteAsync(null);

        await authenticationService.Received(1).RegisterAsync(
            "angler@example.com", "Password1!", Arg.Any<CancellationToken>());
        await authenticationService.Received(1).LoginAsync(
            "angler@example.com", "Password1!", Arg.Any<CancellationToken>());
        await navigator.Received(1).GoToAsync(
            AppRoutes.FishingTrips,
            Arg.Any<CancellationToken>());
        sut.Password.Should().BeEmpty();
        sut.ConfirmPassword.Should().BeEmpty();
        sut.IsBusy.Should().BeFalse();
    }

    private static RegisterViewModel CreateViewModel(
        IAuthenticationService? authenticationService = null,
        IAppNavigator? navigator = null)
    {
        return new RegisterViewModel(
            authenticationService ?? Substitute.For<IAuthenticationService>(),
            navigator ?? Substitute.For<IAppNavigator>(),
            Substitute.For<ILogger<RegisterViewModel>>());
    }
}
