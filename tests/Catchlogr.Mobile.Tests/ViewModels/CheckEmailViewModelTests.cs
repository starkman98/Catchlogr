using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.ViewModels;

/// <summary>Tests confirmation-email resend behavior.</summary>
public sealed class CheckEmailViewModelTests
{
    /// <summary>Verifies that a resend request uses the routed email address.</summary>
    [Fact]
    public async Task ResendCommand_RoutedEmail_RequestsConfirmationEmail()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var sut = new CheckEmailViewModel(
            authenticationService,
            Substitute.For<IAppNavigator>(),
            Substitute.For<ILogger<CheckEmailViewModel>>());
        sut.ApplyQueryAttributes(new Dictionary<string, object>
        {
            ["email"] = "angler@example.com"
        });

        await sut.ResendCommand.ExecuteAsync(null);

        await authenticationService.Received(1)
            .ResendConfirmationEmailAsync(
                "angler@example.com",
                Arg.Any<CancellationToken>());
        sut.StatusMessage.Should().Contain("new email has been sent");
        sut.IsBusy.Should().BeFalse();
    }
}
