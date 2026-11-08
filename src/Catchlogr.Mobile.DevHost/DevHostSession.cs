using Catchlogr.Mobile.Core;
using Catchlogr.Mobile.Core.Configuration;
using Catchlogr.Mobile.Core.Data;
using Catchlogr.Mobile.Core.Services;
using Catchlogr.Mobile.Core.Services.Authentication;
using Catchlogr.Mobile.Core.Services.Navigation;
using Catchlogr.Mobile.Core.Services.Photos;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Owns isolated temporary SQLite storage and the services for a single smoke run.</summary>
public sealed class DevHostSession : IAsyncDisposable
{
    private readonly string _directory;
    private bool _disposed;

    private DevHostSession(ServiceProvider services, string directory)
    {
        Services = services;
        _directory = directory;
    }

    /// <summary>Gets the composed Core services and development adapters.</summary>
    public ServiceProvider Services { get; }

    /// <summary>Creates a local-only session; optional service overrides support isolated HTTP integration tests.</summary>
    public static DevHostSession Create(DevHostOptions options, Action<IServiceCollection>? configure = null)
    {
        options.Validate();
        var directory = Path.Combine(Path.GetTempPath(), "Catchlogr.DevHost", Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddMobileCore(new AppSettings
        {
            BackendEnvironment = BackendEnvironment.Local,
            Database = new DatabaseSettings { RootDirectory = directory, FileName = "catchlogr_devhost.db3" }
        }, options.ApiBaseUri);
        services.AddSingleton<ISecretStorage, MemorySecretStorage>();
        services.AddSingleton<IConnectivityService, DevHostConnectivity>();
        services.AddSingleton<IDeviceLocationService, FixedDeviceLocationService>();
        services.AddSingleton<IPhotoPicker, UnavailablePhotoPicker>();
        services.AddSingleton<DevHostInteraction>();
        services.AddSingleton<IAppInteraction>(provider => provider.GetRequiredService<DevHostInteraction>());
        services.AddSingleton<IAppNavigator>(provider => provider.GetRequiredService<DevHostInteraction>());
        services.AddSingleton<ILogoutDialogService, DevHostLogoutDialog>();
        // Never follow redirects outside the validated local origin, including login POSTs.
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(
            () => new HttpClientHandler { AllowAutoRedirect = false }));
        configure?.Invoke(services);
        return new DevHostSession(services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }), directory);
    }

    /// <summary>Closes SQLite, clears tokens, disposes clients, and removes this run's local files.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            await Services.GetRequiredService<ILocalDatabase>().CloseAsync();
        }
        finally
        {
            Services.GetRequiredService<ITokenStore>().Clear();
            try { await Services.DisposeAsync(); }
            finally
            {
                if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
            }
        }
    }
}
