using FishingLog.Mobile.Configuration;
using FishingLog.Mobile.Data;
using FishingLog.Mobile.Data.Repositories;
using FishingLog.Mobile.Pages;
using FishingLog.Mobile.Services;
using FishingLog.Mobile.Services.Authentication;
using FishingLog.Mobile.Services.Photos;
using FishingLog.Mobile.ViewModels;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;

namespace FishingLog.Mobile;

/// <summary>
/// Configures and creates the FishingLog mobile application.
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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Load and register app settings
		var appSettings = AppSettings.Load();
		builder.Services.AddSingleton(appSettings);
		builder.Services.AddSingleton(appSettings.Api);
		builder.Services.AddSingleton(appSettings.Sync);
		builder.Services.AddSingleton(appSettings.Database);

        // --- Local database ---
        // Singleton: one connection shared for the entire app lifetime
        builder.Services.AddSingleton<ILocalDatabase, LocalDatabase>();

        // --- Local repositories ---
        // Transient: cheap to create, no state of their own
        builder.Services.AddTransient<IFishingTripLocalRepository, FishingTripLocalRepository>();
		builder.Services.AddTransient<ICatchLocalRepository, CatchLocalRepository>();
		builder.Services.AddTransient<ISyncMetadataRepository, SyncMetadataRepository>();

        // --- Device capabilities ---
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<IDeviceLocationService, DeviceLocationService>();
        builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);
        builder.Services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();

        // --- Authentication storage ---
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<AuthenticationMessageHandler>();

        builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/');
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        });

        // --- API client ---
        // Typed HttpClient: BaseAddress and Timeout come from appsettings
        builder.Services.AddHttpClient<IFishingTripApiClient, FishingTripApiClient>(client =>
		{
			var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
			client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
			client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
		})
        .AddHttpMessageHandler<AuthenticationMessageHandler>();
        builder.Services.AddHttpClient<ICatchApiClient, CatchApiClient>(client =>
        {
            var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        })
        .AddHttpMessageHandler<AuthenticationMessageHandler>();
        builder.Services.AddHttpClient<IPhotoApiClient, PhotoApiClient>(client =>
        {
            var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        })
        .AddHttpMessageHandler<AuthenticationMessageHandler>();
        builder.Services.AddHttpClient<IApiHealthClient, ApiHealthClient>(client =>
        {
            var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddHttpClient<ILocationSearchApiClient, LocationSearchApiClient>(client =>
        {
            var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
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

        // --- Pages ---
        builder.Services.AddTransient<FishingTripsPage>();
        builder.Services.AddTransient<AddEditFishingTripPage>();
        builder.Services.AddTransient<FishingTripDetailsPage>();
        builder.Services.AddTransient<AddEditCatchPage>();


        return builder.Build();
	}
}
