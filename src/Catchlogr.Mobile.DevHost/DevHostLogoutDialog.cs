using Catchlogr.Mobile.Core.Services.Authentication;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Declines interactive logout decisions in the console host.</summary>
public sealed class DevHostLogoutDialog : ILogoutDialogService
{
    /// <inheritdoc/>
    public Task<LogoutDecision> ConfirmAsync(LogoutPreparationResult preparation, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(LogoutDecision.Cancel);
    }
}
