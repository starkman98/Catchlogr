using System.Globalization;

namespace FishingLog.Mobile.Presentation;

/// <summary>
/// Formats normalized weather values for compact mobile presentation.
/// </summary>
internal static class WeatherPresentationFormatter
{
    private static readonly string[] DirectionArrows =
        ["↑", "↗", "→", "↘", "↓", "↙", "←", "↖"];

    /// <summary>Gets an app-native symbol for a WMO weather code.</summary>
    public static string GetConditionIcon(int? weatherCode) => weatherCode switch
    {
        0 => "☀️",
        1 => "🌤️",
        2 => "⛅",
        3 => "☁️",
        45 or 48 => "🌫️",
        51 or 53 or 55 or 56 or 57 => "🌦️",
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "🌧️",
        71 or 73 or 75 or 77 or 85 or 86 => "🌨️",
        95 or 96 or 99 => "⛈️",
        _ => "🌡️"
    };

    /// <summary>Formats air temperature in degrees Celsius.</summary>
    public static string FormatTemperature(double? value)
        => value.HasValue ? $"{value.Value:0.#} °C" : "Not available";

    /// <summary>Formats wind speed in metres per second.</summary>
    public static string FormatWindSpeed(double? value)
        => value.HasValue ? $"{value.Value:0.#} m/s" : "Not available";

    /// <summary>Gets an arrow showing where meteorological wind travels.</summary>
    public static string GetWindDirectionArrow(double? value)
    {
        if (!value.HasValue || !double.IsFinite(value.Value))
            return string.Empty;

        var normalizedDegrees = ((value.Value % 360) + 360) % 360;
        var travelDegrees = (normalizedDegrees + 180) % 360;
        var arrowIndex = (int)Math.Floor((travelDegrees + 22.5) / 45) % DirectionArrows.Length;
        return DirectionArrows[arrowIndex];
    }

    /// <summary>Formats the meteorological direction from which wind originates.</summary>
    public static string FormatWindDirectionDegrees(double? value)
        => value.HasValue && double.IsFinite(value.Value)
            ? $"{((value.Value % 360) + 360) % 360:0}°"
            : "Direction unavailable";

    /// <summary>Formats mean sea-level pressure in hectopascals.</summary>
    public static string FormatPressure(double? value)
        => value.HasValue ? $"{value.Value:0} hPa" : "Not available";

    /// <summary>Formats a UTC sample timestamp in the device's local time.</summary>
    public static string FormatSampleTime(DateTime value)
    {
        var utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return utcValue.ToLocalTime().ToString("MMM d, yyyy · HH:mm", CultureInfo.CurrentCulture);
    }

    /// <summary>Formats visible attribution for the weather-data provider.</summary>
    public static string FormatAttribution(string? provider)
        => string.IsNullOrWhiteSpace(provider)
            ? "Weather data provider unavailable"
            : $"Weather data by {provider.Trim()}";
}
