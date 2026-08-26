namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Shows the user a sign-out choice when changes remain only on this device.
/// </summary>
public interface ILogoutDialogService
{
    /// <summary>Gets the user's decision for pending local changes.</summary>
    Task<LogoutDecision> ConfirmAsync(
        LogoutPreparationResult preparation,
        CancellationToken ct = default);
}
