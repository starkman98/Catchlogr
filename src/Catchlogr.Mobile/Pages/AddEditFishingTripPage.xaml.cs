using Catchlogr.Mobile.Core.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>
/// Code-behind for the add/edit fishing trip form page.
/// </summary>
public partial class AddEditFishingTripPage : ContentPage, IQueryAttributable
{
    public AddEditFishingTripPage(AddEditFishingTripViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>Forwards Shell navigation values to the portable ViewModel.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("localId", out var value))
            ((AddEditFishingTripViewModel)BindingContext).LocalIdQuery = Uri.UnescapeDataString(value.ToString() ?? string.Empty);
    }
}
