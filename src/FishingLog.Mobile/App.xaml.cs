using FishingLog.Mobile.Data;

namespace FishingLog.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
	private readonly ILocalDatabase _localDatabase;
	public App(ILocalDatabase localDatabase)
	{
		InitializeComponent();
		_localDatabase = localDatabase;
	}

    /// <summary>
    /// Creates the initial app window with AppShell as the root page.
    /// Preferred over setting MainPage directly in MAUI.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());

    /// <summary>
    /// Called when the app starts. Initialises the local SQLite database.
    /// </summary>
    protected override async void OnStart()
    {
        base.OnStart();
        await _localDatabase.InitializeAsync();
    }
}
