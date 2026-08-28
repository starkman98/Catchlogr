using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catchlogr.Contracts.AuthenticationDTOs;

namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>
/// Uses the Catchlogr Identity API and secure token storage to manage authentication.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private static readonly TimeSpan ExpirationSafetyWindow = TimeSpan.FromMinutes(1);

    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>Initializes a new authentication service.</summary>
    /// <param name="httpClient">The client configured for the Catchlogr API.</param>
    /// <param name="tokenStore">The secure authentication-session store.</param>
    /// <param name="timeProvider">The source of current UTC time.</param>
    public AuthenticationService(
        HttpClient httpClient,
        ITokenStore tokenStore,
        TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        ValidateCredentials(email, password);

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            new RegisterRequest(email.Trim(), password),
            ct);

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc/>
    public async Task<CurrentUserResponse> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        ValidateCredentials(email, password);

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login?useCookies=false",
            new LoginRequest(email.Trim(), password),
            ct);

        response.EnsureSuccessStatusCode();
        var tokens = await ReadTokensAsync(response, ct);

        try
        {
            await SaveTokensAsync(tokens);
            return await GetCurrentUserWithTokenAsync(tokens.AccessToken, ct);
        }
        catch
        {
            _tokenStore.Clear();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        var accessToken = await GetUsableAccessTokenAsync();
        if (accessToken is not null)
            return accessToken;

        await _refreshLock.WaitAsync(ct);
        try
        {
            // Another request may have refreshed the session while this one waited.
            accessToken = await GetUsableAccessTokenAsync();
            if (accessToken is not null)
                return accessToken;

            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _tokenStore.Clear();
                return null;
            }

            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshTokenRequest(refreshToken),
                ct);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                _tokenStore.Clear();
                return null;
            }

            response.EnsureSuccessStatusCode();
            var tokens = await ReadTokensAsync(response, ct);

            try
            {
                await SaveTokensAsync(tokens);
                return tokens.AccessToken;
            }
            catch
            {
                _tokenStore.Clear();
                throw;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> GetUsableAccessTokenAsync()
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();
        var expiresAtUtc = await _tokenStore.GetAccessTokenExpiresAtUtcAsync();

        return !string.IsNullOrWhiteSpace(accessToken) &&
               expiresAtUtc > _timeProvider.GetUtcNow().Add(ExpirationSafetyWindow)
            ? accessToken
            : null;
    }

    /// <inheritdoc/>
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var accessToken = await GetValidAccessTokenAsync(ct);
        if (accessToken is null)
        {
            return null;
        }

        try
        {
            return await GetCurrentUserWithTokenAsync(accessToken, ct);
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            _tokenStore.Clear();
            return null;
        }
    }

    /// <inheritdoc/>
    public void Logout() => _tokenStore.Clear();

    private async Task<CurrentUserResponse> GetCurrentUserWithTokenAsync(
        string accessToken,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(ct)
            ?? throw new InvalidOperationException(
                "The authentication API returned an empty user response.");

        await _tokenStore.SaveCurrentUserAsync(user.Id, user.Email);
        return user;
    }

    private async Task SaveTokensAsync(AccessTokenResponse tokens)
    {
        if (tokens.ExpiresIn <= 0)
        {
            throw new InvalidOperationException(
                "The authentication API returned an invalid token lifetime.");
        }

        var expiresAtUtc = _timeProvider.GetUtcNow().AddSeconds(tokens.ExpiresIn);
        await _tokenStore.SaveTokensAsync(
            tokens.AccessToken,
            tokens.RefreshToken,
            expiresAtUtc);
    }

    private static async Task<AccessTokenResponse> ReadTokensAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<AccessTokenResponse>(ct)
            ?? throw new InvalidOperationException(
                "The authentication API returned an empty token response.");
    }

    private static void ValidateCredentials(string email, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
    }
}
