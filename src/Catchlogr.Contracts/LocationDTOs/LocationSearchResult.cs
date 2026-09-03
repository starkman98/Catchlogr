namespace Catchlogr.Contracts.LocationDTOs;

/// <summary>
/// Represents a named location returned by a geocoding search.
/// </summary>
public sealed class LocationSearchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationSearchResult"/> class.
    /// </summary>
    /// <param name="name">The primary location name.</param>
    /// <param name="displayName">A qualified name suitable for display and selection.</param>
    /// <param name="latitude">Latitude in decimal degrees.</param>
    /// <param name="longitude">Longitude in decimal degrees.</param>
    public LocationSearchResult(
        string name,
        string displayName,
        double latitude,
        double longitude)
    {
        Name = name;
        DisplayName = displayName;
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Gets the primary location name.</summary>
    public string Name { get; }

    /// <summary>Gets the qualified location name shown to the user.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; }
}
