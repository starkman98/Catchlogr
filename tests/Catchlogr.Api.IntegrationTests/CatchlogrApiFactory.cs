using Catchlogr.Application.Interfaces;
using Catchlogr.Infrastructure.Persistence;
using Catchlogr.Infrastructure.Photos;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catchlogr.Api.IntegrationTests;

/// <summary>
/// Hosts the complete API with isolated persistence and private photo storage.
/// </summary>
public sealed class CatchlogrApiFactory : WebApplicationFactory<Program>
{
    private readonly TimeSpan? _bearerTokenLifetime;
    private readonly bool _requireConfirmedEmail;
    private readonly string _rootDirectory = Path.Combine(
        Path.GetTempPath(),
        "Catchlogr.Api.IntegrationTests",
        Guid.NewGuid().ToString("N"));
    private readonly string _databaseName =
        $"Catchlogr-{Guid.NewGuid():N}";

    /// <summary>
    /// Initializes a test host with optional token lifetime and confirmation policy.
    /// </summary>
    public CatchlogrApiFactory(
        TimeSpan? bearerTokenLifetime = null,
        bool requireConfirmedEmail = false)
    {
        _bearerTokenLifetime = bearerTokenLifetime;
        _requireConfirmedEmail = requireConfirmedEmail;
    }

    /// <summary>Gets the email sender capturing Identity messages.</summary>
    public TestIdentityEmailSender EmailSender
        => Services.GetRequiredService<TestIdentityEmailSender>();

    /// <summary>Initializes the isolated integration-test database.</summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootDirectory);
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<CatchlogrDbContext>();
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
                ["Cors:AllowedOrigins:0"] = "https://mobile.test",
                ["PhotoStorage:Provider"] = "Local",
                ["Email:ApiKey"] = "integration-test-key",
                ["Email:FromAddress"] = "account@mail.catchlogr.test",
                ["Email:FromName"] = "Catchlogr",
                ["Email:PublicApiBaseUrl"] = "https://api.catchlogr.test",
                ["PhotoStorage:Local:Path"] = Path.Combine(
                    _rootDirectory,
                    "private-photos")
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatchlogrDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<CatchlogrDbContext>>();
            services.AddDbContext<CatchlogrDbContext>(options =>
                options.UseInMemoryDatabase(
                    _databaseName));
            if (_bearerTokenLifetime.HasValue)
            {
                services.PostConfigure<BearerTokenOptions>(
                    IdentityConstants.BearerScheme,
                    options => options.BearerTokenExpiration =
                        _bearerTokenLifetime.Value);
            }

            services.PostConfigure<IdentityOptions>(
                options => options.SignIn.RequireConfirmedEmail =
                    _requireConfirmedEmail);
            services.RemoveAll<IEmailSender<ApplicationUser>>();
            services.AddSingleton<TestIdentityEmailSender>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(
                serviceProvider => serviceProvider
                    .GetRequiredService<TestIdentityEmailSender>());

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
