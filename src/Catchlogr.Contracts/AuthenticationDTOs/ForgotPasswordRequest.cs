namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>Requests a password-reset code for an account.</summary>
/// <param name="Email">The account email address.</param>
public sealed record ForgotPasswordRequest(string Email);
