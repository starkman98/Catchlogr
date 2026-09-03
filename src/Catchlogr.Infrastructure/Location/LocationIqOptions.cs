namespace Catchlogr.Infrastructure.Location;

/// <summary>
/// Configures access to the LocationIQ search API.
/// </summary>
/// <example>
/// Store <c>LocationSearch:LocationIQ:ApiKey</c> in API user-secrets for
/// local development. Never place the key in the mobile application.
/// </example>
public sealed class LocationIqOptions
{
    /// <summary>
    /// Configuration section containing LocationIQ settings.
    /// </summary>
    public const string SectionName = "LocationSearch:LocationIQ";

    /// <summary>
    /// Gets or sets the regional LocationIQ API base address.
    /// </summary>
    public Uri BaseUri { get; set; } = new("https://eu1.locationiq.com");

    /// <summary>
    /// Gets or sets the LocationIQ access token.
    /// </summary>
    public string? ApiKey { get; set; }
}
