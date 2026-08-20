using FishingLog.Infrastructure.Location;
using FishingLog.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace FishingLog.Tests.Location;

/// <summary>
/// Tests for <see cref="LocationIqLocationSearchService"/>.
/// </summary>
public sealed class LocationIqLocationSearchServiceTests
{
    /// <summary>
    /// Verifies request construction and mapping of provider results.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ValidQuery_BuildsRequestAndMapsResults()
    {
        const string json = """
            [
              {
                "name": "Vänern",
                "display_name": "Vänern, Sweden",
                "lat": "58.9",
                "lon": "13.5",
                "class": "natural",
                "type": "water"
              }
            ]
            """;
        var handler = CreateJsonHandler(json);
        var service = CreateService(handler);

        var results = await service.SearchAsync(
            "  Vänern, Sweden  ",
            TestContext.Current.CancellationToken);

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Vänern");
        results[0].DisplayName.Should().Be("Vänern, Sweden");
        results[0].Latitude.Should().Be(58.9);
        results[0].Longitude.Should().Be(13.5);

        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.Host.Should().Be("eu1.locationiq.com");
        handler.LastRequestUri.AbsolutePath.Should().Be("/v1/search");
        var query = ParseQuery(handler.LastRequestUri);
        query["key"].Should().Be("test key");
        query["q"].Should().Be("Vänern, Sweden");
        query["format"].Should().Be("json");
        query["addressdetails"].Should().Be("1");
        query["normalizeaddress"].Should().Be("1");
        query["accept-language"].Should().Be("en");
        query["limit"].Should().Be("10");
        query["source"].Should().Be("nom");
    }

    /// <summary>
    /// Verifies a configured regional endpoint is used.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ConfiguredEndpoint_UsesConfiguredEndpoint()
    {
        var handler = CreateJsonHandler("[]");
        var options = CreateOptions();
        options.BaseUri = new Uri("https://us1.locationiq.com");
        var service = CreateService(handler, options);

        await service.SearchAsync("Vänern", TestContext.Current.CancellationToken);

        handler.LastRequestUri!.Host.Should().Be("us1.locationiq.com");
    }

    /// <summary>
    /// Verifies water features are returned before non-water results while
    /// provider order remains stable inside each group.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MixedFeatures_PrioritizesWaterFeatures()
    {
        const string json = """
            [
              { "name": "Lake City", "display_name": "Lake City", "lat": "30", "lon": "-82", "class": "place", "type": "city" },
              { "name": "Vänern", "display_name": "Vänern", "lat": "58", "lon": "13", "class": "natural", "type": "water" },
              { "name": "Klarälven", "display_name": "Klarälven", "lat": "59", "lon": "13", "class": "waterway", "type": "river" },
              { "name": "Reservoir", "display_name": "Reservoir", "lat": "57", "lon": "12", "class": "natural", "type": "reservoir" }
            ]
            """;
        var service = CreateService(CreateJsonHandler(json));

        var results = await service.SearchAsync(
            "Lake",
            TestContext.Current.CancellationToken);

        results.Select(result => result.Name).Should().ContainInOrder(
            "Vänern",
            "Reservoir",
            "Klarälven",
            "Lake City");
    }

    /// <summary>
    /// Verifies invalid and incomplete provider rows are omitted safely.
    /// </summary>
    [Fact]
    public async Task SearchAsync_InvalidProviderRows_FiltersInvalidResults()
    {
        const string json = """
            [
              { "display_name": "", "lat": "59", "lon": "13" },
              { "display_name": "Invalid latitude", "lat": "91", "lon": "13" },
              { "display_name": "Invalid number", "lat": "not-a-number", "lon": "13" },
              { "display_name": "Valid, Sweden", "lat": "59", "lon": "13" }
            ]
            """;
        var service = CreateService(CreateJsonHandler(json));

        var results = await service.SearchAsync(
            "Valid",
            TestContext.Current.CancellationToken);

        results.Should().ContainSingle(result => result.Name == "Valid");
    }

    /// <summary>
    /// Verifies empty and not-found provider responses map to an empty collection.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.OK, "[]")]
    [InlineData(HttpStatusCode.NotFound, "{}")]
    public async Task SearchAsync_NoResults_ReturnsEmptyCollection(
        HttpStatusCode statusCode,
        string json)
    {
        var service = CreateService(CreateJsonHandler(json, statusCode));

        var results = await service.SearchAsync(
            "Unknown",
            TestContext.Current.CancellationToken);

        results.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies a missing API key is rejected before an HTTP request is sent.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MissingApiKey_ThrowsWithoutRequest()
    {
        var handler = CreateJsonHandler("[]");
        var options = CreateOptions();
        options.ApiKey = null;
        var service = CreateService(handler, options);

        var action = () => service.SearchAsync("Vänern");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
        handler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies query validation occurs before an HTTP request is sent.
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task SearchAsync_InvalidQuery_ThrowsWithoutRequest(string query)
    {
        var handler = CreateJsonHandler("[]");
        var service = CreateService(handler);

        var action = () => service.SearchAsync(query);

        await action.Should().ThrowAsync<ArgumentException>();
        handler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies caller cancellation is propagated.
    /// </summary>
    [Fact]
    public async Task SearchAsync_CancelledRequest_PropagatesCancellation()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = CreateService(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => service.SearchAsync("Vänern", cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private static LocationIqLocationSearchService CreateService(
        StubHttpMessageHandler handler,
        LocationIqOptions? options = null)
    {
        return new LocationIqLocationSearchService(
            new HttpClient(handler),
            Options.Create(options ?? CreateOptions()));
    }

    private static LocationIqOptions CreateOptions()
        => new() { ApiKey = "test key" };

    private static StubHttpMessageHandler CreateJsonHandler(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }));
    }

    private static Dictionary<string, string> ParseQuery(Uri uri)
    {
        return uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1]));
    }
}
