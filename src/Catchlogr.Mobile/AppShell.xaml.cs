using Catchlogr.Mobile.Pages;

namespace Catchlogr.Mobile;

/// <summary>
/// Shell code-behind. Registers all navigation routes here.
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes that are navigated to programmatically (not in the shell tab bar)
        Routing.RegisterRoute("AddEditFishingTripPage", typeof(AddEditFishingTripPage));
        Routing.RegisterRoute("FishingTripDetailsPage", typeof(FishingTripDetailsPage));
        Routing.RegisterRoute("AddEditCatchPage", typeof(AddEditCatchPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(CheckEmailPage), typeof(CheckEmailPage));
        Routing.RegisterRoute(
            nameof(ForgotPasswordPage),
            typeof(ForgotPasswordPage));
        Routing.RegisterRoute(
            nameof(ResetPasswordPage),
            typeof(ResetPasswordPage));
    }
}
