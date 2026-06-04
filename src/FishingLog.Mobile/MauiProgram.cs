using FishingLog.Mobile.Configuration;
using FishingLog.Mobile.Data;
using FishingLog.Mobile.Data.Repositories;
using FishingLog.Mobile.Pages;
using FishingLog.Mobile.Services;
using FishingLog.Mobile.ViewModels;
using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Services;
using Microsoft.Extensions.Logging;

namespace FishingLog.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
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

        // --- API client ---
        // Typed HttpClient: BaseAddress and Timeout come from appsettings
		builder.Services.AddHttpClient<IFishingTripApiClient, FishingTripApiClient>(client =>
		{
			var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
			client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
			client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
		});
        builder.Services.AddHttpClient<ICatchApiClient, CatchApiClient>(client =>
        {
            var baseUrl = PlatformApiUrl.Resolve(appSettings.Api.BaseUrl);
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(appSettings.Api.Timeout);
        });

        // --- Sync service ---
        builder.Services.AddTransient<IFishingTripSyncService, FishingTripSyncService>();
		builder.Services.AddTransient<ICatchSyncService, CatchSyncService>();
        builder.Services.AddTransient<ISyncOrchestrator, SyncOrchestrator>();

        // --- ViewModels ---
        builder.Services.AddTransient<FishingTripsViewModel>();
        builder.Services.AddTransient<AddEditFishingTripViewModel>();

        // --- Pages ---
        builder.Services.AddTransient<FishingTripsPage>();
        builder.Services.AddTransient<AddEditFishingTripPage>();


        return builder.Build();
	}
}
