using System.Text.Json.Serialization;

namespace Catchlogr.Infrastructure.Location;

/// <summary>
/// Represents the required portion of a LocationIQ search result.
/// </summary>
internal sealed class LocationIqSearchResult
{
    /// <summary>Gets or sets the primary feature name, when supplied.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the qualified provider display name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the latitude represented as invariant text.</summary>
    [JsonPropertyName("lat")]
    public string? Latitude { get; set; }

    /// <summary>Gets or sets the longitude represented as invariant text.</summary>
    [JsonPropertyName("lon")]
    public string? Longitude { get; set; }

    /// <summary>Gets or sets the OpenStreetMap feature class.</summary>
    [JsonPropertyName("class")]
    public string? FeatureClass { get; set; }

    /// <summary>Gets or sets the OpenStreetMap feature type.</summary>
    [JsonPropertyName("type")]
    public string? FeatureType { get; set; }
}
