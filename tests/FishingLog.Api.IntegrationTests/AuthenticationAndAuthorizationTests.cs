using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FishingLog.Contracts.AuthenticationDTOs;
using FishingLog.Contracts.CatchDTOs;
using FishingLog.Contracts.FishingTripDTOs;
using FishingLog.Contracts.PhotoDTOs;
using FluentAssertions;

namespace FishingLog.Api.IntegrationTests;

/// <summary>
/// Exercises bearer authentication and cross-account resource isolation over HTTP.
/// </summary>
public sealed class AuthenticationAndAuthorizationTests
{
    private const string Password = "Password1!";

    /// <summary>Verifies that a new account can register.</summary>
    [Fact]
    public async Task Register_NewAccount_Succeeds()
    {
        using var scenario = await ApiScenario.CreateAsync();

        using var response = await RegisterAsync(
            scenario.Client,
            "new@example.com");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    /// <summary>Verifies that duplicate email registration fails.</summary>
    [Fact]
    public async Task Register_DuplicateEmail_Fails()
    {
        using var scenario = await ApiScenario.CreateAsync();
        using var first = await RegisterAsync(scenario.Client, "same@example.com");

        using var duplicate = await RegisterAsync(
            scenario.Client,
            "same@example.com");

        first.IsSuccessStatusCode.Should().BeTrue();
        duplicate.IsSuccessStatusCode.Should().BeFalse();
    }

    /// <summary>Verifies that correct credentials return access and refresh tokens.</summary>
    [Fact]
    public async Task Login_CorrectCredentials_ReturnsTokenPair()
    {
        using var scenario = await ApiScenario.CreateAsync();
        using var registration = await RegisterAsync(
            scenario.Client,
            "login@example.com");

        var tokens = await LoginAsync(scenario.Client, "login@example.com");

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.ExpiresIn.Should().BePositive();
    }

    /// <summary>Verifies that incorrect credentials return 401.</summary>
    [Fact]
    public async Task Login_IncorrectCredentials_ReturnsUnauthorized()
    {
        using var scenario = await ApiScenario.CreateAsync();
        using var registration = await RegisterAsync(
            scenario.Client,
            "wrong@example.com");

        using var response = await scenario.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new LoginRequest("wrong@example.com", "WrongPassword1!"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Verifies that a protected endpoint rejects a missing token.</summary>
    [Fact]
    public async Task ProtectedEndpoint_MissingToken_ReturnsUnauthorized()
    {
        using var scenario = await ApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/api/fishing-trips",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Verifies that a valid token can access a protected endpoint.</summary>
    [Fact]
    public async Task ProtectedEndpoint_ValidToken_Succeeds()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var tokens = await RegisterAndLoginAsync(
            scenario.Client,
            "valid@example.com");

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            "/api/fishing-trips",
            tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that a refresh token produces a new usable token pair.</summary>
    [Fact]
    public async Task Refresh_ValidRefreshToken_ReturnsNewTokenPair()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var original = await RegisterAndLoginAsync(
            scenario.Client,
            "refresh@example.com");

        using var response = await scenario.Client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(original.RefreshToken),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var refreshed = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(
            TestContext.Current.CancellationToken);

        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Verifies that the current-account endpoint returns the authenticated account.</summary>
    [Fact]
    public async Task Me_ValidToken_ReturnsAuthenticatedAccount()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var tokens = await RegisterAndLoginAsync(
            scenario.Client,
            "me@example.com");

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            "/api/auth/me",
            tokens.AccessToken);
        var account = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        account!.Email.Should().Be("me@example.com");
    }

    /// <summary>Verifies that an invalid bearer token returns 401.</summary>
    [Fact]
    public async Task ProtectedEndpoint_InvalidToken_ReturnsUnauthorized()
    {
        using var scenario = await ApiScenario.CreateAsync();

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            "/api/fishing-trips",
            "invalid-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Verifies that an expired bearer token returns 401.</summary>
    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_ReturnsUnauthorized()
    {
        using var scenario = await ApiScenario.CreateAsync(
            TimeSpan.FromMilliseconds(100));
        var tokens = await RegisterAndLoginAsync(
            scenario.Client,
            "expired@example.com");
        await Task.Delay(
            TimeSpan.FromMilliseconds(250),
            TestContext.Current.CancellationToken);

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            "/api/fishing-trips",
            tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Verifies that each account lists only its own trips.</summary>
    [Fact]
    public async Task Trips_TwoUsers_ReturnOnlyCurrentUsersTrips()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var userA = await RegisterAndLoginAsync(scenario.Client, "a-list@example.com");
        var userB = await RegisterAndLoginAsync(scenario.Client, "b-list@example.com");
        var tripA = await CreateTripAsync(scenario.Client, userA.AccessToken, "A trip");
        var tripB = await CreateTripAsync(scenario.Client, userB.AccessToken, "B trip");

        var tripsA = await GetTripsAsync(scenario.Client, userA.AccessToken);
        var tripsB = await GetTripsAsync(scenario.Client, userB.AccessToken);

        tripsA.Select(trip => trip.Id).Should().Equal(tripA.Id);
        tripsB.Select(trip => trip.Id).Should().Equal(tripB.Id);
    }

    /// <summary>Verifies that another user's trip cannot be retrieved by ID.</summary>
    [Fact]
    public async Task GetTrip_OtherUsersTrip_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var ownership = await CreateOwnedTripScenarioAsync(scenario.Client, "get");

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            $"/api/fishing-trips/{ownership.Trip.Id:D}",
            ownership.UserB.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that another user's trip cannot be updated.</summary>
    [Fact]
    public async Task UpdateTrip_OtherUsersTrip_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var ownership = await CreateOwnedTripScenarioAsync(scenario.Client, "update");
        var request = CreateUpdateTripRequest("Unauthorized update");

        using var response = await SendAuthorizedJsonAsync(
            scenario.Client,
            HttpMethod.Put,
            $"/api/fishing-trips/{ownership.Trip.Id:D}",
            ownership.UserB.AccessToken,
            request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that another user's trip cannot be deleted.</summary>
    [Fact]
    public async Task DeleteTrip_OtherUsersTrip_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var ownership = await CreateOwnedTripScenarioAsync(scenario.Client, "delete");

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Delete,
            $"/api/fishing-trips/{ownership.Trip.Id:D}",
            ownership.UserB.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that another user cannot add a catch to an owned trip.</summary>
    [Fact]
    public async Task CreateCatch_OtherUsersTrip_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var ownership = await CreateOwnedTripScenarioAsync(scenario.Client, "catch");
        var request = CreateCatchRequest();

        using var response = await SendAuthorizedJsonAsync(
            scenario.Client,
            HttpMethod.Post,
            $"/api/fishing-trips/{ownership.Trip.Id:D}/catches",
            ownership.UserB.AccessToken,
            request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that incremental synchronization is scoped to the current user.</summary>
    [Fact]
    public async Task IncrementalSync_TwoUsers_ReturnsOnlyCurrentUsersChanges()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var userA = await RegisterAndLoginAsync(scenario.Client, "a-sync@example.com");
        var userB = await RegisterAndLoginAsync(scenario.Client, "b-sync@example.com");
        var tripA = await CreateTripAsync(scenario.Client, userA.AccessToken, "A sync trip");
        await CreateTripAsync(scenario.Client, userB.AccessToken, "B sync trip");
        var since = Uri.EscapeDataString(
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("O"));

        using var response = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            $"/api/fishing-trips?modifiedSince={since}",
            userA.AccessToken);
        var trips = await response.Content.ReadFromJsonAsync<List<FishingTripResponse>>(
            TestContext.Current.CancellationToken);

        trips!.Select(trip => trip.Id).Should().Equal(tripA.Id);
    }

    /// <summary>Verifies private photo ownership for upload, download and deletion.</summary>
    [Fact]
    public async Task PrivatePhoto_CrossUserAccess_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var ownership = await CreateOwnedTripScenarioAsync(scenario.Client, "photo");
        var currentCatch = await CreateCatchAsync(
            scenario.Client,
            ownership.UserA.AccessToken,
            ownership.Trip.Id);
        var photo = await UploadPhotoAsync(
            scenario.Client,
            ownership.UserA.AccessToken,
            currentCatch.Id);

        using var ownerDownload = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            $"/api/photos/{photo.Id:D}",
            ownership.UserA.AccessToken);
        using var otherDownload = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            $"/api/photos/{photo.Id:D}",
            ownership.UserB.AccessToken);
        using var otherDelete = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Delete,
            $"/api/photos/{photo.Id:D}",
            ownership.UserB.AccessToken);

