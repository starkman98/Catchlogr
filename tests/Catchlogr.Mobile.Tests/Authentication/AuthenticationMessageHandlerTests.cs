using System.Net;
using System.Net.Http.Headers;
using Catchlogr.Mobile.Services.Authentication;
using FluentAssertions;
using NSubstitute;

namespace Catchlogr.Mobile.Tests.Authentication;

/// <summary>
/// Tests bearer-token attachment for protected mobile API requests.
/// </summary>
public sealed class AuthenticationMessageHandlerTests
{
    /// <summary>Verifies that the active access token is added as a Bearer header.</summary>
    [Fact]
    public async Task SendAsync_ValidAccessToken_AddsBearerHeader()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .GetValidAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns("access-token");
        var innerHandler = new StubHttpMessageHandler((request, _) =>
        {
            request.Headers.Authorization.Should().BeEquivalentTo(
                new AuthenticationHeaderValue("Bearer", "access-token"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var client = CreateClient(authenticationService, innerHandler);

        using var response = await client.GetAsync(
            "api/fishing-trips",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await authenticationService.Received(1)
            .GetValidAccessTokenAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that signed-out requests continue without an Authorization header.</summary>
    [Fact]
    public async Task SendAsync_NoAccessToken_SendsRequestWithoutAuthorization()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .GetValidAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var innerHandler = new StubHttpMessageHandler((request, _) =>
        {
            request.Headers.Authorization.Should().BeNull();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        using var client = CreateClient(authenticationService, innerHandler);

        using var response = await client.GetAsync(
            "api/fishing-trips",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Verifies that an explicitly supplied Authorization header is preserved.</summary>
    [Fact]
    public async Task SendAsync_ExistingAuthorizationHeader_PreservesHeader()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var innerHandler = new StubHttpMessageHandler((request, _) =>
        {
            request.Headers.Authorization.Should().BeEquivalentTo(
                new AuthenticationHeaderValue("Custom", "explicit-token"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var client = CreateClient(authenticationService, innerHandler);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/fishing-trips");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Custom", "explicit-token");

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await authenticationService.DidNotReceive()
            .GetValidAccessTokenAsync(Arg.Any<CancellationToken>());
    }

    private static HttpClient CreateClient(
        IAuthenticationService authenticationService,
        HttpMessageHandler innerHandler)
    {
        var handler = new AuthenticationMessageHandler(authenticationService)
        {
            InnerHandler = innerHandler
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.catchlogr.test/")
        };
    }
}
