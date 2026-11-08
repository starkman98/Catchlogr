using Catchlogr.Mobile.Core.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>Displays email-confirmation instructions and resend controls.</summary>
public partial class CheckEmailPage : ContentPage, IQueryAttributable
{
    /// <summary>Initializes a new check-email page.</summary>
    public CheckEmailPage(CheckEmailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>Forwards Shell navigation values to the portable ViewModel.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ((CheckEmailViewModel)BindingContext).ApplyQueryAttributes(query);
    }
}
