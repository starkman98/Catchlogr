using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Mobile.Data;
using FishingLog.Mobile.Services.Authentication;
using FishingLog.Mobile.Services.Navigation;
using Microsoft.Extensions.Logging;

namespace FishingLog.Mobile.ViewModels;

/// <summary>Handles account registration and the initial login.</summary>
public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILocalDatabase _localDatabase;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<RegisterViewModel> _logger;

    /// <summary>The email address for the new account.</summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>The password for the new account.</summary>
    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    /// <summary>The repeated password used to catch typing mistakes.</summary>
    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>A safe, user-facing registration error.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Indicates whether a registration error is visible.</summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Initializes a new registration ViewModel.</summary>
    public RegisterViewModel(
        IAuthenticationService authenticationService,
        ILocalDatabase localDatabase,
        IAppNavigator navigator,
        ILogger<RegisterViewModel> logger)
    {
        _authenticationService = authenticationService;
        _localDatabase = localDatabase;
        _navigator = navigator;
        _logger = logger;
        Title = "Create account";
    }

    [RelayCommand]
    private async Task RegisterAsync(CancellationToken ct)
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;
        if (!ValidateInput())
            return;

        try
        {
            IsBusy = true;
            await _authenticationService.RegisterAsync(Email, Password, ct);
            var user = await _authenticationService.LoginAsync(
                Email,
                Password,
                ct);
            await _localDatabase.ActivateAsync(user.Id, ct);
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            await _navigator.GoToAsync(AppRoutes.FishingTrips, ct);
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            ErrorMessage = "The account could not be created. Check the email and password requirements.";
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Registration failed because the authentication API was unavailable.");
            ErrorMessage = "Unable to reach the server. Check your connection and try again.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected registration error occurred.");
            ErrorMessage = "Something went wrong while creating the account.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenLoginAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        return _navigator.GoToAsync(AppRoutes.Login, ct);
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter an email address and password.";
            return false;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "The passwords do not match.";
            return false;
        }

        return true;
    }
}
