using System.Globalization;

namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>
/// Stores authentication-session information using the platform's
/// encrypted secure-storage implementation.
/// </summary>
public sealed class SecureTokenStore : ITokenStore
{
    private const string AccessTokenKey =
        "catchlogr.auth.access_token";

    private const string RefreshTokenKey =
        "catchlogr.auth.refresh_token";

    private const string AccessTokenExpiresAtUtcKey =
        "catchlogr.auth.access_token_expires_at_utc";

    private const string CurrentUserIdKey =
        "catchlogr.auth.current_user_id";

    private const string CurrentUserEmailKey =
        "catchlogr.auth.current_user_email";

    private readonly ISecureStorage _secureStorage;

    /// <summary>
    /// Initializes a new secure token store.
    /// </summary>
    /// <param name="secureStorage">
    /// The platform secure-storage implementation.
    /// </param>
    public SecureTokenStore(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    /// <inheritdoc/>
    public async Task SaveTokensAsync(
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var expirationValue = accessTokenExpiresAtUtc
            .ToUniversalTime()
            .ToString("O", CultureInfo.InvariantCulture);

        // Remove the access token first. It is written last and therefore
        // acts as a marker that the complete token set was saved.
        _secureStorage.Remove(AccessTokenKey);

        try
        {
            await _secureStorage.SetAsync(
                RefreshTokenKey,
                refreshToken);

            await _secureStorage.SetAsync(
                AccessTokenExpiresAtUtcKey,
                expirationValue);

            await _secureStorage.SetAsync(
                AccessTokenKey,
                accessToken);
        }
        catch
        {
            Clear();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync()
    {
        var value = await ReadAsync(AccessTokenKey);

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    /// <inheritdoc/>
    public async Task<string?> GetRefreshTokenAsync()
    {
        var value = await ReadAsync(RefreshTokenKey);

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    /// <inheritdoc/>
    public async Task<DateTimeOffset?>
        GetAccessTokenExpiresAtUtcAsync()
    {
        var storedValue =
            await ReadAsync(AccessTokenExpiresAtUtcKey);

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return null;
        }

        if (DateTimeOffset.TryParseExact(
            storedValue,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var expiresAt))
        {
            return expiresAt.ToUniversalTime();
        }

        // The session metadata is malformed and cannot be trusted.
        Clear();
        return null;
    }

    /// <inheritdoc/>
    public async Task SaveCurrentUserAsync(
        Guid userId,
        string email)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user identifier is required.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        try
        {
            await _secureStorage.SetAsync(
                CurrentUserIdKey,
                userId.ToString("D"));

            await _secureStorage.SetAsync(
                CurrentUserEmailKey,
                email.Trim());
        }
        catch
        {
            Clear();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        var storedValue = await ReadAsync(CurrentUserIdKey);

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return null;
        }

        if (Guid.TryParse(storedValue, out var userId) &&
            userId != Guid.Empty)
        {
            return userId;
        }

        Clear();
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> GetCurrentUserEmailAsync()
    {
        var value = await ReadAsync(CurrentUserEmailKey);

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _secureStorage.Remove(AccessTokenKey);
        _secureStorage.Remove(RefreshTokenKey);
        _secureStorage.Remove(AccessTokenExpiresAtUtcKey);
        _secureStorage.Remove(CurrentUserIdKey);
        _secureStorage.Remove(CurrentUserEmailKey);
    }

    private async Task<string?> ReadAsync(string key)
    {
        try
        {
            return await _secureStorage.GetAsync(key);
        }
        catch
        {
            // Encrypted values can become unreadable after a device
            // restore or encryption-key change. Force a fresh login.
            _secureStorage.RemoveAll();
            return null;
        }
    }
}