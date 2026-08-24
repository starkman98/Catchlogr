namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Stores authentication tokens and active-account metadata securely
/// on the current device.
/// </summary>
public interface ITokenStore
{
    /// <summary>
    /// Saves a new access token, refresh token, and access-token
    /// expiration time.
    /// </summary>
    /// <param name="accessToken">
    /// The bearer access token.
    /// </param>
    /// <param name="refreshToken">
    /// The token used to obtain a new access token.
    /// </param>
    /// <param name="accessTokenExpiresAtUtc">
    /// The access-token expiration time in UTC.
    /// </param>
    Task SaveTokensAsync(
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresAtUtc);

    /// <summary>
    /// Gets the stored access token, or null when unavailable.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Gets the stored refresh token, or null when unavailable.
    /// </summary>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Gets the access-token expiration time in UTC,
    /// or null when unavailable.
    /// </summary>
    Task<DateTimeOffset?> GetAccessTokenExpiresAtUtcAsync();

    /// <summary>
    /// Saves information identifying the active account.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="email">The account email address.</param>
    Task SaveCurrentUserAsync(
        Guid userId,
        string email);

    /// <summary>
    /// Gets the active account identifier, or null when unavailable.
    /// </summary>
    Task<Guid?> GetCurrentUserIdAsync();

    /// <summary>
    /// Gets the active account email address, or null when unavailable.
    /// </summary>
    Task<string?> GetCurrentUserEmailAsync();

    /// <summary>
    /// Removes all stored authentication-session information.
    /// </summary>
    void Clear();
}