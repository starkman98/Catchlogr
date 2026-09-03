namespace Catchlogr.Mobile.Services.Navigation;

/// <summary>Contains the application Shell routes used by ViewModels.</summary>
public static class AppRoutes
{
    /// <summary>The absolute login route.</summary>
    public const string Login = "//LoginPage";

    /// <summary>The relative registration route.</summary>
    public const string Register = "RegisterPage";

    /// <summary>The relative check-email route.</summary>
    public const string CheckEmail = "CheckEmailPage";

    /// <summary>The relative forgot-password route.</summary>
    public const string ForgotPassword = "ForgotPasswordPage";

    /// <summary>The relative reset-password route.</summary>
    public const string ResetPassword = "ResetPasswordPage";

    /// <summary>The absolute fishing-trips route.</summary>
    public const string FishingTrips = "//FishingTripsPage";

    /// <summary>Builds the check-email route for an account.</summary>
    /// <param name="email">The account email address.</param>
    /// <returns>A Shell route containing the encoded email address.</returns>
    public static string CheckEmailFor(string email)
        => WithEmail(CheckEmail, email);

    /// <summary>Builds the reset-password route for an account.</summary>
    /// <param name="email">The account email address.</param>
    /// <returns>A Shell route containing the encoded email address.</returns>
    public static string ResetPasswordFor(string email)
        => WithEmail(ResetPassword, email);

    private static string WithEmail(string route, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return $"{route}?email={Uri.EscapeDataString(email.Trim())}";
    }
}
