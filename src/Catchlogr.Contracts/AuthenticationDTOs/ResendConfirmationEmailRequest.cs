namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>Requests another Identity email-confirmation message.</summary>
/// <param name="Email">The account email address.</param>
public sealed record ResendConfirmationEmailRequest(string Email);
