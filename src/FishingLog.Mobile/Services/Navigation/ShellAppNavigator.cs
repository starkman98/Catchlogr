namespace FishingLog.Mobile.Services.Navigation;

/// <summary>Navigates through the active MAUI Shell.</summary>
public sealed class ShellAppNavigator : IAppNavigator
{
    /// <inheritdoc/>
    public Task GoToAsync(string route, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        ct.ThrowIfCancellationRequested();

        var shell = Shell.Current
            ?? throw new InvalidOperationException("The application Shell is not available.");

        return shell.GoToAsync(route).WaitAsync(ct);
    }
}
