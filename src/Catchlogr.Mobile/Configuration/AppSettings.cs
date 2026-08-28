using System.Reflection;
using System.Text.Json;

namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Configuration settings for the Catchlogr mobile app
/// </summary>
public class AppSettings
{
    /// <summary>
    /// API configuration settings
    /// </summary>
    public ApiSettings Api { get; set; } = new();

    /// <summary>
    /// Sync configuration settings
    /// </summary>
    public SyncSettings Sync { get; set; } = new();

    /// <summary>
    /// Database configuration settings
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public LoggingSettings? Logging { get; set; }

    /// <summary>
    /// Loads app settings from embedded JSON file
    /// </summary>
    /// <returns>Loaded app settings</returns>
    public static AppSettings Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Catchlogr.Mobile.appsettings.json";

#if DEBUG
        // Try to load Development settings first in Debug builds
        var devResourceName = "Catchlogr.Mobile.appsettings.Development.json";
        using var devStream = assembly.GetManifestResourceStream(devResourceName);
        if (devStream != null)
        {
            return LoadFromStream(devStream);
        }
#endif

        // Fall back to default appsettings.json
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        return LoadFromStream(stream);
    }

    private static AppSettings LoadFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppSettings();
    }
}

/// <summary>
/// API configuration settings
/// </summary>
public class ApiSettings
{
    /// <summary>
    /// Base URL of the API server
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";

    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 8;
}

/// <summary>
/// Sync configuration settings
/// </summary>
public class SyncSettings
{
    /// <summary>
    /// Whether to automatically sync on app startup
    /// </summary>
    public bool AutoSyncOnStartup { get; set; } = true;

    /// <summary>
    /// Interval between automatic syncs in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 15;
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Root directory under which account-specific storage is created.
    /// </summary>
    public string? RootDirectory { get; set; }

    /// <summary>
    /// SQLite database file name
    /// </summary>
    public string FileName { get; set; } = "catchlogr.db3";

}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Log level (Debug, Information, Warning, Error)
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}
