using Catchlogr.Mobile.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>Displays the account login form.</summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    /// <summary>Initializes a new login page.</summary>
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.InitializeAsync();
    }
}
