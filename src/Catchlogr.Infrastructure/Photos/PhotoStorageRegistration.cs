using Catchlogr.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Infrastructure.Photos;

/// <summary>
/// Registers the configured catch-photo object storage provider.
/// </summary>
public static class PhotoStorageRegistration
{
    /// <summary>
    /// Registers catch-photo storage using paths relative to the supplied content root.
    /// </summary>
    /// <param name="services">The service collection receiving the registration.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="contentRootPath">The absolute application content-root path.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddPhotoStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        services.AddSingleton<IPhotoObjectStorage>(_ =>
        {
            var provider = configuration["PhotoStorage:Provider"];

            if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
            {
                var configuredPath = configuration["PhotoStorage:Local:Path"];
                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    throw new InvalidOperationException(
                        "PhotoStorage:Local:Path is missing.");
                }

                var absolutePath = Path.IsPathFullyQualified(configuredPath)
                    ? configuredPath
                    : Path.Combine(
                        Path.GetFullPath(contentRootPath),
                        configuredPath);

                return new LocalPhotoStorage(absolutePath);
            }

            if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    "The S3 photo-storage provider is not implemented.");
            }

            throw new InvalidOperationException(
                $"Unknown photo storage provider '{provider}'.");
        });

        return services;
    }
}