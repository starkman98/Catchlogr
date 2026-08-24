using FishingLog.Mobile.ViewModels;

namespace FishingLog.Mobile.Pages;

/// <summary>Displays the account registration form.</summary>
public partial class RegisterPage : ContentPage
{
    /// <summary>Initializes a new registration page.</summary>
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
