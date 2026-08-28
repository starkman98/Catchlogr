namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Resolves the correct API base URL for the current platform and build configuration.
/// Only applies overrides in DEBUG builds — Release always uses the configured value.
/// </summary>
internal static class PlatformApiUrl
{
    /// <summary>
    /// Returns the base URL to use for the current environment:
    /// <list type="bullet">
    ///   <item>Android emulator  → http://10.0.2.2:5001     (emulator loopback to host)</item>
    ///   <item>iOS simulator     → https://localhost:5001    (simulator shares host network)</item>
    ///   <item>Windows           → https://localhost:5001    (runs on the same machine)</item>
    ///   <item>Physical device   → uses the value from appsettings (set your LAN IP there)</item>
    ///   <item>Release builds    → always uses the value from appsettings</item>
    /// </list>
    /// </summary>
    /// <param name="configuredUrl">The base URL from appsettings.json as the fallback.</param>
    internal static string Resolve(string configuredUrl)
    {
#if DEBUG
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            // Android emulator uses 10.0.2.2 as an alias for the host machine
            // Physical Android device must use your LAN IP — set it in appsettings.Development.json
            return DeviceInfo.Current.DeviceType == DeviceType.Virtual
                ? "http://10.0.2.2:5001"
                : configuredUrl;
        }

        if (DeviceInfo.Current.Platform == DevicePlatform.iOS ||
            DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
        {
            // iOS/macOS simulator shares the host machine's network stack
            // Physical iOS device must use your LAN IP — set it in appsettings.Development.json
            return DeviceInfo.Current.DeviceType == DeviceType.Virtual
                ? "https://localhost:5001"
                : configuredUrl;
        }

        // Windows (WinUI) — always the same machine as the API
        return "https://localhost:5001";
#else
        return configuredUrl;
#endif
    }
}
