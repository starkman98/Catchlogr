using FishingLog.Contracts.AuthenticationDTOs;

namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Manages account registration, login, token refresh, and the active session.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>Registers a new account with the FishingLog API.</summary>
    /// <param name="email">The account email address.</param>
    /// <param name="password">The account password.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    Task RegisterAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Logs in and stores the resulting session securely.</summary>
    /// <param name="email">The account email address.</param>
    /// <param name="password">The account password.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The authenticated account.</returns>
    Task<CurrentUserResponse> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Returns a usable access token, refreshing it when it is near expiration.</summary>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>A bearer token, or null when no valid session exists.</returns>
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default);

    /// <summary>Gets the current account from the API using the active session.</summary>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The current account, or null when signed out.</returns>
    Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken ct = default);

    /// <summary>Removes the locally stored authentication session.</summary>
    void Logout();
}
