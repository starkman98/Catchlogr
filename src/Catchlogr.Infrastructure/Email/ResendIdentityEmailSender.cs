using System.Net;
using System.Net.Mail;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Catchlogr.Infrastructure.Email;

/// <summary>
/// Sends ASP.NET Core Identity account-action messages through Resend.
/// </summary>
/// <example>
/// Register this sender with <c>AddIdentityEmail</c>; Identity invokes it when
/// registration, confirmation resend, or password recovery endpoints run.
/// </example>
public sealed class ResendIdentityEmailSender :
    IEmailSender<ApplicationUser>
{
    private readonly IResend _resend;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendIdentityEmailSender> _logger;

    /// <summary>Initializes a new Resend-backed Identity email sender.</summary>
    /// <param name="resend">The Resend API client.</param>
    /// <param name="options">Validated email delivery settings.</param>
    /// <param name="logger">The structured logger.</param>
    public ResendIdentityEmailSender(
        IResend resend,
        IOptions<EmailOptions> options,
        ILogger<ResendIdentityEmailSender> logger)
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink)
    {
        var publicLink = BuildPublicLink(confirmationLink);
        return SendAsync(
            email,
            "Confirm your Catchlogr account",
            BuildActionHtml(
                "Confirm your email",
                "Confirm your email address to finish creating your Catchlogr account.",
                "Confirm email",
                publicLink),
            $"Confirm your email address to finish creating your Catchlogr account.\n\n{publicLink}",
            "confirmation");
    }

    /// <inheritdoc/>
    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink)
    {
        var publicLink = BuildPublicLink(resetLink);
        return SendAsync(
            email,
            "Reset your Catchlogr password",
            BuildActionHtml(
                "Reset your password",
                "Use the link below to choose a new Catchlogr password.",
                "Reset password",
                publicLink),
            $"Use this link to choose a new Catchlogr password.\n\n{publicLink}",
            "password-reset-link");
    }

    /// <inheritdoc/>
    public Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        var decodedCode = WebUtility.HtmlDecode(resetCode);
        var encodedCode = WebUtility.HtmlEncode(decodedCode);
        var html = $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;background:#f3f6f4;font-family:Arial,sans-serif;color:#17351f">
              <div style="max-width:560px;margin:32px auto;background:#ffffff;border-radius:12px;padding:32px">
                <h1 style="margin-top:0;color:#175c32">Reset your password</h1>
                <p>Enter this code in Catchlogr to choose a new password:</p>
                <p style="font-size:20px;font-weight:700;word-break:break-all;background:#eef7f0;padding:16px;border-radius:8px">{{encodedCode}}</p>
                <p style="color:#5c6f62">If you did not request this, you can ignore this email.</p>
              </div>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            "Reset your Catchlogr password",
            html,
            $"Enter this code in Catchlogr to reset your password:\n\n{decodedCode}\n\nIf you did not request this, you can ignore this email.",
            "password-reset-code");
    }

    private async Task SendAsync(
        string email,
        string subject,
        string htmlBody,
        string textBody,
        string messageType)
    {
        var message = new EmailMessage
        {
            From = new MailAddress(
                _options.FromAddress,
                _options.FromName).ToString(),
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
        message.To.Add(email);

        try
        {
            await _resend.EmailSendAsync(message);
            _logger.LogInformation(
                "Resend accepted an Identity email of type {MessageType}.",
                messageType);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Resend rejected an Identity email of type {MessageType}.",
                messageType);
            throw;
        }
    }

    private string BuildPublicLink(string encodedLink)
    {
        var decodedLink = WebUtility.HtmlDecode(encodedLink);
        if (!Uri.TryCreate(decodedLink, UriKind.Absolute, out var generatedUri))
        {
            throw new InvalidOperationException(
                "Identity generated an invalid account-action URL.");
        }

        var publicPath = generatedUri.AbsolutePath switch
        {
            "/api/auth/confirmEmail" => "/confirm-email",
            "/api/auth/resetPassword" => "/reset-password",
            _ => throw new InvalidOperationException(
                $"Unsupported Identity account-action URL: {generatedUri.AbsolutePath}")
        };

        var publicBase = new Uri(
            _options.PublicWebBaseUrl.AbsoluteUri.TrimEnd('/') + "/");

        var publicUri = new Uri(
            publicBase,
            publicPath.TrimStart('/'));

        var builder = new UriBuilder(publicUri)
        {
            Query = generatedUri.Query.TrimStart('?')
        };

        return builder.Uri.AbsoluteUri;
    }

    private static string BuildActionHtml(
        string heading,
        string introduction,
        string actionText,
        string actionLink)
    {
        var encodedHeading = WebUtility.HtmlEncode(heading);
        var encodedIntroduction = WebUtility.HtmlEncode(introduction);
        var encodedActionText = WebUtility.HtmlEncode(actionText);
        var encodedActionLink = WebUtility.HtmlEncode(actionLink);

        return $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;background:#f3f6f4;font-family:Arial,sans-serif;color:#17351f">
              <div style="max-width:560px;margin:32px auto;background:#ffffff;border-radius:12px;padding:32px">
                <h1 style="margin-top:0;color:#175c32">{{encodedHeading}}</h1>
                <p>{{encodedIntroduction}}</p>
                <p style="margin:28px 0">
                  <a href="{{encodedActionLink}}" style="display:inline-block;background:#175c32;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:700">{{encodedActionText}}</a>
                </p>
                <p style="color:#5c6f62">If you did not request this, you can ignore this email.</p>
              </div>
            </body>
            </html>
            """;
    }
}
