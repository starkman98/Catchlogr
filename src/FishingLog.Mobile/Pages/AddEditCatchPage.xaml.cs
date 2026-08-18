using FishingLog.Mobile.ViewModels;

namespace FishingLog.Mobile.Pages;

/// <summary>
/// Code-behind for the add/edit catch form page.
/// </summary>
public partial class AddEditCatchPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of <see cref="AddEditCatchPage"/>.
    /// </summary>
    public AddEditCatchPage(AddEditCatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
