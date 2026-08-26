namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Represents the action selected after pending local changes are reported.
/// </summary>
public enum LogoutDecision
{
    /// <summary>Try synchronizing the pending changes again.</summary>
    TryAgain,

    /// <summary>Sign out while preserving pending changes on this device.</summary>
    SignOutAnyway,

    /// <summary>Remain signed in.</summary>
    Cancel
}
