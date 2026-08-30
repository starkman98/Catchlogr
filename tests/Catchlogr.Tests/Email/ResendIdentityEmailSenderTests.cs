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
    /// Verifies that confirmation links use the configured public API origin
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
            "https://api.catchlogr.com/api/auth/confirmEmail?userId=123&code=abc_123");
        message.HtmlBody.Should().Contain(
            "userId=123&amp;code=abc_123");
        message.HtmlBody.Should().NotContain("&amp;amp;");
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
                PublicApiBaseUrl = new Uri("https://api.catchlogr.com")
            }),
            Substitute.For<ILogger<ResendIdentityEmailSender>>());
}
