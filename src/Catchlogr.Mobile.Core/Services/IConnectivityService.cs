namespace Catchlogr.Mobile.Core.Services;

/// <summary>Reports whether the device has Internet connectivity.</summary>
public interface IConnectivityService
{
    /// <summary>Gets whether an Internet connection is available.</summary>
    bool IsInternetAvailable { get; }
}
