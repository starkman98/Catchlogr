using System.Net.Http.Json;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Resend;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Captures account email in a local Mailpit inbox for E2E tests.</summary>
public sealed class MailpitEmailTransport(
    HttpClient client,
    IOptions<EmailOptions> options) : IEmailTransport
{
    /// <inheritdoc/>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            From = Address(message.From!.ToString()),
            To = message.To.Select(recipient => Address(recipient.ToString())).ToArray(),
            message.Subject,
            HTML = message.HtmlBody,
            Text = message.TextBody
        };
        using var response = await client.PostAsJsonAsync(
            new Uri(options.Value.MailpitBaseUrl!, "api/v1/send"),
            payload,
            cancellationToken);
        // Avoid exposing a provider response body that may contain account-action codes.
        response.EnsureSuccessStatusCode();
    }

    private static object Address(string value)
    {
        var address = new MailAddress(value);
        return new { Email = address.Address, Name = address.DisplayName };
    }
}
