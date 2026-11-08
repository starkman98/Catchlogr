using Catchlogr.Mobile.Core.Services;
using Catchlogr.Mobile.Core.Services.Navigation;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Executes callbacks inline and fails on unexpected interactive UI operations.</summary>
public sealed class DevHostInteraction : IAppInteraction, IAppNavigator
{
    /// <inheritdoc/>
    public Task GoToAsync(string route, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public Task DisplayAlertAsync(string title, string message, string cancel)
        => throw new InvalidOperationException("The smoke scenario encountered a UI validation error.");
    /// <inheritdoc/>
    public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        => throw new NotSupportedException("Confirmation dialogs are not supported by DevHost.");
    /// <inheritdoc/>
    public void BeginInvokeOnMainThread(Action action) => action();
    /// <inheritdoc/>
    public Task OpenUriAsync(Uri uri) => throw new NotSupportedException("External links are not supported by DevHost.");
}
