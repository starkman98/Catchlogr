namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Resolves platform-specific API addresses for the Local backend.
/// </summary>
internal static class PlatformApiUrl
{
    /// <summary>
    /// Returns the configured URL unchanged unless a Local build requires a
    /// platform-specific host address.
    /// </summary>
    /// <param name="configuredUrl">The base URL from the selected settings file.</param>
    /// <param name="environment">The selected backend environment.</param>
    /// <param name="platform">The platform running the mobile app.</param>
    /// <param name="deviceType">Whether the app runs on virtual or physical hardware.</param>
    internal static string Resolve(
        string configuredUrl,
        BackendEnvironment environment,
        DevicePlatform platform,
        DeviceType deviceType)
    {
        if (environment != BackendEnvironment.Local)
        {
            return configuredUrl;
        }

        if (platform == DevicePlatform.Android &&
            deviceType == DeviceType.Virtual)
        {
            return "http://10.0.2.2:5001";
        }

        return platform == DevicePlatform.WinUI
            ? "https://localhost:7160"
            : configuredUrl;
    }
}
