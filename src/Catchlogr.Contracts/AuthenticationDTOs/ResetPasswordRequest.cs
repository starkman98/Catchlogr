namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>Resets an account password using an Identity reset code.</summary>
/// <param name="Email">The account email address.</param>
/// <param name="ResetCode">The code delivered to the account email address.</param>
/// <param name="NewPassword">The new account password.</param>
public sealed record ResetPasswordRequest(
    string Email,
    string ResetCode,
    string NewPassword);
