using Catchlogr.Mobile.Core.Services;

namespace Catchlogr.Mobile.Services;

/// <summary>Connects portable presentation logic to MAUI dialogs and device APIs.</summary>
public sealed class MauiAppInteraction : IAppInteraction
{
    /// <inheritdoc/>
    public Task DisplayAlertAsync(string title, string message, string cancel)
        => Shell.Current.DisplayAlertAsync(title, message, cancel);
    /// <inheritdoc/>
    public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        => Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    /// <inheritdoc/>
    public void BeginInvokeOnMainThread(Action action) => MainThread.BeginInvokeOnMainThread(action);
    /// <inheritdoc/>
    public async Task OpenUriAsync(Uri uri) => await Launcher.Default.OpenAsync(uri);
}
