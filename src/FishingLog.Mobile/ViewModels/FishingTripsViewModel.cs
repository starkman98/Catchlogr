using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Mobile.Data;
using FishingLog.Mobile.Services.Authentication;
using FishingLog.Mobile.Services.Navigation;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FishingLog.Mobile.ViewModels;

/// <summary>
/// ViewModel for the fishing trips list page.
/// Loads local trips, triggers sync and navigates to add/edit.
/// </summary>
public partial class FishingTripsViewModel : BaseViewModel
{
    private readonly IFishingTripLocalRepository _tripRepo;
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly IApiHealthClient _healthClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILocalDatabase _localDatabase;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<FishingTripsViewModel> _logger;

    private readonly SemaphoreSlim _syncLock = new(1, 1);

    [ObservableProperty]
    public partial ObservableCollection<FishingTripLocalEntity> Trips { get; set; } = [];

    /// <summary>
    /// Short status message shown below the toolbar (e.g. "Offline - trip saved locally.").
    /// Empty string means no message is displayed.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Controls the visibility of the status message bar.</summary>
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    private bool _isRefreshing;

    /// <summary>Gets or sets whether the pull-to-refresh spinner is active.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripsViewModel"/>.
    /// </summary>
    public FishingTripsViewModel(
        IFishingTripLocalRepository tripRepo,
        ISyncOrchestrator syncOrchestrator,
        IApiHealthClient healthClient,
        IAuthenticationService authenticationService,
        ILocalDatabase localDatabase,
        IAppNavigator navigator,
        ILogger<FishingTripsViewModel> logger)
    {
        _tripRepo = tripRepo;
        _syncOrchestrator = syncOrchestrator;
        _healthClient = healthClient;
        _authenticationService = authenticationService;
        _localDatabase = localDatabase;
        _navigator = navigator;
        _logger = logger;
        Title = "Fishing Trips";
    }

    /// <summary>Loads all non-deleted trips from the local database.</summary>
    [RelayCommand]
    private async Task LoadTripsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var trips = await _tripRepo.GetAllAsync();
            Trips = new ObservableCollection<FishingTripLocalEntity>(trips);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Resets the refresh spinner and runs auto-sync. Call from OnAppearing
    /// so a spinner stuck from a previous background/foreground cycle is cleared.
    /// </summary>
    public async Task OnPageAppearingAsync()
    {
        // Ensure spinner is never stuck when the page re-appears
        MainThread.BeginInvokeOnMainThread(() => IsRefreshing = false);
        await RunSyncAsync(showRefreshSpinner: false);
    }

    /// <summary>Runs the sync service then reloads the list. Used by pull-to-refresh.</summary>
    [RelayCommand]
    private Task SyncAsync() => RunSyncAsync(showRefreshSpinner: true);

    /// <summary>Internal sync logic shared by auto-sync and pull-to-refresh.</summary>
    private async Task RunSyncAsync(bool showRefreshSpinner)
    {
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogInformation("[VM] SyncCommand skipped — sync already in progress.");
            return;
        }

        _logger.LogInformation("[VM] SyncCommand started.");
        try
        {
            if (showRefreshSpinner)
                IsRefreshing = true;

            var localTrips = await _tripRepo.GetAllAsync();
            Trips = new ObservableCollection<FishingTripLocalEntity>(localTrips);

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                _ = ShowStatusMessageAsync("No internet - will sync later.");
                return;
            }

            if (!await _healthClient.IsHealthyAsync())
            {
                _ = ShowStatusMessageAsync("Server unavailable - will sync later.");
                return;
            }
            
            await _syncOrchestrator.SyncAsync();
            
            var syncedTrips = await _tripRepo.GetAllAsync();
            Trips = new ObservableCollection<FishingTripLocalEntity>(syncedTrips);
            _logger.LogInformation("[VM] SyncCommand finished. Trips loaded: {Count}", Trips.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "[VM] SyncCommand: offline/timeout — {Type}: {Message}", ex.GetType().Name, ex.Message);
            _ = ShowStatusMessageAsync("Server Offline - trip saved locally, will sync when back online.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VM] SyncCommand: unexpected error — {Type}: {Message}", ex.GetType().Name, ex.Message);
            _ = ShowStatusMessageAsync("Oops - something went wrong during sync.");
        }
        finally
        {
            IsRefreshing = false;
            _syncLock.Release();
            _logger.LogInformation("[VM] SyncCommand finally. IsRefreshing reset to false.");
        }
    }

    /// <summary>Shows a status message then clears it after a short delay.</summary>
    private async Task ShowStatusMessageAsync(string message, int displaySeconds = 4)
    {
        StatusMessage = message;
        await Task.Delay(TimeSpan.FromSeconds(displaySeconds));
        StatusMessage = string.Empty;
    }

    /// <summary>Navigates to the add trip page.</summary>
    [RelayCommand]
    private async Task AddTripAsync()
        => await Shell.Current.GoToAsync("AddEditFishingTripPage");

    /// <summary>Closes the active account database and signs out locally.</summary>
    [RelayCommand]
    private async Task LogoutAsync(CancellationToken ct)
    {
        await _syncLock.WaitAsync(ct);
        try
        {
            await _localDatabase.CloseAsync(ct);
            _authenticationService.Logout();
            Trips.Clear();
            await _navigator.GoToAsync(AppRoutes.Login, ct);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>Navigates to the details page for the selected trip.</summary>
    [RelayCommand]
    private async Task SelectTripAsync(FishingTripLocalEntity trip)
        => await Shell.Current.GoToAsync($"FishingTripDetailsPage?tripLocalId={trip.Id}");

    /// <summary>Soft-deletes a trip after confirmation.</summary>
    [RelayCommand]
    private async Task DeleteTripAsync(FishingTripLocalEntity trip)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete trip",
            $"Delete \"{trip.Name}\"?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _tripRepo.DeleteAsync(trip.Id);
        Trips.Remove(trip);
    }
}

