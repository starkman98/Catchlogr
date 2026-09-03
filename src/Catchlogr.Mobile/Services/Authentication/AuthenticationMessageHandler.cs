using System.Net.Http.Headers;

namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>
/// Adds the active bearer token to requests sent to protected API endpoints.
/// </summary>
public sealed class AuthenticationMessageHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// Initializes a new authentication message handler.
    /// </summary>
    /// <param name="authenticationService">
    /// The service that supplies a valid access token.
    /// </param>
    public AuthenticationMessageHandler(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var accessToken = await _authenticationService
                .GetValidAccessTokenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
