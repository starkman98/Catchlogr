namespace Catchlogr.Web.Configuration;

/// <summary>Defines connectivity settings for the Catchlogr API.</summary>
public sealed class ApiOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Api";

    /// <summary>Gets or sets the absolute Catchlogr API base URL.</summary>
    public Uri BaseUrl { get; set; } = null!;

    /// <summary>Gets or sets the HTTP request timeout in seconds.</summary>
    public int Timeout { get; set; } = 30;
}
