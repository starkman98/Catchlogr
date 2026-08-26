namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Presents user-friendly sign-out choices through the current MAUI Shell.
/// </summary>
public sealed class ShellLogoutDialogService : ILogoutDialogService
{
    private const string TryAgain = "Try again";
    private const string SignOutAnyway = "Sign out anyway";
    private const string Cancel = "Cancel";

    /// <inheritdoc/>
    public async Task<LogoutDecision> ConfirmAsync(
        LogoutPreparationResult preparation,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var message = preparation.Status == LogoutPreparationStatus.PendingChangesOffline
            ? "You’re offline. Some changes are saved only on this device. They will be available when you sign in to this account again on this device."
            : "Some changes are saved only on this device and couldn’t be backed up to your account. They will remain on this device, but won’t be available elsewhere until you sign in and sync.";

        var selection = await Shell.Current.DisplayActionSheetAsync(
            message,
            Cancel,
            null,
            TryAgain,
            SignOutAnyway);

        return selection switch
        {
            TryAgain => LogoutDecision.TryAgain,
            SignOutAnyway => LogoutDecision.SignOutAnyway,
            _ => LogoutDecision.Cancel
        };
    }
}
