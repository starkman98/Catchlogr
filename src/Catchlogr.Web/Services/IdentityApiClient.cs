using System.Net;
using System.Net.Http.Json;
using Catchlogr.Contracts.AuthenticationDTOs;

namespace Catchlogr.Web.Services;

/// <summary>Calls the Catchlogr Identity minimal API for public account actions.</summary>
public sealed class IdentityApiClient : IIdentityApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityApiClient> _logger;

    /// <summary>Initializes a new Identity API client.</summary>
    /// <param name="httpClient">The configured Catchlogr API HTTP client.</param>
    /// <param name="logger">The structured logger.</param>
    public IdentityApiClient(
        HttpClient httpClient,
        ILogger<IdentityApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IdentityActionResult> ConfirmEmailAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var path = "api/auth/confirmEmail" +
            $"?userId={Uri.EscapeDataString(userId)}" +
            $"&code={Uri.EscapeDataString(code)}";

        try
        {
            using var response = await _httpClient.GetAsync(
                path,
                cancellationToken);
            return MapResponse(response.StatusCode);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The Identity API request timed out.");
            return IdentityActionResult.ServiceUnavailable;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The Identity API request failed.");
            return IdentityActionResult.ServiceUnavailable;
        }
    }

    /// <inheritdoc />
    public async Task<IdentityActionResult> ResetPasswordAsync(
        string email,
        string resetCode,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/resetPassword",
                new ResetPasswordRequest(
                    email.Trim(),
                    resetCode,
                    newPassword),
                cancellationToken);
            return MapResponse(response.StatusCode);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The Identity API request timed out.");
            return IdentityActionResult.ServiceUnavailable;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The Identity API request failed.");
            return IdentityActionResult.ServiceUnavailable;
        }
    }

    private static IdentityActionResult MapResponse(HttpStatusCode statusCode)
        => statusCode switch
        {
            >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices =>
                IdentityActionResult.Succeeded,
            HttpStatusCode.BadRequest or
            HttpStatusCode.Unauthorized or
            HttpStatusCode.NotFound => IdentityActionResult.Rejected,
            _ => IdentityActionResult.ServiceUnavailable
        };
}
