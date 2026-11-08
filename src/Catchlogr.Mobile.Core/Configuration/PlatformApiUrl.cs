namespace Catchlogr.Mobile.Core.Configuration;

/// <summary>
/// Resolves platform-specific API addresses for the Local backend.
/// </summary>
public static class PlatformApiUrl
{
    /// <summary>
    /// Returns the configured URL unchanged unless a Local build requires a
    /// platform-specific host address.
    /// </summary>
    /// <param name="configuredUrl">The base URL from the selected settings file.</param>
    /// <param name="environment">The selected backend environment.</param>
    /// <param name="isAndroid">Whether the host is Android.</param>
    /// <param name="isVirtual">Whether the host uses virtual hardware.</param>
    /// <param name="isWindows">Whether the host is Windows.</param>
    public static string Resolve(
        string configuredUrl,
        BackendEnvironment environment,
        bool isAndroid,
        bool isVirtual,
        bool isWindows)
    {
        if (environment != BackendEnvironment.Local)
        {
            return configuredUrl;
        }

        if (isAndroid && isVirtual)
        {
            return "http://10.0.2.2:5001";
        }

        return isWindows
            ? "https://localhost:7160"
            : configuredUrl;
    }
}
