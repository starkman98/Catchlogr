using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Catchlogr.Mobile.Services;
using Catchlogr.Contracts.LocationDTOs;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Catchlogr.Mobile.ViewModels;

/// <summary>
/// ViewModel for add and edit fishing trip page.
/// Receives an optional <c>localId</c> query parameter via Shell navigation.
/// When localId is 0 or absent the page is in Add mode, otherwise Edit mode.
/// </summary>
[QueryProperty(nameof(LocalIdQuery), "localId")]
public partial class AddEditFishingTripViewModel : BaseViewModel
{
    private readonly IFishingTripLocalRepository _repository;
    private readonly IDeviceLocationService _deviceLocationService;
    private readonly ILocationSearchApiClient _locationSearchApiClient;
    private readonly ILogger<AddEditFishingTripViewModel> _logger;
    private int _localId;

    // -------------------------------------------------------------------------
    // Form Properties
    // -------------------------------------------------------------------------

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string? LocationName { get; set; }
    [ObservableProperty] public partial string? Note { get; set; }

    /// <summary>Gets or sets the latitude captured for weather lookup.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocationCaptured))]
    public partial double? Latitude { get; set; }

    /// <summary>Gets or sets the longitude captured for weather lookup.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocationCaptured))]
    public partial double? Longitude { get; set; }

    /// <summary>Gets or sets the user-facing state of the optional location capture.</summary>
    [ObservableProperty]
    public partial string LocationStatus { get; set; }
        = "Optional: use your device or search by name for weather data.";

    /// <summary>Gets or sets whether the app is currently requesting a location.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocationActionIdle))]
    public partial bool IsLocating { get; set; }

    /// <summary>Gets or sets the current named-location search results.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLocationSearchResults))]
    public partial IReadOnlyList<LocationSearchResult> LocationSearchResults { get; set; } = [];

    /// <summary>Gets or sets the user-facing state of the named-location search.</summary>
    [ObservableProperty]
    public partial string LocationSearchStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a named-location search is in progress.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocationActionIdle))]
    public partial bool IsSearchingLocation { get; set; }

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

    /// <summary>Gets whether both coordinates have been captured.</summary>
    public bool IsLocationCaptured => Latitude.HasValue && Longitude.HasValue;

    /// <summary>Gets whether the location controls and save action are available.</summary>
    public bool IsLocationActionIdle => !IsLocating && !IsSearchingLocation;

    /// <summary>Gets whether named-location results are available for selection.</summary>
    public bool HasLocationSearchResults => LocationSearchResults.Count > 0;

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
    /// <param name="repository">Repository used to persist trips locally.</param>
    /// <param name="deviceLocationService">Service used to capture device coordinates.</param>
    /// <param name="locationSearchApiClient">Client used to search locations through the API.</param>
    /// <param name="logger">Logger used for unexpected location failures.</param>
    public AddEditFishingTripViewModel(
        IFishingTripLocalRepository repository,
        IDeviceLocationService deviceLocationService,
        ILocationSearchApiClient locationSearchApiClient,
        ILogger<AddEditFishingTripViewModel> logger)
    {
        _repository = repository;
        _deviceLocationService = deviceLocationService;
        _locationSearchApiClient = locationSearchApiClient;
        _logger = logger;
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

    /// <summary>Captures the current device coordinates for weather lookup.</summary>
    [RelayCommand]
    private async Task CaptureCurrentLocationAsync(CancellationToken cancellationToken)
    {
        if (IsLocating)
            return;

        IsLocating = true;
        LocationStatus = "Finding your current location…";

        try
        {
            var location = await _deviceLocationService.GetCurrentLocationAsync(cancellationToken);
            if (location is null)
            {
                LocationStatus = "A location could not be found. Try again outdoors.";
                return;
            }

            Latitude = location.Latitude;
            Longitude = location.Longitude;
            LocationSearchResults = [];
            LocationSearchStatus = string.Empty;
            LocationStatus = FormatLocationStatus(location.AccuracyMeters);
        }
        catch (PermissionException)
        {
            LocationStatus = "Location permission was denied. You can still save the trip.";
        }
        catch (FeatureNotEnabledException)
        {
            LocationStatus = "Location services are turned off. Enable them and try again.";
        }
        catch (FeatureNotSupportedException)
        {
            LocationStatus = "Location is not supported on this device.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LocationStatus = "Location request cancelled.";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to capture the device location.");
            LocationStatus = "Location could not be captured. You can still save the trip.";
        }
        finally
        {
            IsLocating = false;
        }
    }

    /// <summary>Searches for coordinates matching the entered location name.</summary>
    [RelayCommand]
    private async Task SearchLocationAsync(CancellationToken cancellationToken)
    {
        var query = LocationName?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length is < 2 or > 100)
        {
            LocationSearchResults = [];
            LocationSearchStatus =
                "Enter a location name between 2 and 100 characters first.";
            return;
        }

        if (!IsLocationActionIdle)
            return;

        IsSearchingLocation = true;
        LocationSearchResults = [];
        LocationSearchStatus = "Searching locations…";

        try
        {
            var results = await _locationSearchApiClient.SearchAsync(
                query,
                cancellationToken);

            LocationSearchResults = results;
            LocationSearchStatus = results.Count == 0
                ? "No matching locations were found. Try adding a region or country."
                : "Select the location you want to use:";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LocationSearchStatus = "Location search cancelled.";
        }
        catch (HttpRequestException exception)
        {
            _logger.LogInformation(exception, "The location search API is unavailable.");
            LocationSearchStatus =
                "Location search is unavailable. Check your connection and try again.";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to search named locations.");
            LocationSearchStatus =
                "Locations could not be searched. You can still save the trip.";
        }
        finally
        {
            IsSearchingLocation = false;
        }
    }

    /// <summary>Uses a named-location search result for weather lookup.</summary>
    /// <param name="result">The location selected by the user.</param>
    [RelayCommand]
    private void SelectLocationSearchResult(LocationSearchResult result)
    {
        LocationName = result.DisplayName;
        Latitude = result.Latitude;
        Longitude = result.Longitude;
        LocationSearchResults = [];
        LocationSearchStatus = string.Empty;
        LocationStatus = $"Selected {result.DisplayName} for weather data.";
    }

    /// <summary>Removes the captured coordinates from the trip.</summary>
    [RelayCommand]
    private void ClearLocation()
    {
        Latitude = null;
        Longitude = null;
        LocationSearchResults = [];
        LocationSearchStatus = string.Empty;
        LocationStatus = "No precise location saved. Weather will not be fetched automatically.";
    }

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
        Latitude = trip.Latitude;
        Longitude = trip.Longitude;
        if (IsLocationCaptured)
            LocationStatus = "Precise location saved for weather data.";

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
            Latitude = Latitude,
            Longitude = Longitude,
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
        trip.Latitude = Latitude;
        trip.Longitude = Longitude;
        trip.Note = Note;
        trip.StartTime = startTime;
        trip.EndTime = endTime;
        await _repository.UpdateAsync(trip);
    }

    private static DateTime ToUtc(DateTime localDate, TimeSpan localTime)
        => DateTime.SpecifyKind(localDate.Date + localTime, DateTimeKind.Local).ToUniversalTime();

    private static string FormatLocationStatus(double? accuracyMeters)
        => accuracyMeters is > 0
            ? $"Location captured (about ±{Math.Round(accuracyMeters.Value)} m)."
            : "Location captured for weather data.";
}
