using Catchlogr.Infrastructure.Email;
using Catchlogr.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Resend;

namespace Catchlogr.Tests.Email;

/// <summary>Tests the Resend adapter for Identity account-action messages.</summary>
public sealed class ResendIdentityEmailSenderTests
{
    /// <summary>
    /// Verifies that confirmation links use the configured public web origin
    /// and are encoded exactly once in HTML.
    /// </summary>
    [Fact]
    public async Task SendConfirmationLinkAsync_IdentityLink_SendsPublicLink()
    {
        var resend = Substitute.For<IResend>();
        EmailMessage? sentMessage = null;
        _ = resend.EmailSendAsync(
            Arg.Do<EmailMessage>(message => sentMessage = message),
            Arg.Any<CancellationToken>());
        var sut = CreateSender(resend);

        await sut.SendConfirmationLinkAsync(
            new ApplicationUser(),
            "angler@example.com",
            "https://internal.test/api/auth/confirmEmail?userId=123&amp;code=abc_123");

        sentMessage.Should().NotBeNull();
        var message = sentMessage!;
        message.From.Should().NotBeNull();
        var from = message.From!.ToString();
        from.Should().Contain("Catchlogr");
        from.Should()
            .Contain("account@mail.catchlogr.com");
        message.To.Should().ContainSingle()
            .Which.ToString().Should().Be("angler@example.com");
        message.TextBody.Should().Contain(
            "https://web.catchlogr.test/confirm-email?userId=123&code=abc_123");
        message.HtmlBody.Should().Contain(
            "userId=123&amp;code=abc_123");
        message.HtmlBody.Should().NotContain("&amp;amp;");
    }

    /// <summary>
    /// Verifies that password-reset links use the public web reset route.
    /// </summary>
    [Fact]
    public async Task SendPasswordResetLinkAsync_IdentityLink_SendsPublicLink()
    {
        var resend = Substitute.For<IResend>();
        EmailMessage? sentMessage = null;
        _ = resend.EmailSendAsync(
            Arg.Do<EmailMessage>(message => sentMessage = message),
            Arg.Any<CancellationToken>());
        var sut = CreateSender(resend);

        await sut.SendPasswordResetLinkAsync(
            new ApplicationUser(),
            "angler@example.com",
            "https://internal.test/api/auth/resetPassword?email=angler%40example.com&amp;code=abc%2B123");

        sentMessage.Should().NotBeNull();
        sentMessage!.TextBody.Should().Contain(
            "https://web.catchlogr.test/reset-password?email=angler%40example.com&code=abc%2B123");
        sentMessage.HtmlBody.Should().Contain(
            "email=angler%40example.com&amp;code=abc%2B123");
        sentMessage.HtmlBody.Should().NotContain("&amp;amp;");
    }

    /// <summary>
    /// Verifies that unexpected Identity routes are not exposed through email.
    /// </summary>
    [Fact]
    public async Task SendConfirmationLinkAsync_UnsupportedPath_Throws()
    {
        var resend = Substitute.For<IResend>();
        var sut = CreateSender(resend);

        var action = () => sut.SendConfirmationLinkAsync(
            new ApplicationUser(),
            "angler@example.com",
            "https://internal.test/api/auth/changeEmail?code=abc");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported Identity account-action URL*");
        await resend.DidNotReceive().EmailSendAsync(
            Arg.Any<EmailMessage>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that reset codes are available in both body formats.</summary>
    [Fact]
    public async Task SendPasswordResetCodeAsync_ResetCode_SendsCode()
    {
        var resend = Substitute.For<IResend>();
        EmailMessage? sentMessage = null;
        _ = resend.EmailSendAsync(
            Arg.Do<EmailMessage>(message => sentMessage = message),
            Arg.Any<CancellationToken>());
        var sut = CreateSender(resend);

        await sut.SendPasswordResetCodeAsync(
            new ApplicationUser(),
            "angler@example.com",
            "abc_123");

        sentMessage.Should().NotBeNull();
        sentMessage!.TextBody.Should().Contain("abc_123");
        sentMessage.HtmlBody.Should().Contain("abc_123");
        sentMessage.Subject.Should().Be("Reset your Catchlogr password");
    }

    private static ResendIdentityEmailSender CreateSender(IResend resend)
        => new(
            resend,
            Options.Create(new EmailOptions
            {
                ApiKey = "test-key",
                FromAddress = "account@mail.catchlogr.com",
                FromName = "Catchlogr",
                PublicWebBaseUrl = new Uri("https://web.catchlogr.test")
            }),
            Substitute.For<ILogger<ResendIdentityEmailSender>>());
}
