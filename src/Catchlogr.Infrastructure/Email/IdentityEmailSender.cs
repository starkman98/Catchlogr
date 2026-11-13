using System.Net;
using System.Net.Mail;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Sends Identity account-action messages through the configured transport.</summary>
public sealed class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailTransport _transport;
    private readonly EmailOptions _options;
    private readonly IdentityEmailLinks _links;
    private readonly ILogger<IdentityEmailSender> _logger;

    /// <summary>Initializes account email with a delivery transport and validated settings.</summary>
    public IdentityEmailSender(
        IEmailTransport transport,
        IOptions<EmailOptions> options,
        ILogger<IdentityEmailSender> logger)
    {
        _transport = transport;
        _options = options.Value;
        _links = new IdentityEmailLinks(_options);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => SendAsync(email, new AccountActionEmail
        {
            Subject = "Confirm your Catchlogr account",
            Heading = "Confirm your email",
            Introduction = "Confirm your email address to finish creating your Catchlogr account.",
            ActionText = "Confirm email",
            Link = _links.Confirmation(confirmationLink),
            MessageType = "confirmation"
        });

    /// <inheritdoc/>
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => SendAsync(email, new AccountActionEmail
        {
            Subject = "Reset your Catchlogr password",
            Heading = "Reset your password",
            Introduction = "Use the link below to choose a new Catchlogr password.",
            ActionText = "Reset password",
            Link = _links.PasswordReset(resetLink),
            MessageType = "password-reset-link"
        });

    /// <inheritdoc/>
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var decodedCode = WebUtility.HtmlDecode(resetCode);
        return SendAsync(email, new AccountActionEmail
        {
            Subject = "Reset your Catchlogr password",
            Heading = "Reset your password",
            Introduction = "Use the link below to choose a new Catchlogr password.",
            ActionText = "Reset password",
            Link = _links.ResetCode(email, decodedCode),
            ResetCode = decodedCode,
            MessageType = "password-reset-code"
        });
    }

    private async Task SendAsync(string email, AccountActionEmail content)
    {
        var message = new EmailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName).ToString(),
            Subject = content.Subject,
            HtmlBody = content.BuildHtml(),
            TextBody = content.TextBody
        };
        message.To.Add(email);
        try
        {
            await _transport.SendAsync(message);
            _logger.LogInformation("Transport accepted an Identity email of type {MessageType}.", content.MessageType);
        }
        catch (Exception)
        {
            _logger.LogError("Transport rejected an Identity email of type {MessageType}.", content.MessageType);
            throw;
        }
    }
}
