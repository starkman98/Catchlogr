using Catchlogr.Mobile.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>Displays the code-based password reset form.</summary>
public partial class ResetPasswordPage : ContentPage
{
    /// <summary>Initializes a new reset-password page.</summary>
    public ResetPasswordPage(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
