namespace FishingLog.Contracts.AuthenticationDTOs;

/// <summary>
/// Contains the authorized users refreshtoken.
/// </summary>
/// <param name="RefreshToken">The authorized users refreshtoken.</param>
public sealed record RefreshTokenRequest(string RefreshToken);
