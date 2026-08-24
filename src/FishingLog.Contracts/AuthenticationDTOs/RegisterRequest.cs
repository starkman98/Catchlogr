namespace FishingLog.Contracts.AuthenticationDTOs;

/// <summary>
/// Contains the credentials needed to register an account.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The account password.</param>
public sealed record RegisterRequest(string Email, string Password);
