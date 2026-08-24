using FishingLog.Mobile.Data;

namespace FishingLog.Mobile;

/// <summary>Represents the FishingLog MAUI application.</summary>
public partial class App : Microsoft.Maui.Controls.Application
{
	/// <summary>Initializes the application resources.</summary>
	public App()
	{
		InitializeComponent();
	}

    /// <summary>
    /// Creates the initial app window with AppShell as the root page.
    /// Preferred over setting MainPage directly in MAUI.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());

}
