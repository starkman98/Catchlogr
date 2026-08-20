using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Application.Weather;
using FishingLog.Mobile.Presentation;
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
    private static readonly Uri WeatherProviderUri = new("https://open-meteo.com/");

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

    /// <summary>True when provider weather is available for the selected trip.</summary>
    [ObservableProperty] public partial bool IsWeatherAvailable { get; set; }

    /// <summary>App-native symbol representing the WMO weather condition.</summary>
    [ObservableProperty] public partial string WeatherIcon { get; set; } = string.Empty;

    /// <summary>Readable description of the WMO weather condition.</summary>
    [ObservableProperty] public partial string WeatherCondition { get; set; } = string.Empty;

    /// <summary>Formatted air temperature measured in degrees Celsius.</summary>
    [ObservableProperty] public partial string AirTemperature { get; set; } = string.Empty;

    /// <summary>Formatted wind speed measured in metres per second.</summary>
    [ObservableProperty] public partial string WindSpeed { get; set; } = string.Empty;

    /// <summary>Arrow showing the direction in which the wind travels.</summary>
    [ObservableProperty] public partial string WindDirectionArrow { get; set; } = string.Empty;

    /// <summary>Formatted meteorological direction from which the wind originates.</summary>
    [ObservableProperty] public partial string WindDirectionDegrees { get; set; } = string.Empty;

    /// <summary>Formatted mean sea-level pressure measured in hectopascals.</summary>
    [ObservableProperty] public partial string Pressure { get; set; } = string.Empty;

    /// <summary>Local display time represented by the provider weather sample.</summary>
    [ObservableProperty] public partial string WeatherSampleTime { get; set; } = string.Empty;

    /// <summary>Visible attribution for the weather-data provider.</summary>
    [ObservableProperty] public partial string WeatherAttribution { get; set; } = string.Empty;

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
        PopulateWeather(trip);

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

    private void PopulateWeather(FishingTripLocalEntity trip)
    {
        IsWeatherAvailable = trip.WeatherSampleTimeUtc.HasValue;

        if (!IsWeatherAvailable)
        {
            WeatherIcon = string.Empty;
            WeatherCondition = string.Empty;
            AirTemperature = string.Empty;
            WindSpeed = string.Empty;
            WindDirectionArrow = string.Empty;
            WindDirectionDegrees = string.Empty;
            Pressure = string.Empty;
            WeatherSampleTime = string.Empty;
            WeatherAttribution = string.Empty;
            return;
        }

        WeatherIcon = WeatherPresentationFormatter.GetConditionIcon(trip.WeatherCode);
        WeatherCondition = WmoWeatherCodeDescriptions.GetDescription(trip.WeatherCode);
        AirTemperature = WeatherPresentationFormatter.FormatTemperature(trip.AirTemperatureC);
        WindSpeed = WeatherPresentationFormatter.FormatWindSpeed(trip.WindSpeedMps);
        WindDirectionArrow = WeatherPresentationFormatter.GetWindDirectionArrow(trip.WindDirectionDegrees);
        WindDirectionDegrees = WeatherPresentationFormatter.FormatWindDirectionDegrees(trip.WindDirectionDegrees);
        Pressure = WeatherPresentationFormatter.FormatPressure(trip.PressureHpa);
        WeatherSampleTime = WeatherPresentationFormatter.FormatSampleTime(trip.WeatherSampleTimeUtc!.Value);
        WeatherAttribution = WeatherPresentationFormatter.FormatAttribution(trip.WeatherProvider);
    }

    /// <summary>Opens the weather provider's website for attribution details.</summary>
    [RelayCommand]
    private async Task OpenWeatherProviderAsync()
        => await Launcher.Default.OpenAsync(WeatherProviderUri);

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
