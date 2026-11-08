using Catchlogr.Mobile.Core.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>
/// Code-behind for the fishing trip details page.
/// </summary>
public partial class FishingTripDetailsPage : ContentPage, IQueryAttributable
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
    /// <summary>Forwards Shell navigation values to the portable ViewModel.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("tripLocalId", out var value))
            ((FishingTripDetailsViewModel)BindingContext).TripLocalIdQuery = Uri.UnescapeDataString(value.ToString() ?? string.Empty);
    }
}
