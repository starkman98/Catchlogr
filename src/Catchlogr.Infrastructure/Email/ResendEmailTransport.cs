using Resend;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Delivers account email through Resend.</summary>
public sealed class ResendEmailTransport(IResend resend) : IEmailTransport
{
    /// <inheritdoc/>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => await resend.EmailSendAsync(message, cancellationToken);
}
