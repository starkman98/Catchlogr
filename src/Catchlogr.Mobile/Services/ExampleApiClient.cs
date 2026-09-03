using Catchlogr.Mobile.Configuration;

namespace Catchlogr.Mobile.Services;

/// <summary>
/// Example service showing how to use AppSettings
/// </summary>
public class ExampleApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    /// <summary>
    /// Initializes a new instance of the ExampleApiClient
    /// </summary>
    /// <param name="apiSettings">API configuration settings from DI</param>
    public ExampleApiClient(ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_apiSettings.Timeout)
        };
    }

    /// <summary>
    /// Example method using the API base URL from settings
    /// </summary>
    public async Task<string> GetHealthAsync(CancellationToken ct = default)
    {
        // The base URL comes from appsettings.json (or appsettings.Development.json in debug)
        var response = await _httpClient.GetAsync("/health", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
