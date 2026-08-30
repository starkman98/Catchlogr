using System.Collections.Concurrent;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Catchlogr.Api.IntegrationTests;

/// <summary>Captures Identity account-action messages during integration tests.</summary>
public sealed class TestIdentityEmailSender :
    IEmailSender<ApplicationUser>
{
    private readonly ConcurrentQueue<string> _confirmationLinks = new();
    private readonly ConcurrentQueue<string> _passwordResetLinks = new();
    private readonly ConcurrentQueue<string> _passwordResetCodes = new();

    /// <summary>Gets captured email-confirmation links in send order.</summary>
    public IReadOnlyList<string> ConfirmationLinks
        => _confirmationLinks.ToArray();

    /// <summary>Gets captured password-reset links in send order.</summary>
    public IReadOnlyList<string> PasswordResetLinks
        => _passwordResetLinks.ToArray();

    /// <summary>Gets captured password-reset codes in send order.</summary>
    public IReadOnlyList<string> PasswordResetCodes
        => _passwordResetCodes.ToArray();

    /// <inheritdoc/>
    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink)
    {
        _confirmationLinks.Enqueue(confirmationLink);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink)
    {
        _passwordResetLinks.Enqueue(resetLink);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        _passwordResetCodes.Enqueue(resetCode);
        return Task.CompletedTask;
    }
}