        ownerDownload.StatusCode.Should().Be(HttpStatusCode.OK);
        otherDownload.StatusCode.Should().Be(HttpStatusCode.NotFound);
        otherDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var ownerDelete = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Delete,
            $"/api/photos/{photo.Id:D}",
            ownership.UserA.AccessToken);
        using var deletedDownload = await SendAuthorizedAsync(
            scenario.Client,
            HttpMethod.Get,
            $"/api/photos/{photo.Id:D}",
            ownership.UserA.AccessToken);
        ownerDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deletedDownload.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that photo upload requires authentication.</summary>
    [Fact]
    public async Task PrivatePhoto_UnauthenticatedUpload_ReturnsUnauthorized()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var user = await RegisterAndLoginAsync(
            scenario.Client,
            "photo-auth@example.com");
        var trip = await CreateTripAsync(
            scenario.Client,
            user.AccessToken,
            "Photo auth trip");
        var currentCatch = await CreateCatchAsync(
            scenario.Client,
            user.AccessToken,
            trip.Id);

        using var response = await SendPhotoUploadAsync(
            scenario.Client,
            null,
            currentCatch.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Verifies that a photo cannot be uploaded to another user's catch.</summary>
    [Fact]
    public async Task PrivatePhoto_OtherUsersCatchUpload_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();
        var ownership = await CreateOwnedTripScenarioAsync(
            scenario.Client,
            "photo-upload");
        var currentCatch = await CreateCatchAsync(
            scenario.Client,
            ownership.UserA.AccessToken,
            ownership.Trip.Id);

        using var response = await SendPhotoUploadAsync(
            scenario.Client,
            ownership.UserB.AccessToken,
            currentCatch.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that public static upload paths are no longer served.</summary>
    [Fact]
    public async Task LegacyPublicUploadPath_Always_ReturnsNotFound()
    {
        using var scenario = await ApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/uploads/legacy-photo.jpg",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email)
        => await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, Password));

    private static async Task<AccessTokenResponse> RegisterAndLoginAsync(
        HttpClient client,
        string email)
    {
        using var registration = await RegisterAsync(client, email);
        registration.EnsureSuccessStatusCode();
        return await LoginAsync(client, email);
    }

    private static async Task<AccessTokenResponse> LoginAsync(
        HttpClient client,
        string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new LoginRequest(email, Password));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccessTokenResponse>()
            ?? throw new InvalidOperationException("Login returned no token pair.");
    }

    private static async Task<FishingTripResponse> CreateTripAsync(
        HttpClient client,
        string accessToken,
        string name)
    {
        using var response = await SendAuthorizedJsonAsync(
            client,
            HttpMethod.Post,
            "/api/fishing-trips",
            accessToken,
            CreateTripRequest(name));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FishingTripResponse>()
            ?? throw new InvalidOperationException("Trip creation returned no trip.");
    }

    private static async Task<List<FishingTripResponse>> GetTripsAsync(
        HttpClient client,
        string accessToken)
    {
        using var response = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            "/api/fishing-trips",
            accessToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<FishingTripResponse>>() ?? [];
    }

    private static async Task<CatchResponse> CreateCatchAsync(
        HttpClient client,
        string accessToken,
        Guid tripId)
    {
        using var response = await SendAuthorizedJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/fishing-trips/{tripId:D}/catches",
            accessToken,
            CreateCatchRequest());
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatchResponse>()
            ?? throw new InvalidOperationException("Catch creation returned no catch.");
    }

    private static async Task<PhotoUploadResponse> UploadPhotoAsync(
        HttpClient client,
        string accessToken,
        Guid catchId)
    {
        using var response = await SendPhotoUploadAsync(
            client,
            accessToken,
            catchId);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PhotoUploadResponse>()
            ?? throw new InvalidOperationException("Photo upload returned no metadata.");
    }

    private static async Task<HttpResponseMessage> SendPhotoUploadAsync(
        HttpClient client,
        string? accessToken,
        Guid catchId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/catches/{catchId:D}/photos");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        }

        using var form = new MultipartFormDataContent();
        using var image = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xD9]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(image, "file", "catch.jpg");
        request.Content = form;
        return await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
    }

    private static async Task<OwnedTripScenario> CreateOwnedTripScenarioAsync(
        HttpClient client,
        string suffix)
    {
        var userA = await RegisterAndLoginAsync(
            client,
            $"a-{suffix}@example.com");
        var userB = await RegisterAndLoginAsync(
            client,
            $"b-{suffix}@example.com");
        var trip = await CreateTripAsync(client, userA.AccessToken, "Owned trip");
        return new OwnedTripScenario(userA, userB, trip);
    }

    private static Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client,
        HttpMethod method,
        string uri,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendAuthorizedJsonAsync<T>(
        HttpClient client,
        HttpMethod method,
        string uri,
        string accessToken,
        T content)
    {
        var request = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(content)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        return client.SendAsync(request);
    }

    private static CreateFishingTripRequest CreateTripRequest(string name)
        => new(
            name,
            "Test lake",
            null,
            null,
            58.0,
            13.0,
            DateTime.UtcNow.AddHours(-1),
            null,
            null);

    private static UpdateFishingTripRequest CreateUpdateTripRequest(string name)
        => new(
            name,
            "Test lake",
            null,
            null,
            58.0,
            13.0,
            DateTime.UtcNow.AddHours(-1),
            null,
            null);

    private static CreateCatchRequest CreateCatchRequest()
        => new(
            "Pike",
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null,
            null,
            null,
            null);

    private sealed record OwnedTripScenario(
        AccessTokenResponse UserA,
        AccessTokenResponse UserB,
        FishingTripResponse Trip);

    private sealed class ApiScenario : IDisposable
    {
        private ApiScenario(
            FishingLogApiFactory factory,
            HttpClient client)
        {
            Factory = factory;
            Client = client;
        }

        public FishingLogApiFactory Factory { get; }

        public HttpClient Client { get; }

        public static async Task<ApiScenario> CreateAsync(
            TimeSpan? bearerTokenLifetime = null)
        {
            var factory = new FishingLogApiFactory(bearerTokenLifetime);
            await factory.InitializeAsync();
            return new ApiScenario(factory, factory.CreateClient());
        }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }
}

