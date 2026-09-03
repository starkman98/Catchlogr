namespace Catchlogr.Web.Services;

/// <summary>Provides public account-action operations backed by Catchlogr.Api.</summary>
public interface IIdentityApiClient
{
    /// <summary>Confirms an email address using an Identity confirmation token.</summary>
    /// <param name="userId">The Identity user identifier.</param>
    /// <param name="code">The one-time email-confirmation code.</param>
    /// <param name="cancellationToken">Cancels the pending HTTP request.</param>
    /// <returns>The result of the confirmation attempt.</returns>
    Task<IdentityActionResult> ConfirmEmailAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>Resets an account password using an Identity reset token.</summary>
    /// <param name="email">The account email address.</param>
    /// <param name="resetCode">The one-time password-reset code.</param>
    /// <param name="newPassword">The replacement password.</param>
    /// <param name="cancellationToken">Cancels the pending HTTP request.</param>
    /// <returns>The result of the reset attempt.</returns>
    Task<IdentityActionResult> ResetPasswordAsync(
        string email,
        string resetCode,
        string newPassword,
        CancellationToken cancellationToken = default);
}
