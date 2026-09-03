using Catchlogr.Mobile.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>Displays the password-reset-code request form.</summary>
public partial class ForgotPasswordPage : ContentPage
{
    /// <summary>Initializes a new forgot-password page.</summary>
    public ForgotPasswordPage(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
