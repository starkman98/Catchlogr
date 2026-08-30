using Catchlogr.Mobile.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>Displays email-confirmation instructions and resend controls.</summary>
public partial class CheckEmailPage : ContentPage
{
    /// <summary>Initializes a new check-email page.</summary>
    public CheckEmailPage(CheckEmailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
