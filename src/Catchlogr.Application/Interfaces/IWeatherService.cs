using Catchlogr.Application.Weather;

namespace Catchlogr.Application.Interfaces;

/// <summary>
/// Retrieves normalized weather conditions for fishing-trip locations.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets the weather sample nearest to the supplied UTC timestamp.
    /// </summary>
    Task<WeatherSnapshot?> GetWeatherAsync(
        double latitude,
        double longitude,
        DateTime timestampUtc,
        CancellationToken ct = default);
}
