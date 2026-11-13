using Resend;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Delivers composed account email without changing Identity links or tokens.</summary>
public interface IEmailTransport
{
    /// <summary>Delivers a message using the selected provider.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
