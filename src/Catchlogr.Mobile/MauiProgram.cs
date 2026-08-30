using Catchlogr.Mobile.Configuration;
using Catchlogr.Mobile.Data;
using Catchlogr.Mobile.Data.Repositories;
using Catchlogr.Mobile.Pages;
using Catchlogr.Mobile.Services;
using Catchlogr.Mobile.Services.Authentication;
using Catchlogr.Mobile.Services.Navigation;
using Catchlogr.Mobile.Services.Photos;
using Catchlogr.Mobile.ViewModels;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;

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
		var appSettings = AppSettings.Load();
		var apiBaseUrl = PlatformApiUrl.Resolve(
			appSettings.Api.BaseUrl,
			appSettings.BackendEnvironment,
			DeviceInfo.Current.Platform,
			DeviceInfo.Current.DeviceType);
		var apiBaseUri = new Uri(apiBaseUrl.TrimEnd('/') + '/');
		builder.Services.AddSingleton(appSettings);
		builder.Services.AddSingleton(appSettings.Api);
		builder.Services.AddSingleton(appSettings.Sync);
		builder.Services.AddSingleton(appSettings.Database);

        // --- Local database ---
        // Singleton: one manager switches the active account connection safely
        builder.Services.AddSingleton<LocalDatabase>();
        builder.Services.AddSingleton<ILocalDatabase>(services =>
            services.GetRequiredService<LocalDatabase>());
        builder.Services.AddSingleton<IAccountStorageContext>(services =>
            services.GetRequiredService<LocalDatabase>());

        // --- Local repositories ---
        // Transient: cheap to create, no state of their own
        builder.Services.AddTransient<IFishingTripLocalRepository, FishingTripLocalRepository>();
		builder.Services.AddTransient<ICatchLocalRepository, CatchLocalRepository>();
		builder.Services.AddTransient<ISyncMetadataRepository, SyncMetadataRepository>();

        // --- Device capabilities ---
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<IDeviceLocationService, DeviceLocationService>();
        builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);
        builder.Services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();

        // --- Authentication storage ---
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddTransient<ILogoutService, LogoutService>();
        builder.Services.AddSingleton<ILogoutDialogService, ShellLogoutDialogService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<AuthenticationMessageHandler>();
        builder.Services.AddSingleton<IAppNavigator, ShellAppNavigator>();

        builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        });

        // --- API client ---
        // Typed HttpClient: BaseAddress and Timeout come from appsettings
		builder.Services.AddHttpClient<IFishingTripApiClient, FishingTripApiClient>(client =>
		{
			client.BaseAddress = apiBaseUri;
			client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
		})
        .AddHttpMessageHandler<AuthenticationMessageHandler>();
        builder.Services.AddHttpClient<ICatchApiClient, CatchApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        })
        .AddHttpMessageHandler<AuthenticationMessageHandler>();
        builder.Services.AddHttpClient<IPhotoApiClient, PhotoApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        })
        .AddHttpMessageHandler<AuthenticationMessageHandler>();
        builder.Services.AddHttpClient<IApiHealthClient, ApiHealthClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddHttpClient<ILocationSearchApiClient, LocationSearchApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        })
        .AddHttpMessageHandler<AuthenticationMessageHandler>();

        // --- Sync service ---
        builder.Services.AddTransient<IFishingTripSyncService, FishingTripSyncService>();
		builder.Services.AddTransient<ICatchSyncService, CatchSyncService>();
        builder.Services.AddTransient<ISyncOrchestrator, SyncOrchestrator>();

        // --- ViewModels ---
        builder.Services.AddTransient<FishingTripsViewModel>();
        builder.Services.AddTransient<AddEditFishingTripViewModel>();
        builder.Services.AddTransient<FishingTripDetailsViewModel>();
        builder.Services.AddTransient<AddEditCatchViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();

        // --- Pages ---
        builder.Services.AddTransient<FishingTripsPage>();
        builder.Services.AddTransient<AddEditFishingTripPage>();
        builder.Services.AddTransient<FishingTripDetailsPage>();
        builder.Services.AddTransient<AddEditCatchPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();


        return builder.Build();
	}
}
