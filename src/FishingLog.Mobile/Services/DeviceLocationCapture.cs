namespace FishingLog.Mobile.Services;

/// <summary>
/// Represents a location captured from the current device.
/// </summary>
public sealed class DeviceLocationCapture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceLocationCapture"/> class.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees.</param>
    /// <param name="longitude">Longitude in decimal degrees.</param>
    /// <param name="accuracyMeters">Estimated horizontal accuracy in metres, when available.</param>
    public DeviceLocationCapture(double latitude, double longitude, double? accuracyMeters)
    {
        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
    }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; }

    /// <summary>Gets the estimated horizontal accuracy in metres, when available.</summary>
    public double? AccuracyMeters { get; }
}
