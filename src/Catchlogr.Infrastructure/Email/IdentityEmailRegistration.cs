using System.Net.Mail;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Registers transactional email delivery for ASP.NET Core Identity.</summary>
public static class IdentityEmailRegistration
{
    /// <summary>Adds the validated Resend client and Identity email sender.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddIdentityEmail(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddIdentityEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(EmailOptions.SectionName);
        services.AddOptions<EmailOptions>()
            .Bind(section)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "Email API key is required.")
            .Validate(
                options => MailAddress.TryCreate(options.FromAddress, out _),
                "Email sender address must be a valid email address.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromName),
                "Email sender name is required.")
            .Validate(
                options => options.PublicApiBaseUrl is
                    { IsAbsoluteUri: true } &&
                    options.PublicApiBaseUrl.Scheme == Uri.UriSchemeHttps,
                "Email public API base URL must be an absolute HTTPS URL.")
            .ValidateOnStart();

        services.AddResend(options =>
        {
            options.ApiToken = section[nameof(EmailOptions.ApiKey)]
                ?? string.Empty;
            options.ThrowExceptions = true;
        });
        services.AddTransient<
            IEmailSender<ApplicationUser>,
            ResendIdentityEmailSender>();

        return services;
    }
}
