using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FishingLog.Contracts.AuthenticationDTOs;
using FishingLog.Mobile.Services.Authentication;
using FluentAssertions;
using NSubstitute;

namespace FishingLog.Mobile.Tests.Authentication;

/// <summary>
/// Tests API authentication, token refresh, and local session management.
/// </summary>
public sealed class AuthenticationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that registration sends normalized credentials to Identity.</summary>
    [Fact]
    public async Task RegisterAsync_ValidCredentials_SendsRegistrationRequest()
    {
        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.Should().Be("/api/auth/register");
            var body = await request.Content!.ReadFromJsonAsync<RegisterRequest>(ct);
            body.Should().Be(new RegisterRequest("angler@example.com", "Password1!"));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CreateService(handler, Substitute.For<ITokenStore>());

        await sut.RegisterAsync(
            "  angler@example.com  ",
            "Password1!",
            TestContext.Current.CancellationToken);
    }

    /// <summary>Verifies that blank registration credentials are rejected locally.</summary>
    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("angler@example.com", " ")]
    public async Task RegisterAsync_BlankCredentials_ThrowsArgumentException(
        string email,
        string password)
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("HTTP should not be called."));
        var sut = CreateService(handler, Substitute.For<ITokenStore>());

        var action = () => sut.RegisterAsync(email, password);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Verifies that login stores tokens and authenticated account metadata.</summary>
    [Fact]
    public async Task LoginAsync_ValidCredentials_SavesSessionAndReturnsUser()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        var user = new CurrentUserResponse(
            Guid.NewGuid(),
            "angler@example.com",
            Now.UtcDateTime,
            "Angler");
        var callNumber = 0;
        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            callNumber++;
            if (callNumber == 1)
            {
                request.RequestUri!.PathAndQuery.Should()
                    .Be("/api/auth/login?useCookies=false");
                var login = await request.Content!.ReadFromJsonAsync<LoginRequest>(ct);
                login.Should().Be(new LoginRequest("angler@example.com", "Password1!"));
                return JsonResponse(new AccessTokenResponse(
                    "Bearer",
                    "access-token",
                    1_800,
                    "refresh-token"));
            }

            request.RequestUri!.PathAndQuery.Should().Be("/api/auth/me");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("access-token");
            return JsonResponse(user);
        });
        var sut = CreateService(handler, tokenStore);

        var result = await sut.LoginAsync(
            "angler@example.com",
            "Password1!",
            TestContext.Current.CancellationToken);

        result.Should().Be(user);
        await tokenStore.Received(1).SaveTokensAsync(
            "access-token",
            "refresh-token",
            Now.AddMinutes(30));
        await tokenStore.Received(1).SaveCurrentUserAsync(user.Id, user.Email);
    }

    /// <summary>Verifies that a usable stored access token avoids a refresh request.</summary>
    [Fact]
    public async Task GetValidAccessTokenAsync_UnexpiredToken_ReturnsStoredToken()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns("stored-access-token");
        tokenStore.GetAccessTokenExpiresAtUtcAsync().Returns(Now.AddMinutes(5));
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("HTTP should not be called."));
        var sut = CreateService(handler, tokenStore);

        var result = await sut.GetValidAccessTokenAsync(
            TestContext.Current.CancellationToken);

        result.Should().Be("stored-access-token");
        await tokenStore.DidNotReceive().GetRefreshTokenAsync();
    }

    /// <summary>Verifies that a near-expiry access token is refreshed and replaced.</summary>
    [Fact]
    public async Task GetValidAccessTokenAsync_NearExpiryToken_RefreshesTokenPair()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns("old-access-token");
        tokenStore.GetAccessTokenExpiresAtUtcAsync().Returns(Now.AddSeconds(30));
        tokenStore.GetRefreshTokenAsync().Returns("stored-refresh-token");
        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be("/api/auth/refresh");
            var body = await request.Content!.ReadFromJsonAsync<RefreshTokenRequest>(ct);
            body.Should().Be(new RefreshTokenRequest("stored-refresh-token"));
            return JsonResponse(new AccessTokenResponse(
                "Bearer",
                "new-access-token",
                1_800,
                "new-refresh-token"));
        });
        var sut = CreateService(handler, tokenStore);

        var result = await sut.GetValidAccessTokenAsync(
            TestContext.Current.CancellationToken);

        result.Should().Be("new-access-token");
        await tokenStore.Received(1).SaveTokensAsync(
            "new-access-token",
            "new-refresh-token",
            Now.AddMinutes(30));
    }

    /// <summary>Verifies that concurrent callers share one token refresh operation.</summary>
    [Fact]
    public async Task GetValidAccessTokenAsync_ConcurrentCallers_RefreshesOnlyOnce()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns((string?)null);
        tokenStore.GetAccessTokenExpiresAtUtcAsync().Returns((DateTimeOffset?)null);
        tokenStore.GetRefreshTokenAsync().Returns("stored-refresh-token");
        tokenStore
            .SaveTokensAsync(
                "new-access-token",
                "new-refresh-token",
                Now.AddMinutes(30))
            .Returns(_ =>
            {
                tokenStore.GetAccessTokenAsync().Returns("new-access-token");
                tokenStore.GetAccessTokenExpiresAtUtcAsync()
                    .Returns(Now.AddMinutes(30));
                return Task.CompletedTask;
            });
        var refreshCalls = 0;
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            Interlocked.Increment(ref refreshCalls);
            await Task.Delay(50, ct);
            return JsonResponse(new AccessTokenResponse(
                "Bearer",
                "new-access-token",
                1_800,
                "new-refresh-token"));
        });
        var sut = CreateService(handler, tokenStore);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => sut.GetValidAccessTokenAsync(
                    TestContext.Current.CancellationToken)));

        results.Should().OnlyContain(token => token == "new-access-token");
        refreshCalls.Should().Be(1);
    }

    /// <summary>Verifies that a rejected refresh signs out the local session.</summary>
    [Fact]
    public async Task GetValidAccessTokenAsync_RefreshRejected_ClearsSession()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetRefreshTokenAsync().Returns("invalid-refresh-token");
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var sut = CreateService(handler, tokenStore);

        var result = await sut.GetValidAccessTokenAsync(
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
        tokenStore.Received(1).Clear();
    }

    /// <summary>Verifies that the current-user request uses the active bearer token.</summary>
    [Fact]
    public async Task GetCurrentUserAsync_ValidSession_ReturnsAndSavesUser()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns("access-token");
        tokenStore.GetAccessTokenExpiresAtUtcAsync().Returns(Now.AddMinutes(5));
        var user = new CurrentUserResponse(
            Guid.NewGuid(),
            "angler@example.com",
            Now.UtcDateTime,
            null);
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.Headers.Authorization!.Parameter.Should().Be("access-token");
            return Task.FromResult(JsonResponse(user));
        });
        var sut = CreateService(handler, tokenStore);

        var result = await sut.GetCurrentUserAsync(
            TestContext.Current.CancellationToken);

        result.Should().Be(user);
        await tokenStore.Received(1).SaveCurrentUserAsync(user.Id, user.Email);
    }

    /// <summary>Verifies that an unauthorized current-user response clears the session.</summary>
    [Fact]
    public async Task GetCurrentUserAsync_Unauthorized_ClearsSessionAndReturnsNull()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns("access-token");
        tokenStore.GetAccessTokenExpiresAtUtcAsync().Returns(Now.AddMinutes(5));
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var sut = CreateService(handler, tokenStore);

        var result = await sut.GetCurrentUserAsync(
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
        tokenStore.Received(1).Clear();
    }

    /// <summary>Verifies that logout removes the local authentication session.</summary>
    [Fact]
    public void Logout_Always_ClearsSession()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("HTTP should not be called."));
        var sut = CreateService(handler, tokenStore);

        sut.Logout();

        tokenStore.Received(1).Clear();
    }

    private static AuthenticationService CreateService(
        HttpMessageHandler handler,
        ITokenStore tokenStore)
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.fishinglog.test/")
        };

        return new AuthenticationService(
            httpClient,
            tokenStore,
            timeProvider);
    }

    private static HttpResponseMessage JsonResponse<T>(T value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(
                value,
                options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
    }
}
