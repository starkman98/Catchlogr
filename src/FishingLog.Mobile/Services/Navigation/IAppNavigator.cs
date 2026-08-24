namespace FishingLog.Mobile.Services.Navigation;

/// <summary>Provides application navigation without coupling ViewModels to MAUI Shell.</summary>
public interface IAppNavigator
{
    /// <summary>Navigates to the specified Shell route.</summary>
    /// <param name="route">The absolute or relative Shell route.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    Task GoToAsync(string route, CancellationToken ct = default);
}
