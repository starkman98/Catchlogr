using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishingLog.Mobile.Services.Authentication;
using FishingLog.Mobile.Services.Navigation;
using Microsoft.Extensions.Logging;

namespace FishingLog.Mobile.ViewModels;

/// <summary>Handles login, session restoration, and navigation to registration.</summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ITokenStore _tokenStore;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<LoginViewModel> _logger;
    private bool _hasCheckedExistingSession;

    /// <summary>The email address entered by the user.</summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>The password entered by the user.</summary>
    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    /// <summary>A safe, user-facing authentication error.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Indicates whether an authentication error is visible.</summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Initializes a new login ViewModel.</summary>
    public LoginViewModel(
        IAuthenticationService authenticationService,
        ITokenStore tokenStore,
        IAppNavigator navigator,
        ILogger<LoginViewModel> logger)
    {
        _authenticationService = authenticationService;
        _tokenStore = tokenStore;
        _navigator = navigator;
        _logger = logger;
        Title = "Sign in";
    }

    /// <summary>Restores a locally known account so offline users can reach cached data.</summary>
    /// <param name="ct">A token that can cancel the operation.</param>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_hasCheckedExistingSession)
            return;

        _hasCheckedExistingSession = true;
        var currentUserId = await _tokenStore.GetCurrentUserIdAsync();
        if (currentUserId.HasValue)
            await _navigator.GoToAsync(AppRoutes.FishingTrips, ct);
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken ct)
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter your email address and password.";
            return;
        }

        try
        {
            IsBusy = true;
            await _authenticationService.LoginAsync(Email, Password, ct);
            Password = string.Empty;
            await _navigator.GoToAsync(AppRoutes.FishingTrips, ct);
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            ErrorMessage = "The email address or password is incorrect.";
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Login failed because the authentication API was unavailable.");
            ErrorMessage = "Unable to reach the server. Check your connection and try again.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected login error occurred.");
            ErrorMessage = "Something went wrong while signing in.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenRegisterAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        return _navigator.GoToAsync(AppRoutes.Register, ct);
    }
}
