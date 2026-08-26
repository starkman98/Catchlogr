namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Reports the state of locally saved changes before sign-out.
/// </summary>
/// <param name="Status">The preparation outcome.</param>
/// <param name="PendingChangeCount">The number of records still saved only on this device.</param>
public sealed record LogoutPreparationResult(
    LogoutPreparationStatus Status,
    int PendingChangeCount);
