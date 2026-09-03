using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Catchlogr.Mobile.ViewModels;

/// <summary>Requests a password-reset code and opens the reset form.</summary>
public partial class ForgotPasswordViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<ForgotPasswordViewModel> _logger;

    /// <summary>The account email address.</summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>A safe, user-facing error.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Indicates whether an error is visible.</summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Initializes a new forgot-password ViewModel.</summary>
    public ForgotPasswordViewModel(
        IAuthenticationService authenticationService,
        IAppNavigator navigator,
        ILogger<ForgotPasswordViewModel> logger)
    {
        _authenticationService = authenticationService;
        _navigator = navigator;
        _logger = logger;
        Title = "Forgot password";
    }

    [RelayCommand]
    private async Task SendCodeAsync(CancellationToken ct)
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Enter your email address.";
            return;
        }

        try
        {
            IsBusy = true;
            await _authenticationService.ForgotPasswordAsync(Email, ct);
            await _navigator.GoToAsync(
                AppRoutes.ResetPasswordFor(Email),
                ct);
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Password recovery failed because the API was unavailable.");
            ErrorMessage =
                "Unable to reach the server. Check your connection and try again.";
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unexpected password recovery error occurred.");
            ErrorMessage = "Something went wrong while requesting the code.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenLoginAsync(CancellationToken ct)
        => _navigator.GoToAsync(AppRoutes.Login, ct);
}
