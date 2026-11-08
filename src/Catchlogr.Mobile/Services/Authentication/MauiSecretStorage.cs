using Catchlogr.Mobile.Core.Services.Authentication;

namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>Uses MAUI secure storage for individual session secrets.</summary>
public sealed class MauiSecretStorage : ISecretStorage
{
    /// <inheritdoc/>
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    /// <inheritdoc/>
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
    /// <inheritdoc/>
    public bool Remove(string key) => SecureStorage.Default.Remove(key);
}
