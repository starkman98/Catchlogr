using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;

namespace FishingLog.Mobile.ViewModels;

/// <summary>
/// ViewModel for add and edit fishing trip page.
/// Receives an optional <c>localId</c> query parameter via Shell navigation.
/// When localId is 0 or absent the page is in Add mode, otherwise Edit mode.
/// </summary>
[QueryProperty(nameof(LocalIdQuery), "localId")]
public partial class AddEditFishingTripViewModel : BaseViewModel
{
    private readonly IFishingTripLocalRepository _repository;
    private int _localId;

    // -------------------------------------------------------------------------
    // Form Properties
    // -------------------------------------------------------------------------

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string? LocationName { get; set; }
    [ObservableProperty] public partial string? Note { get; set; }

    // DatePicker and TimePicker are separate controls in MAUI
    [ObservableProperty] public partial DateTime StartDate { get; set; } = DateTime.Today;
    [ObservableProperty] public partial TimeSpan StartTimeOfDay { get; set; } = DateTime.Now.TimeOfDay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEndDateVisible))]
    public partial bool HasEndDate { get; set; }

    [ObservableProperty] public partial DateTime EndDate { get; set; } = DateTime.Today;
    [ObservableProperty] public partial TimeSpan EndTimeOfDay { get; set; } = DateTime.Now.TimeOfDay;

    /// <summary>Controls visibility of the end date picker.</summary>
    public bool IsEndDateVisible => HasEndDate;

    // -------------------------------------------------------------------------
    // Shell query property — called by MAUI when navigating with ?localId=n
    // -------------------------------------------------------------------------

    /// <summary>
    /// Set by Shell navigation when editing an existing trip.
    /// Uses a string because Shell always passes query parameters as strings.
    /// </summary>
    public string? LocalIdQuery
    {
        set
        {
            if (int.TryParse(value, out var id) && id > 0)
                _ = LoadTripAsync(id);
        }
    }

    /// <summary>True when editing an existing trip, false when adding a new one.</summary>
    public bool IsEditMode => _localId > 0;

    /// <summary>
    /// Initializes a new instance of <see cref="AddEditFishingTripViewModel"/>.
    /// </summary>
    public AddEditFishingTripViewModel(IFishingTripLocalRepository repository)
    {
        _repository = repository;
        Title = "New Trip";
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    /// <summary>Validates and saves the trip locally, then navigates back.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Name is required.", "OK");
            return;
        }

        var startTimeUtc = ToUtc(StartDate.Date, StartTimeOfDay);
        var endTimeUtc = HasEndDate ? (DateTime?)ToUtc(EndDate, EndTimeOfDay) : null;

        if (endTimeUtc.HasValue && endTimeUtc < startTimeUtc)
        {
            await Shell.Current.DisplayAlertAsync("Validation", "End date must be after start date.", "OK");
            return;
        }

        if (IsEditMode)
            await UpdateExistingTripAsync(startTimeUtc, endTimeUtc);
        else
            await AddNewTripAsync(startTimeUtc, endTimeUtc);

        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Navigates back without saving.</summary>
    [RelayCommand]
    private async Task CancelAsync()
        => await Shell.Current.GoToAsync("..");

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task LoadTripAsync(int id)
    {
        var trip = await _repository.GetByIdAsync(id);
        if (trip is null) return;

        _localId = id;
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
        Title = "Edit Trip";
    }

    private async Task AddNewTripAsync(DateTime startTime, DateTime? endTime)
    {
        var trip = new FishingTripLocalEntity
        {
            Name = Name,
            LocationName = LocationName,
            Note = Note,
            StartTime = startTime,
            EndTime = endTime,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(trip);
    }

    private async Task UpdateExistingTripAsync(DateTime startTime, DateTime? endTime)
    {
        var trip = await _repository.GetByIdAsync(_localId);
        if (trip is null) return;

        trip.Name = Name;
        trip.LocationName = LocationName;
        trip.Note = Note;
        trip.StartTime = startTime;
        trip.EndTime = endTime;
        await _repository.UpdateAsync(trip);
    }

    private static DateTime ToUtc(DateTime localDate, TimeSpan localTime)
        => DateTime.SpecifyKind(localDate.Date + localTime, DateTimeKind.Local).ToUniversalTime();
}