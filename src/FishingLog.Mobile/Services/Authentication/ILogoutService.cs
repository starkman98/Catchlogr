namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Prepares and completes a local account sign-out.
/// </summary>
public interface ILogoutService
{
    /// <summary>
    /// Attempts to synchronize pending local changes and reports anything that remains.
    /// </summary>
    Task<LogoutPreparationResult> PrepareAsync(CancellationToken ct = default);

    /// <summary>
    /// Closes active account storage and removes the local authentication session.
    /// </summary>
    Task CompleteAsync(CancellationToken ct = default);
}
