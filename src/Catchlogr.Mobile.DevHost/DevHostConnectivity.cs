using Catchlogr.Mobile.Core.Services;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Allows sync attempts; the HTTP clients establish actual API availability.</summary>
public sealed class DevHostConnectivity : IConnectivityService
{
    /// <inheritdoc/>
    public bool IsInternetAvailable => true;
}
