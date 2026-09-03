using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Catchlogr.Mobile.ViewModels;

/// <summary>Handles confirmation-email status and resend requests.</summary>
public partial class CheckEmailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<CheckEmailViewModel> _logger;

    /// <summary>The account email address awaiting confirmation.</summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

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

    /// <summary>Initializes a new check-email ViewModel.</summary>
    public CheckEmailViewModel(
        IAuthenticationService authenticationService,
        IAppNavigator navigator,
        ILogger<CheckEmailViewModel> logger)
    {
        _authenticationService = authenticationService;
        _navigator = navigator;
        _logger = logger;
        Title = "Check your email";
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
    private async Task ResendAsync(CancellationToken ct)
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Enter your email address.";
            return;
        }

        try
        {
            IsBusy = true;
            await _authenticationService.ResendConfirmationEmailAsync(
                Email,
                ct);
            StatusMessage =
                "If the account is awaiting confirmation, a new email has been sent.";
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Confirmation email resend failed because the API was unavailable.");
            ErrorMessage =
                "Unable to reach the server. Check your connection and try again.";
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unexpected confirmation email resend error occurred.");
            ErrorMessage = "Something went wrong while sending the email.";
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
