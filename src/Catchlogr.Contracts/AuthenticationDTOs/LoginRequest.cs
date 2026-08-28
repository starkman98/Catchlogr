namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>
/// Contains the credentials needed to login to an existing account.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The account password.</param>
public sealed record LoginRequest(string Email, string Password);
