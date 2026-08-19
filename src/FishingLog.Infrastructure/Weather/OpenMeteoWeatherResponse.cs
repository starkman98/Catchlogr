using System.Text.Json.Serialization;

namespace FishingLog.Infrastructure.Weather;

/// <summary>
/// Represents the required portion of an Open-Meteo response.
/// </summary>
internal sealed class OpenMeteoWeatherResponse
{
    /// <summary>
    /// Gets or sets the returned hourly weather data.
    /// </summary>
    [JsonPropertyName("hourly")]
    public OpenMeteoHourlyWeather? Hourly { get; set; }
}