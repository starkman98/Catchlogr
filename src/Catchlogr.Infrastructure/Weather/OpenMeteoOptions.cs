namespace Catchlogr.Infrastructure.Weather;

/// <summary>
/// Configures access to the Open-Meteo weather APIs.
/// </summary>
public sealed class OpenMeteoOptions
{
    /// <summary>
    /// Configuration section containing Open-Meteo settings.
    /// </summary>
    public const string SectionName = "Weather:OpenMeteo";

    /// <summary>
    /// Gets or sets the forecast API base address.
    /// </summary>
    public Uri ForecastBaseUri { get; set; } =
        new("https://api.open-meteo.com");

    /// <summary>
    /// Gets or sets the historical forecast API base address.
    /// </summary>
    public Uri HistoricalForecastBaseUri { get; set; } =
        new("https://historical-forecast-api.open-meteo.com");

    /// <summary>
    /// Gets or sets the historical archive API base address.
    /// </summary>
    public Uri ArchiveBaseUri { get; set; } =
        new("https://archive-api.open-meteo.com");

    /// <summary>
    /// Gets or sets the optional commercial API key.
    /// </summary>
    public string? ApiKey { get; set; }
}
