using System.Text.Json.Serialization;

namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Identifies the API environment targeted by a mobile build.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BackendEnvironment>))]
public enum BackendEnvironment
{
    /// <summary>Indicates that no backend environment was configured.</summary>
    Unknown,

    /// <summary>Targets an API running on the developer's computer.</summary>
    Local,

    /// <summary>Targets the deployed development API.</summary>
    Development,

    /// <summary>Targets the production API.</summary>
    Production
}
