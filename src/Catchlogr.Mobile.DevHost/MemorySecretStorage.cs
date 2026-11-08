using System.Collections.Concurrent;
using Catchlogr.Mobile.Core.Services.Authentication;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Keeps session secrets in process memory for a single DevHost run.</summary>
public sealed class MemorySecretStorage : ISecretStorage
{
    private readonly ConcurrentDictionary<string, string> _values = new();

    /// <inheritdoc/>
    public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));
    /// <inheritdoc/>
    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public bool Remove(string key) => _values.TryRemove(key, out _);
}
