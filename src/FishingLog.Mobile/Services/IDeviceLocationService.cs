namespace FishingLog.Mobile.Services;

/// <summary>
/// Captures the current foreground location of the device.
/// </summary>
public interface IDeviceLocationService
{
    /// <summary>
    /// Requests permission and captures the current device location.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the location request.</param>
    /// <returns>The captured location, or <see langword="null"/> when no location could be obtained.</returns>
    Task<DeviceLocationCapture?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
