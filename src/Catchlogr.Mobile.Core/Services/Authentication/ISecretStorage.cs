namespace Catchlogr.Mobile.Core.Services.Authentication;

/// <summary>Persists individual secrets using host-provided encrypted storage.</summary>
public interface ISecretStorage
{
    /// <summary>Reads a secret, or returns null if absent.</summary>
    Task<string?> GetAsync(string key);
    /// <summary>Writes a secret.</summary>
    Task SetAsync(string key, string value);
    /// <summary>Removes a secret and reports whether it existed.</summary>
    bool Remove(string key);
}
