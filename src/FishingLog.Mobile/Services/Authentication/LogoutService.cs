using FishingLog.Mobile.Data;
using FishingLog.Sync.Abstractions;
using Microsoft.Extensions.Logging;

namespace FishingLog.Mobile.Services.Authentication;

/// <summary>
/// Coordinates synchronization and local session cleanup during sign-out.
/// </summary>
public sealed class LogoutService : ILogoutService
{
    private readonly IFishingTripLocalRepository _tripRepository;
    private readonly ICatchLocalRepository _catchRepository;
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly IConnectivity _connectivity;
    private readonly IApiHealthClient _healthClient;
    private readonly ILocalDatabase _localDatabase;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<LogoutService> _logger;

    /// <summary>Initializes a logout service with local-data and session dependencies.</summary>
    public LogoutService(
        IFishingTripLocalRepository tripRepository,
        ICatchLocalRepository catchRepository,
        ISyncOrchestrator syncOrchestrator,
        IConnectivity connectivity,
        IApiHealthClient healthClient,
        ILocalDatabase localDatabase,
        IAuthenticationService authenticationService,
        ILogger<LogoutService> logger)
    {
        _tripRepository = tripRepository;
        _catchRepository = catchRepository;
        _syncOrchestrator = syncOrchestrator;
        _connectivity = connectivity;
        _healthClient = healthClient;
        _localDatabase = localDatabase;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<LogoutPreparationResult> PrepareAsync(
        CancellationToken ct = default)
    {
        var pendingCount = await CountPendingChangesAsync(ct);
        if (pendingCount == 0)
            return new(LogoutPreparationStatus.Ready, 0);

        if (_connectivity.NetworkAccess != NetworkAccess.Internet ||
            !await _healthClient.IsHealthyAsync(ct))
        {
            return new(
                LogoutPreparationStatus.PendingChangesOffline,
                pendingCount);
        }

        try
        {
            await _syncOrchestrator.SyncAsync(ct);
            pendingCount = await CountPendingChangesAsync(ct);
            return pendingCount == 0
                ? new(LogoutPreparationStatus.Ready, 0)
                : new(
                    LogoutPreparationStatus.PendingChangesSyncFailed,
                    pendingCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            TimeoutException)
        {
            _logger.LogWarning(
                exception,
                "Unable to synchronize pending changes before sign-out.");
            return new(
                LogoutPreparationStatus.PendingChangesSyncFailed,
                pendingCount);
        }
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(CancellationToken ct = default)
    {
        try
        {
            await _localDatabase.CloseAsync(ct);
        }
        finally
        {
            _authenticationService.Logout();
        }
    }

    private async Task<int> CountPendingChangesAsync(CancellationToken ct)
    {
        var trips = await _tripRepository.GetDirtyAsync(ct);
        var catches = await _catchRepository.GetDirtyAsync(ct);
        return trips.Count + catches.Count;
    }
}
