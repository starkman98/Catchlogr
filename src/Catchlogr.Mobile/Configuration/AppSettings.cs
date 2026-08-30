using System.Reflection;
using System.Text.Json;

namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Configuration settings for the Catchlogr mobile app
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the backend environment selected for this build.
    /// </summary>
    public BackendEnvironment BackendEnvironment { get; set; }

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
#if LOCAL
        const string resourceName = "Catchlogr.Mobile.appsettings.Local.json";
        const BackendEnvironment expectedEnvironment = BackendEnvironment.Local;
#elif DEBUG
        const string resourceName = "Catchlogr.Mobile.appsettings.Development.json";
        const BackendEnvironment expectedEnvironment = BackendEnvironment.Development;
#else
        const string resourceName = "Catchlogr.Mobile.appsettings.json";
        const BackendEnvironment expectedEnvironment = BackendEnvironment.Production;
#endif

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        var settings = LoadFromStream(stream);
        settings.Validate(expectedEnvironment);
        return settings;
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

    private void Validate(BackendEnvironment expectedEnvironment)
    {
        if (BackendEnvironment != expectedEnvironment)
        {
            throw new InvalidOperationException(
                $"The mobile build expects the '{expectedEnvironment}' backend, " +
                $"but the selected settings declare '{BackendEnvironment}'.");
        }

        if (!Uri.TryCreate(Api.BaseUrl, UriKind.Absolute, out var apiUri))
        {
            throw new InvalidOperationException(
                "Api:BaseUrl must be an absolute URL.");
        }

        if (apiUri.Scheme != Uri.UriSchemeHttp &&
            apiUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Api:BaseUrl must use HTTP or HTTPS.");
        }

        if (BackendEnvironment != BackendEnvironment.Local &&
            apiUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Development and Production API URLs must use HTTPS.");
        }

        if (Api.Timeout <= 0)
        {
            throw new InvalidOperationException(
                "Api:Timeout must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Database.FileName) ||
            Path.GetFileName(Database.FileName) != Database.FileName)
        {
            throw new InvalidOperationException(
                "Database:FileName must contain a file name without a path.");
        }
    }
}
