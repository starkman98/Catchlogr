using FishingLog.Application.Interfaces;
using FishingLog.Application.Weather;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Json;

namespace FishingLog.Infrastructure.Weather;

/// <summary>
/// Retrieves normalized weather information from Open-Meteo.
/// </summary>
public sealed class OpenMeteoWeatherService : IWeatherService
{
    private static readonly DateTime ArchiveStartUtc =
        new(1940, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime HistoricalForecastStartUtc =
        new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly HttpClient _httpClient;
    private readonly OpenMeteoOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenMeteoWeatherService"/> class.
    /// </summary>
    public OpenMeteoWeatherService(
        HttpClient httpClient,
        IOptions<OpenMeteoOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<WeatherSnapshot?> GetWeatherAsync(
        double latitude,
        double longitude,
        DateTime timestampUtc,
        CancellationToken ct = default)
    {
        ValidateArguments(latitude, longitude, timestampUtc);

        var endpoint = SelectEndpoint(timestampUtc);
        if (endpoint is null)
        {
            return null;
        }

        var requestUri = BuildRequestUri(
            endpoint,
            latitude,
            longitude,
            timestampUtc.Date);

        using var response = await _httpClient.GetAsync(requestUri, ct);
        response.EnsureSuccessStatusCode();

        var weatherResponse =
            await response.Content.ReadFromJsonAsync<OpenMeteoWeatherResponse>(
                cancellationToken: ct);

        return MapNearestSample(weatherResponse?.Hourly, timestampUtc);
    }

    private static WeatherSnapshot? MapNearestSample(
        OpenMeteoHourlyWeather? hourly,
        DateTime timestampUtc)
    {
        if (hourly?.Time is not { Length: > 0 })
        {
            return null;
        }

        var nearestIndex = -1;
        var nearestDifference = TimeSpan.MaxValue;
        var nearestTimeUtc = DateTime.MaxValue;

        for (var index = 0; index < hourly.Time.Length; index++)
        {
            var parsedTime = ParseUtcTimestamp(hourly.Time[index]);
            if (!parsedTime.HasValue)
            {
                continue;
            }

            var difference = (parsedTime.Value - timestampUtc).Duration();
            if (difference < nearestDifference ||
                (difference == nearestDifference &&
                 parsedTime.Value < nearestTimeUtc))
            {
                nearestDifference = difference;
                nearestIndex = index;
                nearestTimeUtc = parsedTime.Value;
            }
        }

        if (nearestIndex < 0)
        {
            return null;
        }

        return new WeatherSnapshot(
            GetValue(hourly.AirTemperatureC, nearestIndex),
            GetValue(hourly.WeatherCode, nearestIndex),
            GetValue(hourly.WindSpeedMps, nearestIndex),
            GetValue(hourly.WindDirectionDegrees, nearestIndex),
            GetValue(hourly.PressureHpa, nearestIndex),
            nearestTimeUtc,
            "Open-Meteo");
    }

    private static T? GetValue<T>(T?[]? values, int index)
        where T : struct
    {
        return values is not null && index < values.Length
            ? values[index]
            : null;
    }

    private static void ValidateArguments(
        double latitude,
        double longitude,
        DateTime timestampUtc)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                latitude,
                "Latitude must be between -90 and 90.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                longitude,
                "Longitude must be between -180 and 180.");
        }

        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The weather timestamp must be UTC.",
                nameof(timestampUtc));
        }
    }

    private Uri? SelectEndpoint(DateTime timestampUtc)
    {
        var todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var requestedDateUtc = timestampUtc.Date;
        var lastForecastDateUtc = todayUtc.AddDays(15);

        if (requestedDateUtc > lastForecastDateUtc ||
            requestedDateUtc < ArchiveStartUtc)
        {
            return null;
        }

        if (requestedDateUtc >= todayUtc)
        {
            return new Uri(_options.ForecastBaseUri, "/v1/forecast");
        }

        if (requestedDateUtc >= HistoricalForecastStartUtc)
        {
            return new Uri(
                _options.HistoricalForecastBaseUri,
                "/v1/forecast");
        }

        return new Uri(_options.ArchiveBaseUri, "/v1/archive");
    }

    private Uri BuildRequestUri(
        Uri endpoint,
        double latitude,
        double longitude,
        DateTime timestampUtc)
    {
        var query = new Dictionary<string, string>
        {
            ["latitude"] = latitude.ToString(CultureInfo.InvariantCulture),
            ["longitude"] = longitude.ToString(CultureInfo.InvariantCulture),
            ["hourly"] =
                "temperature_2m,weather_code,wind_speed_10m," +
                "wind_direction_10m,pressure_msl",
            ["wind_speed_unit"] = "ms",
            ["timezone"] = "UTC",
            ["start_date"] = timestampUtc.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            ["end_date"] = timestampUtc.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            query["apikey"] = _options.ApiKey;
        }

        var queryString = string.Join(
            "&",
            query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}=" +
                $"{Uri.EscapeDataString(pair.Value)}"));

        var builder = new UriBuilder(endpoint)
        {
            Query = queryString
        };

        return builder.Uri;
    }

    private static DateTime? ParseUtcTimestamp(string value)
    {
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out var timestamp)
                ? timestamp
                : null;
    }
}
