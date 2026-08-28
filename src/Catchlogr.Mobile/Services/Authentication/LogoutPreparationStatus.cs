namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>
/// Describes whether local account changes are ready for a safe sign-out.
/// </summary>
public enum LogoutPreparationStatus
{
    /// <summary>No unsynchronized changes remain.</summary>
    Ready,

    /// <summary>Changes remain because the device cannot currently reach the server.</summary>
    PendingChangesOffline,

    /// <summary>Changes remain after a synchronization attempt failed or was incomplete.</summary>
    PendingChangesSyncFailed
}
