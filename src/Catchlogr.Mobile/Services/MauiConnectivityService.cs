using Catchlogr.Mobile.Core.Services;

namespace Catchlogr.Mobile.Services;

/// <summary>Reports connectivity through MAUI.</summary>
public sealed class MauiConnectivityService : IConnectivityService
{
    /// <inheritdoc/>
    public bool IsInternetAvailable => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
