using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.ViewModels;

/// <summary>Tests password-reset-code requests.</summary>
public sealed class ForgotPasswordViewModelTests
{
    /// <summary>Verifies that a successful request opens the code entry form.</summary>
    [Fact]
    public async Task SendCodeCommand_ValidEmail_OpensResetPassword()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var navigator = Substitute.For<IAppNavigator>();
        var sut = new ForgotPasswordViewModel(
            authenticationService,
            navigator,
            Substitute.For<ILogger<ForgotPasswordViewModel>>())
        {
            Email = "angler@example.com"
        };

        await sut.SendCodeCommand.ExecuteAsync(null);

        await authenticationService.Received(1).ForgotPasswordAsync(
            "angler@example.com",
            Arg.Any<CancellationToken>());
        await navigator.Received(1).GoToAsync(
            AppRoutes.ResetPasswordFor("angler@example.com"),
            Arg.Any<CancellationToken>());
        sut.IsBusy.Should().BeFalse();
    }
}
