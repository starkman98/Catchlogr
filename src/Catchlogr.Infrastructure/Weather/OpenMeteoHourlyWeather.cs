using System.Text.Json.Serialization;

namespace Catchlogr.Infrastructure.Weather;

/// <summary>
/// Represents hourly values returned by Open-Meteo.
/// </summary>
internal sealed class OpenMeteoHourlyWeather
{
    [JsonPropertyName("time")]
    public string[]? Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public double?[]? AirTemperatureC { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public int?[]? WeatherCode { get; set; } = [];

    [JsonPropertyName("wind_speed_10m")]
    public double?[]? WindSpeedMps { get; set; } = [];

    [JsonPropertyName("wind_direction_10m")]
    public double?[]? WindDirectionDegrees { get; set; } = [];

    [JsonPropertyName("pressure_msl")]
    public double?[]? PressureHpa { get; set; } = [];
}
