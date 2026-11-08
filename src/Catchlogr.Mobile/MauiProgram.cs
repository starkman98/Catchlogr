using Catchlogr.Mobile.Core;
using Catchlogr.Mobile.Services;
using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.Services.Photos;
using Catchlogr.Mobile.Core.Services;
using Catchlogr.Mobile.Core.Configuration;
using Catchlogr.Mobile.Pages;
using Catchlogr.Mobile.Core.Services.Authentication;
using Catchlogr.Mobile.Core.Services.Navigation;
using Catchlogr.Mobile.Core.Services.Photos;
using Catchlogr.Mobile.Core.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;

namespace Catchlogr.Mobile;

/// <summary>
/// Configures and creates the Catchlogr mobile application.
/// </summary>
public static class MauiProgram
{
	/// <summary>Creates the configured MAUI application.</summary>
	public static MauiApp CreateMauiApp()
	{
		SecureStorageInitializer.Initialize();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG || LOCAL
		builder.Logging.AddDebug();
#endif

		// Load and register app settings
		var appSettings = AppSettings.Load(typeof(MauiProgram).Assembly,
#if LOCAL
            BackendEnvironment.Local
#elif DEBUG
            BackendEnvironment.Development
#else
            BackendEnvironment.Production
#endif
        );
		var apiBaseUrl = PlatformApiUrl.Resolve(
			appSettings.Api.BaseUrl,
			appSettings.BackendEnvironment,
			DeviceInfo.Current.Platform == DevicePlatform.Android,
			DeviceInfo.Current.DeviceType == DeviceType.Virtual,
            DeviceInfo.Current.Platform == DevicePlatform.WinUI);
		var apiBaseUri = new Uri(apiBaseUrl.TrimEnd('/') + '/');
        appSettings.Database.RootDirectory ??= FileSystem.AppDataDirectory;
        builder.Services.AddMobileCore(appSettings, apiBaseUri);

        // --- Device capabilities ---
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        builder.Services.AddSingleton<IAppInteraction, MauiAppInteraction>();
        builder.Services.AddSingleton<IDeviceLocationService, DeviceLocationService>();
        builder.Services.AddSingleton<IPhotoPicker, MauiPhotoPicker>();

        // --- Authentication storage ---
        builder.Services.AddSingleton<ISecretStorage, MauiSecretStorage>();
        builder.Services.AddSingleton<ILogoutDialogService, ShellLogoutDialogService>();
        builder.Services.AddSingleton<IAppNavigator, ShellAppNavigator>();

        // --- Pages ---
        builder.Services.AddTransient<FishingTripsPage>();
        builder.Services.AddTransient<AddEditFishingTripPage>();
        builder.Services.AddTransient<FishingTripDetailsPage>();
        builder.Services.AddTransient<AddEditCatchPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<CheckEmailPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<ResetPasswordPage>();


        return builder.Build();
	}
}
