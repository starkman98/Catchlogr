using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Catchlogr.Mobile.Services;

/// <summary>
/// Captures device coordinates through the .NET MAUI geolocation API.
/// </summary>
public sealed class DeviceLocationService : IDeviceLocationService
{
    private static readonly TimeSpan LocationTimeout = TimeSpan.FromSeconds(10);
    private readonly IGeolocation _geolocation;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceLocationService"/> class.
    /// </summary>
    /// <param name="geolocation">The platform geolocation implementation.</param>
    public DeviceLocationService(IGeolocation geolocation)
    {
        _geolocation = geolocation;
    }

    /// <inheritdoc />
    public async Task<DeviceLocationCapture?> GetCurrentLocationAsync(
        CancellationToken cancellationToken = default)
    {
        var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (permission != PermissionStatus.Granted)
            permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (permission != PermissionStatus.Granted)
            throw new PermissionException("Location permission was not granted.");

        var request = new GeolocationRequest(GeolocationAccuracy.High, LocationTimeout);
        var location = await _geolocation.GetLocationAsync(request, cancellationToken);

        return location is null
            ? null
            : new DeviceLocationCapture(location.Latitude, location.Longitude, location.Accuracy);
    }
}
