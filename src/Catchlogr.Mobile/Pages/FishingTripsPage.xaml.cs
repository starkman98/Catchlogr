using Catchlogr.Mobile.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>
/// Code-behind for the fishing trips list page.
/// Reloads trips from the local database every time the page appears.
/// </summary>
public partial class FishingTripsPage : ContentPage
{
    private readonly FishingTripsViewModel _viewModel;

    public FishingTripsPage(FishingTripsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Called every time this page becomes visible (including when navigating back from add/edit).
    /// Ensures the list is always fresh after saving a trip.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.OnPageAppearingAsync();
    }
}