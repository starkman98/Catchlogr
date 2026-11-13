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
    private const string ResendHttpClientName = "IdentityEmailResend";

    /// <summary>Adds the validated email transport and Identity email sender.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environmentName">Host environment; only E2E permits Mailpit capture.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddIdentityEmail(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddIdentityEmail(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName = "Production")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(EmailOptions.SectionName);
        var captureEmail = UseCaptureProvider(section, environmentName);
        AddValidatedOptions(services, section, captureEmail);
        services.AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSender>();
        if (captureEmail)
        {
            services.AddHttpClient<IEmailTransport, MailpitEmailTransport>()
                .RemoveAllLoggers();
            return services;
        }
        return AddResendTransport(services, section);
    }

    private static bool UseCaptureProvider(IConfiguration section, string environmentName)
    {
        var provider = section[nameof(EmailOptions.Provider)] ?? "Resend";
        var captureEmail = provider == "Mailpit";
        if (provider != "Resend" && !captureEmail)
            throw new InvalidOperationException("Unsupported email provider.");
        if (captureEmail && environmentName != "E2E")
            throw new InvalidOperationException("Mailpit is allowed only in the E2E environment.");
        return captureEmail;
    }

    private static void AddValidatedOptions(
        IServiceCollection services, IConfiguration section, bool captureEmail)
    {
        services.AddOptions<EmailOptions>()
            .Bind(section)
            .Validate(
                options => captureEmail || !string.IsNullOrWhiteSpace(options.ApiKey),
                "Email API key is required.")
            .Validate(
                options => MailAddress.TryCreate(options.FromAddress, out _),
                "Email sender address must be a valid email address.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromName),
                "Email sender name is required.")
            .Validate(
                options => options.PublicWebBaseUrl is
                    { IsAbsoluteUri: true } &&
                    options.PublicWebBaseUrl.Scheme == Uri.UriSchemeHttps,
                "Email public web base URL must be an absolute HTTPS URL.")
            .Validate(
                options => !captureEmail || IsLocalCaptureUrl(options.MailpitBaseUrl),
                "Mailpit must use an HTTP loopback origin or the mailpit container.")
            .ValidateOnStart();
    }

    private static IServiceCollection AddResendTransport(IServiceCollection services, IConfiguration section)
    {
        services.AddTransient<IEmailTransport, ResendEmailTransport>();

        var resendOptions = new ResendClientOptions
        {
            ApiToken = section[nameof(EmailOptions.ApiKey)] ?? string.Empty,
            ThrowExceptions = true
        };
        services.AddHttpClient(ResendHttpClientName);
        services.AddSingleton<IResend>(serviceProvider =>
            ResendClient.Create(
                resendOptions,
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(ResendHttpClientName)));

        return services;
    }

    private static bool IsLocalCaptureUrl(Uri? uri)
        => uri is { IsAbsoluteUri: true } &&
            uri.Scheme == Uri.UriSchemeHttp &&
            (uri.IsLoopback || uri.Host == "mailpit") &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            uri.AbsolutePath == "/" &&
            string.IsNullOrEmpty(uri.Query) &&
            string.IsNullOrEmpty(uri.Fragment);
}
