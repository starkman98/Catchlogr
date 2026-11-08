using Catchlogr.Mobile.Core.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>
/// Code-behind for the add/edit catch form page.
/// </summary>
public partial class AddEditCatchPage : ContentPage, IQueryAttributable
{
    /// <summary>
    /// Initializes a new instance of <see cref="AddEditCatchPage"/>.
    /// </summary>
    public AddEditCatchPage(AddEditCatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>Forwards Shell navigation values to the portable ViewModel.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ((AddEditCatchViewModel)BindingContext).ApplyQueryAttributes(query);
    }
}
