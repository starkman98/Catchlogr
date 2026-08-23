using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Contracts.CatchDTOs;
using FishingLog.Mobile.Services.Photos;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using System.Collections.ObjectModel;

namespace FishingLog.Mobile.ViewModels;

/// <summary>
/// ViewModel for adding and editing catches for a fishing trip.
/// </summary>
public partial class AddEditCatchViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ICatchLocalRepository _catchRepo;
    private readonly IFishingTripLocalRepository _tripRepo;
    private readonly IPhotoCaptureService _photoCaptureService;

    private int _tripLocalId;
    private int _catchLocalId;
    private string? _originalLocalPhotoPath;
    private string? _localPhotoPathPendingDeletion;

    /// <summary>Available bait type names for the picker.</summary>
    public ObservableCollection<string> BaitTypes { get; } = new(Enum.GetNames<BaitType>());

    /// <summary>Name of the caught species.</summary>
    [ObservableProperty] public partial string Species { get; set; } = string.Empty;

    /// <summary>Optional catch length in centimeters.</summary>
    [ObservableProperty] public partial int? Length { get; set; }

    /// <summary>Optional catch weight in grams.</summary>
    [ObservableProperty] public partial int? Weight { get; set; }

    /// <summary>Private on-device path used to preview and upload a captured photo.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoSource))]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial string? LocalPhotoPath { get; set; }

    /// <summary>Public URL assigned by the API after photo upload.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoSource))]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial string? PhotoUrl { get; set; }

    /// <summary>True when the local photo must be uploaded during the next sync.</summary>
    [ObservableProperty] public partial bool IsPhotoUploadPending { get; set; }

    /// <summary>Old server photo URL to remove after the catch update succeeds.</summary>
    [ObservableProperty] public partial string? PhotoUrlPendingDeletion { get; set; }

    /// <summary>Optional notes for the catch.</summary>
    [ObservableProperty] public partial string? Note { get; set; }

    /// <summary>Local calendar date when the fish was caught.</summary>
    [ObservableProperty] public partial DateTime CaughtDate { get; set; } = DateTime.Today;

    /// <summary>Local time of day when the fish was caught.</summary>
    [ObservableProperty] public partial TimeSpan CaughtTimeOfDay { get; set; } = DateTime.Now.TimeOfDay;

    /// <summary>Optional water depth at the catch location.</summary>
    [ObservableProperty] public partial double? Depth { get; set; }

    /// <summary>Optional GPS latitude for the catch.</summary>
    [ObservableProperty] public partial double? Latitude { get; set; }

    /// <summary>Optional GPS longitude for the catch.</summary>
    [ObservableProperty] public partial double? Longitude { get; set; }

    /// <summary>Optional bait name.</summary>
    [ObservableProperty] public partial string? BaitName { get; set; }

    /// <summary>Optional bait type name.</summary>
    [ObservableProperty] public partial string? BaitType { get; set; }

    /// <summary>Optional bait color.</summary>
    [ObservableProperty] public partial string? BaitColor { get; set; }

    /// <summary>Optional bait weight in grams.</summary>
    [ObservableProperty] public partial int? BaitWeightGrams { get; set; }

    /// <summary>Optional bait length in millimeters.</summary>
    [ObservableProperty] public partial int? BaitLengthMm { get; set; }

    /// <summary>True when editing an existing catch.</summary>
    public bool IsEditMode => _catchLocalId > 0;

    /// <summary>Best available source for the photo preview.</summary>
    public string? PhotoSource => LocalPhotoPath ?? PhotoUrl;

    /// <summary>True when the form currently has a photo.</summary>
    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoSource);

    /// <summary>
    /// Initializes a new instance of <see cref="AddEditCatchViewModel"/>.
    /// </summary>
    public AddEditCatchViewModel(
        ICatchLocalRepository catchRepo,
        IFishingTripLocalRepository tripRepo,
        IPhotoCaptureService photoCaptureService)
    {
        _catchRepo = catchRepo;
        _tripRepo = tripRepo;
        _photoCaptureService = photoCaptureService;
        ResetForm();
    }

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _tripLocalId = GetQueryInt(query, "tripLocalId");
        _catchLocalId = GetQueryInt(query, "catchLocalId");

        if (_catchLocalId > 0)
            _ = LoadCatchAsync(_catchLocalId);
        else
            ResetForm();
    }

    /// <summary>Validates and saves the catch locally, then navigates back.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_tripLocalId <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Validation", "A catch must belong to a fishing trip.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Species))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Species is required.", "OK");
            return;
        }

        var caughtAtUtc = ToUtc(CaughtDate, CaughtTimeOfDay);

        if (IsEditMode)
            await UpdateExistingCatchAsync(caughtAtUtc);
        else
            await AddNewCatchAsync(caughtAtUtc);

        await _photoCaptureService.DeleteAsync(_localPhotoPathPendingDeletion);
        _localPhotoPathPendingDeletion = null;
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Navigates back without saving.</summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!string.Equals(LocalPhotoPath, _originalLocalPhotoPath, StringComparison.Ordinal))
            await _photoCaptureService.DeleteAsync(LocalPhotoPath);

        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Opens the device camera and stores the captured photo privately.</summary>
    [RelayCommand]
    private async Task CapturePhotoAsync()
    {
        try
        {
            var capturedPath = await _photoCaptureService.CaptureAsync();
            if (capturedPath is null)
                return;

            if (!string.IsNullOrWhiteSpace(LocalPhotoPath))
            {
                if (string.Equals(LocalPhotoPath, _originalLocalPhotoPath, StringComparison.Ordinal))
                    _localPhotoPathPendingDeletion = LocalPhotoPath;
                else
                    await _photoCaptureService.DeleteAsync(LocalPhotoPath);
            }

            if (!string.IsNullOrWhiteSpace(PhotoUrl))
                PhotoUrlPendingDeletion ??= PhotoUrl;

            LocalPhotoPath = capturedPath;
            IsPhotoUploadPending = true;
        }
        catch (NotSupportedException ex)
        {
            await Shell.Current.DisplayAlertAsync("Camera unavailable", ex.Message, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync(
                "Photo error",
                "The photo could not be captured. Please check the camera permission and try again.",
                "OK");
        }
    }

    /// <summary>Removes the selected photo from this catch.</summary>
    [RelayCommand]
    private async Task RemovePhotoAsync()
    {
        if (!string.IsNullOrWhiteSpace(LocalPhotoPath))
        {
            if (string.Equals(LocalPhotoPath, _originalLocalPhotoPath, StringComparison.Ordinal))
                _localPhotoPathPendingDeletion = LocalPhotoPath;
            else
                await _photoCaptureService.DeleteAsync(LocalPhotoPath);
        }

        if (!string.IsNullOrWhiteSpace(PhotoUrl))
            PhotoUrlPendingDeletion ??= PhotoUrl;

        LocalPhotoPath = null;
        PhotoUrl = null;
        IsPhotoUploadPending = false;
    }

    private async Task LoadCatchAsync(int id)
    {
        var localCatch = await _catchRepo.GetByIdAsync(id);
        if (localCatch is null)
            return;

        _tripLocalId = localCatch.FishingTripLocalId;
        Species = localCatch.Species;
        Length = localCatch.Length;
        Weight = localCatch.Weight;
        LocalPhotoPath = localCatch.LocalPhotoPath;
        PhotoUrl = localCatch.PhotoUrl;
        IsPhotoUploadPending = localCatch.IsPhotoUploadPending;
        PhotoUrlPendingDeletion = localCatch.PhotoUrlPendingDeletion;
        _originalLocalPhotoPath = localCatch.LocalPhotoPath;
        Note = localCatch.Note;
        Depth = localCatch.Depth;
        Latitude = localCatch.Latitude;
        Longitude = localCatch.Longitude;
        BaitName = localCatch.BaitName;
        BaitType = localCatch.BaitType;
        BaitColor = localCatch.BaitColor;
        BaitWeightGrams = localCatch.BaitWeightGrams;
        BaitLengthMm = localCatch.BaitLengthMm;

        var localCaughtAt = localCatch.CaughtAt.ToLocalTime();
        CaughtDate = localCaughtAt.Date;
        CaughtTimeOfDay = localCaughtAt.TimeOfDay;
        Title = "Edit Catch";
        OnPropertyChanged(nameof(IsEditMode));
    }

    private async Task AddNewCatchAsync(DateTime caughtAtUtc)
    {
        var trip = await _tripRepo.GetByIdAsync(_tripLocalId);

        var localCatch = new CatchLocalEntity
        {
            FishingTripLocalId = _tripLocalId,
            FishingTripServerId = trip?.ServerId,
            Species = Species.Trim(),
            Length = Length,
            Weight = Weight,
            LocalPhotoPath = LocalPhotoPath,
            PhotoUrl = PhotoUrl,
            IsPhotoUploadPending = IsPhotoUploadPending,
            PhotoUrlPendingDeletion = PhotoUrlPendingDeletion,
            Note = Note,
            CaughtAt = caughtAtUtc,
            Depth = Depth,
            Latitude = Latitude,
            Longitude = Longitude,
            BaitName = BaitName,
            BaitType = BaitType,
            BaitColor = BaitColor,
            BaitWeightGrams = BaitWeightGrams,
            BaitLengthMm = BaitLengthMm
        };

        await _catchRepo.AddAsync(localCatch);
    }

    private async Task UpdateExistingCatchAsync(DateTime caughtAtUtc)
    {
        var localCatch = await _catchRepo.GetByIdAsync(_catchLocalId);
        if (localCatch is null)
            return;

        var trip = await _tripRepo.GetByIdAsync(_tripLocalId);

        localCatch.FishingTripLocalId = _tripLocalId;
        localCatch.FishingTripServerId = trip?.ServerId;
        localCatch.Species = Species.Trim();
        localCatch.Length = Length;
        localCatch.Weight = Weight;
        localCatch.LocalPhotoPath = LocalPhotoPath;
        localCatch.PhotoUrl = PhotoUrl;
        localCatch.IsPhotoUploadPending = IsPhotoUploadPending;
        localCatch.PhotoUrlPendingDeletion = PhotoUrlPendingDeletion;
        localCatch.Note = Note;
        localCatch.CaughtAt = caughtAtUtc;
        localCatch.Depth = Depth;
        localCatch.Latitude = Latitude;
        localCatch.Longitude = Longitude;
        localCatch.BaitName = BaitName;
        localCatch.BaitType = BaitType;
        localCatch.BaitColor = BaitColor;
        localCatch.BaitWeightGrams = BaitWeightGrams;
        localCatch.BaitLengthMm = BaitLengthMm;

        await _catchRepo.UpdateAsync(localCatch);
    }

    private void ResetForm()
    {
        _catchLocalId = 0;
        Species = string.Empty;
        Length = null;
        Weight = null;
        LocalPhotoPath = null;
        PhotoUrl = null;
        IsPhotoUploadPending = false;
        PhotoUrlPendingDeletion = null;
        _originalLocalPhotoPath = null;
        _localPhotoPathPendingDeletion = null;
        Note = null;
        CaughtDate = DateTime.Today;
        CaughtTimeOfDay = DateTime.Now.TimeOfDay;
        Depth = null;
        Latitude = null;
        Longitude = null;
        BaitName = null;
        BaitType = null;
        BaitColor = null;
        BaitWeightGrams = null;
        BaitLengthMm = null;
        Title = "New Catch";
        OnPropertyChanged(nameof(IsEditMode));
    }

    private static int GetQueryInt(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
            return 0;

        return value switch
        {
            int number => number,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0
        };
    }

    private static DateTime ToUtc(DateTime localDate, TimeSpan localTime)
        => DateTime.SpecifyKind(localDate.Date + localTime, DateTimeKind.Local).ToUniversalTime();
}
