using Catchlogr.Mobile.Core.Configuration;
using Catchlogr.Mobile.Core.Data;
using Catchlogr.Mobile.Core.Data.Repositories;
using Catchlogr.Mobile.Core.Services;
using Catchlogr.Mobile.Core.Services.Authentication;
using Catchlogr.Mobile.Core.Services.Photos;
using Catchlogr.Mobile.Core.ViewModels;
using Catchlogr.Sync.Abstractions;
using Catchlogr.Sync.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Mobile.Core;

/// <summary>Registers mobile logic shared by platform and development hosts.</summary>
public static class MobileCoreServiceCollectionExtensions
{
    /// <summary>Registers persistence, authentication, clients, synchronization, and ViewModels.</summary>
    /// <remarks>The host supplies device, navigation, dialog, interaction, and secret-storage adapters.</remarks>
    public static IServiceCollection AddMobileCore(this IServiceCollection services, AppSettings settings, Uri apiBaseUri)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(apiBaseUri);
        services.AddLogging();
        services.AddSingleton(settings);
        services.AddSingleton(settings.Api);
        services.AddSingleton(settings.Sync);
        services.AddSingleton(settings.Database);
        services.AddSingleton<LocalDatabase>();
        services.AddSingleton<ILocalDatabase>(provider => provider.GetRequiredService<LocalDatabase>());
        services.AddSingleton<IAccountStorageContext>(provider => provider.GetRequiredService<LocalDatabase>());
        services.AddTransient<IFishingTripLocalRepository, FishingTripLocalRepository>();
        services.AddTransient<ICatchLocalRepository, CatchLocalRepository>();
        services.AddTransient<ISyncMetadataRepository, SyncMetadataRepository>();
        services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();
        services.AddSingleton<ITokenStore, SecureTokenStore>();
        services.AddTransient<ILogoutService, LogoutService>();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<AuthenticationMessageHandler>();
        services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(settings.Api.Timeout);
        });
        services.AddHttpClient<IFishingTripApiClient, FishingTripApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(settings.Api.Timeout);
        }).AddHttpMessageHandler<AuthenticationMessageHandler>();
        services.AddHttpClient<ICatchApiClient, CatchApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(settings.Api.Timeout);
        }).AddHttpMessageHandler<AuthenticationMessageHandler>();
        services.AddHttpClient<IPhotoApiClient, PhotoApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(settings.Api.Timeout);
        }).AddHttpMessageHandler<AuthenticationMessageHandler>();
        services.AddHttpClient<ILocationSearchApiClient, LocationSearchApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(settings.Api.Timeout);
        }).AddHttpMessageHandler<AuthenticationMessageHandler>();
        services.AddHttpClient<IApiHealthClient, ApiHealthClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddTransient<IFishingTripSyncService, FishingTripSyncService>();
        services.AddTransient<ICatchSyncService, CatchSyncService>();
        services.AddTransient<ISyncOrchestrator, SyncOrchestrator>();
        services.AddTransient<FishingTripsViewModel>();
        services.AddTransient<AddEditFishingTripViewModel>();
        services.AddTransient<FishingTripDetailsViewModel>();
        services.AddTransient<AddEditCatchViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<CheckEmailViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();
        services.AddTransient<ResetPasswordViewModel>();
        return services;
    }
}
