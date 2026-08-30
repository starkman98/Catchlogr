using System.Net;
using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Catchlogr.Mobile.ViewModels;

/// <summary>Resets an account password using the emailed Identity code.</summary>
public partial class ResetPasswordViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<ResetPasswordViewModel> _logger;

    /// <summary>The account email address.</summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>The password-reset code from the email.</summary>
    [ObservableProperty]
    public partial string ResetCode { get; set; } = string.Empty;

    /// <summary>The new password.</summary>
    [ObservableProperty]
    public partial string NewPassword { get; set; } = string.Empty;

    /// <summary>The repeated new password.</summary>
    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>A safe, user-facing error.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>A safe, user-facing success message.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Indicates whether an error is visible.</summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Indicates whether a status message is visible.</summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>Initializes a new reset-password ViewModel.</summary>
    public ResetPasswordViewModel(
        IAuthenticationService authenticationService,
        IAppNavigator navigator,
        ILogger<ResetPasswordViewModel> logger)
    {
        _authenticationService = authenticationService;
        _navigator = navigator;
        _logger = logger;
        Title = "Reset password";
    }

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("email", out var value) &&
            value is string email)
        {
            Email = email;
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(CancellationToken ct)
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        if (!ValidateInput())
            return;

        try
        {
            IsBusy = true;
            await _authenticationService.ResetPasswordAsync(
                Email,
                ResetCode,
                NewPassword,
                ct);
            ResetCode = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            StatusMessage =
                "Your password has been reset. You can now sign in.";
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ErrorMessage =
                "The code is invalid or expired, or the new password does not meet the requirements.";
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Password reset failed because the API was unavailable.");
            ErrorMessage =
                "Unable to reach the server. Check your connection and try again.";
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unexpected password reset error occurred.");
            ErrorMessage = "Something went wrong while resetting the password.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenLoginAsync(CancellationToken ct)
        => _navigator.GoToAsync(AppRoutes.Login, ct);

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(ResetCode) ||
            string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Enter your email, reset code, and new password.";
            return false;
        }

        if (!string.Equals(
            NewPassword,
            ConfirmPassword,
            StringComparison.Ordinal))
        {
            ErrorMessage = "The passwords do not match.";
            return false;
        }

        return true;
    }
}
