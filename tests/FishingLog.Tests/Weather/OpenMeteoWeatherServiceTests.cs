using FishingLog.Infrastructure.Weather;
using FishingLog.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text;

namespace FishingLog.Tests.Weather;

/// <summary>
/// Tests for <see cref="OpenMeteoWeatherService"/>.
/// </summary>
public sealed class OpenMeteoWeatherServiceTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies request parameters, invariant formatting, and normalized mapping.
    /// </summary>
    [Fact]
    public async Task GetWeatherAsync_ValidRequest_BuildsExpectedRequestAndMapsSample()
    {
        const string json = """
            {
              "hourly": {
                "time": ["2026-08-19T10:00", "2026-08-19T11:00"],
                "temperature_2m": [13.1, 14.2],
                "weather_code": [2, 3],
                "wind_speed_10m": [2.4, 3.5],
                "wind_direction_10m": [180, 225],
                "pressure_msl": [1011.2, 1012.3]
              }
            }
            """;

        var handler = CreateJsonHandler(json);
        var service = CreateService(handler);
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");

        try
        {
            var result = await service.GetWeatherAsync(
                59.3293,
                18.0686,
                Utc(2026, 8, 19, 10, 40),
                TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!.AirTemperatureC.Should().Be(14.2);
            result.WeatherCode.Should().Be(3);
            result.WindSpeedMps.Should().Be(3.5);
            result.WindDirectionDegrees.Should().Be(225);
            result.PressureHpa.Should().Be(1012.3);
            result.WeatherSampleTimeUtc.Should().Be(Utc(2026, 8, 19, 11));
            result.WeatherSampleTimeUtc.Kind.Should().Be(DateTimeKind.Utc);
            result.WeatherProvider.Should().Be("Open-Meteo");

            handler.LastRequestUri.Should().NotBeNull();
            var requestUri = handler.LastRequestUri!;
            var query = ParseQuery(requestUri);

            requestUri.Host.Should().Be("api.open-meteo.com");
            requestUri.AbsolutePath.Should().Be("/v1/forecast");
            query["latitude"].Should().Be("59.3293");
            query["longitude"].Should().Be("18.0686");
            query["hourly"].Should().Be(
                "temperature_2m,weather_code,wind_speed_10m," +
                "wind_direction_10m,pressure_msl");
            query["wind_speed_unit"].Should().Be("ms");
            query["timezone"].Should().Be("UTC");
            query["start_date"].Should().Be("2026-08-19");
            query["end_date"].Should().Be("2026-08-19");
            query.Should().NotContainKey("apikey");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// Verifies endpoint selection across all supported date ranges.
    /// </summary>
    [Theory]
    [InlineData(2026, 8, 19, "api.open-meteo.com", "/v1/forecast")]
    [InlineData(2026, 9, 3, "api.open-meteo.com", "/v1/forecast")]
    [InlineData(2026, 8, 18, "historical-forecast-api.open-meteo.com", "/v1/forecast")]
    [InlineData(2022, 1, 1, "historical-forecast-api.open-meteo.com", "/v1/forecast")]
    [InlineData(2021, 12, 31, "archive-api.open-meteo.com", "/v1/archive")]
    [InlineData(1940, 1, 1, "archive-api.open-meteo.com", "/v1/archive")]
    public async Task GetWeatherAsync_SupportedDate_SelectsExpectedEndpoint(
        int year,
        int month,
        int day,
        string expectedHost,
        string expectedPath)
    {
        var handler = CreateJsonHandler("{}");
        var service = CreateService(handler);

        await service.GetWeatherAsync(
            59,
            18,
            Utc(year, month, day, 12),
            TestContext.Current.CancellationToken);

        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.Host.Should().Be(expectedHost);
        handler.LastRequestUri.AbsolutePath.Should().Be(expectedPath);
    }

    /// <summary>
    /// Verifies that dates outside provider coverage do not send a request.
    /// </summary>
    [Theory]
    [InlineData(2026, 9, 4)]
    [InlineData(1939, 12, 31)]
    public async Task GetWeatherAsync_UnsupportedDate_ReturnsNullWithoutRequest(
        int year,
        int month,
        int day)
    {
        var handler = CreateJsonHandler("{}");
        var service = CreateService(handler);

        var result = await service.GetWeatherAsync(
            59,
            18,
            Utc(year, month, day, 12),
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
        handler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that a configured commercial API key is included in the query.
    /// </summary>
    [Fact]
    public async Task GetWeatherAsync_ConfiguredApiKey_AddsApiKeyParameter()
    {
        var handler = CreateJsonHandler("{}");
        var options = new OpenMeteoOptions { ApiKey = "test key" };
        var service = CreateService(handler, options);

        await service.GetWeatherAsync(
            59,
            18,
            Utc(2026, 8, 19, 12),
            TestContext.Current.CancellationToken);

        var query = ParseQuery(handler.LastRequestUri!);
        query["apikey"].Should().Be("test key");
    }

    /// <summary>
    /// Verifies that the earlier sample wins an equal-distance tie.
    /// </summary>
    [Fact]
    public async Task GetWeatherAsync_EqualDistanceSamples_SelectsEarlierSample()
    {
        const string json = """
            {
              "hourly": {
                "time": ["2026-08-19T12:00", "2026-08-19T10:00"],
                "temperature_2m": [12, 10]
              }
            }
            """;

        var service = CreateService(CreateJsonHandler(json));

        var result = await service.GetWeatherAsync(
            59,
            18,
            Utc(2026, 8, 19, 11),
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.AirTemperatureC.Should().Be(10);
        result.WeatherSampleTimeUtc.Should().Be(Utc(2026, 8, 19, 10));
    }

    /// <summary>
    /// Verifies that arrays shorter than the time array map absent values to null.
    /// </summary>
    [Fact]
    public async Task GetWeatherAsync_ShortValueArrays_MapsMissingValuesToNull()
    {
        const string json = """
            {
              "hourly": {
                "time": ["2026-08-19T10:00", "2026-08-19T11:00"],
                "temperature_2m": [10]
              }
            }
            """;

        var service = CreateService(CreateJsonHandler(json));

        var result = await service.GetWeatherAsync(
            59,
            18,
            Utc(2026, 8, 19, 11),
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.AirTemperatureC.Should().BeNull();
        result.WeatherCode.Should().BeNull();
        result.WindSpeedMps.Should().BeNull();
        result.WindDirectionDegrees.Should().BeNull();
        result.PressureHpa.Should().BeNull();
    }

    /// <summary>
    /// Verifies that absent, empty, and malformed hourly samples return null.
    /// </summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"hourly\":{\"time\":[]}}")]
    [InlineData("{\"hourly\":{\"time\":[\"not-a-time\"]}}")]
    public async Task GetWeatherAsync_NoValidHourlySamples_ReturnsNull(string json)
    {
        var service = CreateService(CreateJsonHandler(json));

        var result = await service.GetWeatherAsync(
            59,
            18,
            Utc(2026, 8, 19, 12),
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies latitude validation, including non-finite values.
    /// </summary>
    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task GetWeatherAsync_InvalidLatitude_Throws(double latitude)
    {
        var service = CreateService(CreateJsonHandler("{}"));

        var action = () => service.GetWeatherAsync(
            latitude,
            18,
            Utc(2026, 8, 19));

        var exception = await action.Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
        exception.Which.ParamName.Should().Be("latitude");
    }

    /// <summary>
    /// Verifies longitude validation, including non-finite values.
    /// </summary>
    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public async Task GetWeatherAsync_InvalidLongitude_Throws(double longitude)
    {
        var service = CreateService(CreateJsonHandler("{}"));

        var action = () => service.GetWeatherAsync(
            59,
            longitude,
            Utc(2026, 8, 19));

        var exception = await action.Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
        exception.Which.ParamName.Should().Be("longitude");
    }

    /// <summary>
    /// Verifies that non-UTC timestamps are rejected.
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task GetWeatherAsync_NonUtcTimestamp_Throws(DateTimeKind kind)
    {
        var service = CreateService(CreateJsonHandler("{}"));
        var timestamp = DateTime.SpecifyKind(new DateTime(2026, 8, 19), kind);

        var action = () => service.GetWeatherAsync(59, 18, timestamp);

        var exception = await action.Should().ThrowAsync<ArgumentException>();
        exception.Which.ParamName.Should().Be("timestampUtc");
    }

    /// <summary>
    /// Verifies that caller cancellation is propagated.
    /// </summary>
    [Fact]
    public async Task GetWeatherAsync_CancelledRequest_PropagatesCancellation()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = CreateService(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => service.GetWeatherAsync(
            59,
            18,
            Utc(2026, 8, 19, 12),
            cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that provider HTTP errors remain visible to the caller.
    /// </summary>
    [Fact]
    public async Task GetWeatherAsync_UnsuccessfulResponse_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var service = CreateService(handler);

        var action = () => service.GetWeatherAsync(
            59,
            18,
            Utc(2026, 8, 19, 12));

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    private static OpenMeteoWeatherService CreateService(
        StubHttpMessageHandler handler,
        OpenMeteoOptions? options = null)
    {
        return new OpenMeteoWeatherService(
            new HttpClient(handler),
            Options.Create(options ?? new OpenMeteoOptions()),
            new FixedTimeProvider(FixedUtcNow));
    }

    private static StubHttpMessageHandler CreateJsonHandler(string json)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
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

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour = 0,
        int minute = 0)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}
