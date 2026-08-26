using FishingLog.Application.Interfaces;
using FishingLog.Infrastructure.Persistence;
using FishingLog.Infrastructure.Photos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FishingLog.Api.IntegrationTests;

/// <summary>
/// Hosts the complete API with isolated persistence and private photo storage.
/// </summary>
public sealed class FishingLogApiFactory : WebApplicationFactory<Program>
{
    private readonly TimeSpan? _bearerTokenLifetime;
    private readonly string _rootDirectory = Path.Combine(
        Path.GetTempPath(),
        "FishingLog.Api.IntegrationTests",
        Guid.NewGuid().ToString("N"));
    private readonly string _databaseName =
        $"FishingLog-{Guid.NewGuid():N}";

    /// <summary>Initializes a test host with an optional access-token lifetime.</summary>
    public FishingLogApiFactory(TimeSpan? bearerTokenLifetime = null)
    {
        _bearerTokenLifetime = bearerTokenLifetime;
    }

    /// <summary>Initializes the isolated integration-test database.</summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootDirectory);
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FishingLogDbContext>();
        await database.Database.EnsureCreatedAsync(ct);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocationSearch:LocationIQ:ApiKey"] = "integration-test-key",
                ["LocationSearch:LocationIQ:BaseUri"] = "https://location.test/",
                ["Cors:AllowedOrigins:0"] = "https://mobile.test"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FishingLogDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<FishingLogDbContext>>();
            services.AddDbContext<FishingLogDbContext>(options =>
                options.UseInMemoryDatabase(
                    _databaseName));
            if (_bearerTokenLifetime.HasValue)
            {
                services.PostConfigure<BearerTokenOptions>(
                    IdentityConstants.BearerScheme,
                    options => options.BearerTokenExpiration =
                        _bearerTokenLifetime.Value);
            }

            services.RemoveAll<IPhotoObjectStorage>();
            services.AddSingleton<IPhotoObjectStorage>(
                new LocalPhotoStorage(
                    Path.Combine(_rootDirectory, "private-photos")));
        });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }
}

