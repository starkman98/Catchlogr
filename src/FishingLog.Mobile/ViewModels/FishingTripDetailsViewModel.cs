using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using System.Collections.ObjectModel;

namespace FishingLog.Mobile.ViewModels;

/// <summary>
/// ViewModel for displaying one fishing trip and its local catches.
/// </summary>
[QueryProperty(nameof(TripLocalIdQuery), "tripLocalId")]
public partial class FishingTripDetailsViewModel : BaseViewModel
{
    private readonly IFishingTripLocalRepository _tripRepo;
    private readonly ICatchLocalRepository _catchRepo;
    private readonly ISyncOrchestrator _syncOrchestrator;

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private int _tripLocalId;

    /// <summary>Catches connected to the selected fishing trip.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CatchesCount))]
    public partial ObservableCollection<CatchLocalEntity> Catches { get; set; } = [];

    /// <summary>Display name of the selected fishing trip.</summary>
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    /// <summary>Human-readable location name for the selected fishing trip.</summary>
    [ObservableProperty] public partial string? LocationName { get; set; }

    /// <summary>Free-text notes for the selected fishing trip.</summary>
    [ObservableProperty] public partial string? Note { get; set; }

    /// <summary>Local calendar date when the trip started.</summary>
    [ObservableProperty] public partial DateTime StartDate { get; set; } = DateTime.Today;

    /// <summary>Local time of day when the trip started.</summary>
    [ObservableProperty] public partial TimeSpan StartTimeOfDay { get; set; } = DateTime.Now.TimeOfDay;

    /// <summary>True when the selected trip has an end date.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEndDateVisible))]
    public partial bool HasEndDate { get; set; }

    /// <summary>Local calendar date when the trip ended.</summary>
    [ObservableProperty] public partial DateTime EndDate { get; set; } = DateTime.Today;

    /// <summary>Local time of day when the trip ended.</summary>
    [ObservableProperty] public partial TimeSpan EndTimeOfDay { get; set; } = DateTime.Now.TimeOfDay;

    /// <summary>True while the page is running pull-to-refresh sync.</summary>
    [ObservableProperty] public partial bool IsRefreshing { get; set; }

    /// <summary>Controls visibility of the end date fields.</summary>
    public bool IsEndDateVisible => HasEndDate;

    /// <summary>Number of catches connected to the selected trip.</summary>
    public int CatchesCount => Catches.Count;

    /// <summary>
    /// Set by Shell navigation when opening the trip details page.
    /// </summary>
    public string? TripLocalIdQuery
    {
        set
        {
            if (int.TryParse(value, out var id) && id > 0)
            {
                _tripLocalId = id;
                _ = LoadAsync();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripDetailsViewModel"/>.
    /// </summary>
    public FishingTripDetailsViewModel(
        IFishingTripLocalRepository tripRepo,
        ICatchLocalRepository catchRepo,
        ISyncOrchestrator syncOrchestrator)
    {
        _tripRepo = tripRepo;
        _catchRepo = catchRepo;
        _syncOrchestrator = syncOrchestrator;
        Title = "Trip Details";
    }

    /// <summary>Reloads the selected trip and its catches from the local database.</summary>
    public async Task LoadAsync()
    {
        if (_tripLocalId <= 0)
            return;

        var trip = await _tripRepo.GetByIdAsync(_tripLocalId);
        if (trip is null)
            return;

        Name = trip.Name;
        LocationName = trip.LocationName;
        Note = trip.Note;

        var localStart = trip.StartTime.ToLocalTime();
        StartDate = localStart.Date;
        StartTimeOfDay = localStart.TimeOfDay;

        HasEndDate = trip.EndTime.HasValue;
        if (trip.EndTime.HasValue)
        {
            var localEnd = trip.EndTime.Value.ToLocalTime();
            EndDate = localEnd.Date;
            EndTimeOfDay = localEnd.TimeOfDay;
        }

        Title = trip.Name;

        var catches = await _catchRepo.GetByTripIdAsync(_tripLocalId);
        Catches = new ObservableCollection<CatchLocalEntity>(catches);
    }

    /// <summary>Runs a full sync and reloads the selected trip and its catches.</summary>
    [RelayCommand]
    private async Task SyncAsync()
    {
        if (!await _syncLock.WaitAsync(0))
            return;

        try
        {
            IsRefreshing = true;
            await _syncOrchestrator.SyncAsync();
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
            _syncLock.Release();
        }
    }

    /// <summary>Navigates to the edit page for the selected trip.</summary>
    [RelayCommand]
    private async Task EditTripAsync()
        => await Shell.Current.GoToAsync($"AddEditFishingTripPage?localId={_tripLocalId}");

    /// <summary>Navigates to the add catch page for the selected trip.</summary>
    [RelayCommand]
    private async Task AddCatchAsync()
        => await Shell.Current.GoToAsync($"AddEditCatchPage?tripLocalId={_tripLocalId}");

    /// <summary>Navigates to the edit catch page for the selected catch.</summary>
    [RelayCommand]
    private async Task SelectCatchAsync(CatchLocalEntity localCatch)
        => await Shell.Current.GoToAsync($"AddEditCatchPage?tripLocalId={_tripLocalId}&catchLocalId={localCatch.Id}");

    /// <summary>Soft-deletes a catch after confirmation.</summary>
    [RelayCommand]
    private async Task DeleteCatchAsync(CatchLocalEntity localCatch)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete catch",
            $"Delete \"{localCatch.Species}\"?",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        await _catchRepo.DeleteAsync(localCatch.Id);
        Catches.Remove(localCatch);
        OnPropertyChanged(nameof(CatchesCount));
    }
}
