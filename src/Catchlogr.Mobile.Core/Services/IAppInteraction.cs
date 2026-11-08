namespace Catchlogr.Mobile.Core.Services;

/// <summary>Provides dialogs, UI dispatch, and external links through the host.</summary>
public interface IAppInteraction
{
    /// <summary>Displays an informational alert.</summary>
    Task DisplayAlertAsync(string title, string message, string cancel);
    /// <summary>Asks the user to confirm an action.</summary>
    Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
    /// <summary>Schedules an update on the UI thread.</summary>
    void BeginInvokeOnMainThread(Action action);
    /// <summary>Opens a URI with the platform launcher.</summary>
    Task OpenUriAsync(Uri uri);
}
