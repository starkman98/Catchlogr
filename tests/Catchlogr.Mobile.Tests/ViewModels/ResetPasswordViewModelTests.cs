using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.ViewModels;

/// <summary>Tests code-based password reset behavior.</summary>
public sealed class ResetPasswordViewModelTests
{
    /// <summary>Verifies that mismatched passwords are rejected locally.</summary>
    [Fact]
    public async Task ResetPasswordCommand_MismatchedPasswords_ShowsError()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var sut = CreateViewModel(authenticationService);
        sut.Email = "angler@example.com";
        sut.ResetCode = "reset-code";
        sut.NewPassword = "Password1!";
        sut.ConfirmPassword = "Different1!";

        await sut.ResetPasswordCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("The passwords do not match.");
        await authenticationService.DidNotReceive().ResetPasswordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that a valid code resets and clears sensitive fields.</summary>
    [Fact]
    public async Task ResetPasswordCommand_ValidInput_ResetsPassword()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var sut = CreateViewModel(authenticationService);
        sut.Email = "angler@example.com";
        sut.ResetCode = " reset-code ";
        sut.NewPassword = "Password1!";
        sut.ConfirmPassword = "Password1!";

        await sut.ResetPasswordCommand.ExecuteAsync(null);

        await authenticationService.Received(1).ResetPasswordAsync(
            "angler@example.com",
            " reset-code ",
            "Password1!",
            Arg.Any<CancellationToken>());
        sut.ResetCode.Should().BeEmpty();
        sut.NewPassword.Should().BeEmpty();
        sut.ConfirmPassword.Should().BeEmpty();
        sut.StatusMessage.Should().Contain("now sign in");
        sut.IsBusy.Should().BeFalse();
    }

    private static ResetPasswordViewModel CreateViewModel(
        IAuthenticationService authenticationService)
        => new(
            authenticationService,
            Substitute.For<IAppNavigator>(),
            Substitute.For<ILogger<ResetPasswordViewModel>>());
}
