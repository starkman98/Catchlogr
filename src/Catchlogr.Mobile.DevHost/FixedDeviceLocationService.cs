using Catchlogr.Mobile.Core.Services;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Provides synthetic coordinates only when the scenario requests location capture.</summary>
public sealed class FixedDeviceLocationService : IDeviceLocationService
{
    /// <inheritdoc/>
    public Task<DeviceLocationCapture?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<DeviceLocationCapture?>(new(60, 15, 10));
    }
}
