namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>
/// Contains bearer tokens returned after login or token refresh.
/// </summary>
/// <param name="TokenType">The token type, normally Bearer.</param>
/// <param name="AccessToken">The short-lived access token.</param>
/// <param name="ExpiresIn">
/// The access-token lifetime in seconds.
/// </param>
/// <param name="RefreshToken">
/// The token used to obtain a new token pair.
/// </param>
public sealed record AccessTokenResponse(
    string TokenType,
    string AccessToken,
    long ExpiresIn,
    string RefreshToken);