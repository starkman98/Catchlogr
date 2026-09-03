namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Defines API client configuration settings.
/// </summary>
public class ApiSettings
{
    /// <summary>Gets or sets the API server base URL.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the HTTP request timeout in seconds.</summary>
    public int Timeout { get; set; } = 8;
}
