using System.Net;
using System.Net.Http.Json;
using Catchlogr.Contracts.AuthenticationDTOs;
using Catchlogr.Mobile.Core.Data;
using Catchlogr.Mobile.Core.Services.Authentication;
using Catchlogr.Mobile.DevHost;
using Catchlogr.Sync.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Api.IntegrationTests;

/// <summary>Exercises the composed DevHost against the real authenticated API with temporary SQLite.</summary>
public sealed class DevHostScenarioTests
{
    private const string Password = "reeds beside quiet water";

    /// <summary>Separate runs create owned trips, clear secrets, remove local files, and keep remote trips isolated.</summary>
    [Fact]
    public async Task Run_ConfirmedAccounts_SyncsTripsAndCleansEachLocalSession()
    {
        using var factory = new CatchlogrApiFactory(requireConfirmedEmail: true);
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();
        foreach (var email in new[] { "first-devhost@example.com", "second-devhost@example.com" })
        {
            await RegisterConfirmedAsync(factory, client, email);
            var options = new DevHostOptions { TripName = "DevHost scenario" };
            var session = CreateSession(factory, options);
            var tokens = session.Services.GetRequiredService<ITokenStore>();
            string? accountDirectory = null;
            try
            {
                var serverId = await DevHostScenario.RunAsync(session, options, email, Password, TestContext.Current.CancellationToken);
                serverId.Should().NotBeEmpty();
                var trips = await session.Services.GetRequiredService<IFishingTripApiClient>()
                    .GetAllAsync(TestContext.Current.CancellationToken);
                trips.Should().ContainSingle(trip => trip.Id == serverId && trip.Name == options.TripName);
                var database = session.Services.GetRequiredService<ILocalDatabase>();
                accountDirectory = database.ActiveAccountDirectory;
                Directory.Exists(accountDirectory).Should().BeTrue();
                (await session.Services.GetRequiredService<IFishingTripLocalRepository>()
                    .GetDirtyAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
            }
            finally { await session.DisposeAsync(); }
            Directory.Exists(accountDirectory).Should().BeFalse();
            (await tokens.GetAccessTokenAsync()).Should().BeNull();
            (await tokens.GetRefreshTokenAsync()).Should().BeNull();
            (await tokens.GetCurrentUserIdAsync()).Should().BeNull();
        }
    }

    /// <summary>A swallowed sync upload failure is detected rather than reported as a successful smoke test.</summary>
    [Fact]
    public async Task Run_UploadUnavailable_RejectsSuccessAndCleansLocalState()
    {
        using var factory = new CatchlogrApiFactory(requireConfirmedEmail: true);
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();
        const string email = "upload-devhost@example.com";
        await RegisterConfirmedAsync(factory, client, email);
        var options = new DevHostOptions();
        var session = CreateSession(factory, options, rejectUpload: true);
        var database = session.Services.GetRequiredService<ILocalDatabase>();
        var tokens = session.Services.GetRequiredService<ITokenStore>();
        string? directory = null;
        try
        {
            var action = () => DevHostScenario.RunAsync(session, options, email, Password, TestContext.Current.CancellationToken);
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Sync did not upload*");
            directory = database.ActiveAccountDirectory;
            (await session.Services.GetRequiredService<IFishingTripLocalRepository>()
                .GetDirtyAsync(TestContext.Current.CancellationToken)).Should().ContainSingle();
        }
        finally { await session.DisposeAsync(); }
        Directory.Exists(directory).Should().BeFalse();
        database.ActiveUserId.Should().BeNull();
        (await tokens.GetAccessTokenAsync()).Should().BeNull();
    }

    /// <summary>Rejected credentials never activate account storage or leave a token.</summary>
    [Fact]
    public async Task Run_InvalidLogin_DoesNotActivateStorage()
    {
        using var factory = new CatchlogrApiFactory();
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        var options = new DevHostOptions();
        await using var session = CreateSession(factory, options);
        var action = () => DevHostScenario.RunAsync(session, options, "missing@example.com", Password, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<HttpRequestException>();

        session.Services.GetRequiredService<ILocalDatabase>().ActiveUserId.Should().BeNull();
        (await session.Services.GetRequiredService<ITokenStore>().GetAccessTokenAsync()).Should().BeNull();
    }

    /// <summary>Cancellation during upload never reports success and still permits complete local cleanup.</summary>
    [Fact]
    public async Task Run_CancelledDuringUpload_CleansActiveSession()
    {
        using var factory = new CatchlogrApiFactory(requireConfirmedEmail: true);
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();
        const string email = "cancel-devhost@example.com";
        await RegisterConfirmedAsync(factory, client, email);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var options = new DevHostOptions();
        var session = DevHostSession.Create(options, services => services.ConfigureHttpClientDefaults(builder =>
            builder.ConfigurePrimaryHttpMessageHandler(() => new CancelTripUploadHandler(factory.Server.CreateHandler(), cancellation))));
        var database = session.Services.GetRequiredService<ILocalDatabase>();
        var tokens = session.Services.GetRequiredService<ITokenStore>();
        string? directory = null;
        try
        {
            var action = () => DevHostScenario.RunAsync(session, options, email, Password, cancellation.Token);
            await action.Should().ThrowAsync<OperationCanceledException>();
            cancellation.IsCancellationRequested.Should().BeTrue();
            directory = database.ActiveAccountDirectory;
        }
        finally { await session.DisposeAsync(); }
        Directory.Exists(directory).Should().BeFalse();
        database.ActiveUserId.Should().BeNull();
        (await tokens.GetAccessTokenAsync()).Should().BeNull();
    }

    private static DevHostSession CreateSession(CatchlogrApiFactory factory, DevHostOptions options, bool rejectUpload = false)
        => DevHostSession.Create(options, services => services.ConfigureHttpClientDefaults(builder =>
            builder.ConfigurePrimaryHttpMessageHandler(() => rejectUpload
                ? new RejectTripUploadHandler(factory.Server.CreateHandler())
                : factory.Server.CreateHandler())));

    private static async Task RegisterConfirmedAsync(CatchlogrApiFactory factory, HttpClient client, string email)
    {
        using var registration = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, Password), TestContext.Current.CancellationToken);
        registration.EnsureSuccessStatusCode();
        var link = factory.EmailSender.ConfirmationLinks.Last();
        using var confirmation = await client.GetAsync(new Uri(WebUtility.HtmlDecode(link)).PathAndQuery,
            TestContext.Current.CancellationToken);
        confirmation.EnsureSuccessStatusCode();
    }

}
