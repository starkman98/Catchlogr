namespace FishingLog.Application.Weather;

/// <summary>
/// Represents normalized weather conditions for one UTC timestamp.
/// </summary>
/// <param name="AirTemperatureC">Air temperature in degrees Celsius.</param>
/// <param name="WeatherCode">WMO weather interpretation code.</param>
/// <param name="WindSpeedMps">Wind speed at 10 metres in metres per second.</param>
/// <param name="WindDirectionDegrees">Wind direction at 10 metres in degrees.</param>
/// <param name="PressureHpa">Mean sea-level pressure in hectopascals.</param>
/// <param name="WeatherSampleTimeUtc">UTC timestamp represented by this sample.</param>
/// <param name="WeatherProvider">Name of the weather-data provider.</param>
public sealed record WeatherSnapshot(
    double? AirTemperatureC,
    int? WeatherCode,
    double? WindSpeedMps,
    double? WindDirectionDegrees,
    double? PressureHpa,
    DateTime WeatherSampleTimeUtc,
    string WeatherProvider);
