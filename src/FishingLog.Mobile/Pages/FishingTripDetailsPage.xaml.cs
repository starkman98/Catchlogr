using FishingLog.Mobile.ViewModels;

namespace FishingLog.Mobile.Pages;

/// <summary>
/// Code-behind for the fishing trip details page.
/// </summary>
public partial class FishingTripDetailsPage : ContentPage
{
    private readonly FishingTripDetailsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripDetailsPage"/>.
    /// </summary>
    public FishingTripDetailsPage(FishingTripDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Reloads local trip details and catches whenever the page appears.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadAsync();
    }
}
