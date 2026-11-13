using System.Net;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Contains the content and action link for one Identity email.</summary>
internal sealed record AccountActionEmail
{
    /// <summary>Gets the message subject.</summary>
    public required string Subject { get; init; }
    /// <summary>Gets the email heading.</summary>
    public required string Heading { get; init; }
    /// <summary>Gets the explanation shown before the action.</summary>
    public required string Introduction { get; init; }
    /// <summary>Gets the button text.</summary>
    public required string ActionText { get; init; }
    /// <summary>Gets the public Web action link.</summary>
    public required string Link { get; init; }
    /// <summary>Gets the non-sensitive message category used for logging.</summary>
    public required string MessageType { get; init; }
    /// <summary>Gets an optional code for entry into the Mobile reset form.</summary>
    public string? ResetCode { get; init; }

    /// <summary>Creates the plain-text alternative.</summary>
    public string TextBody => $"{Introduction}\n\n{Link}" +
        (ResetCode is null ? string.Empty : $"\n\nOr enter this reset code in the Catchlogr app:\n\n{ResetCode}");

    /// <summary>Creates HTML with all externally supplied content encoded.</summary>
    public string BuildHtml()
    {
        var encodedHeading = WebUtility.HtmlEncode(Heading);
        var encodedIntroduction = WebUtility.HtmlEncode(Introduction);
        var encodedActionText = WebUtility.HtmlEncode(ActionText);
        var encodedActionLink = WebUtility.HtmlEncode(Link);
        var fallbackHtml = string.IsNullOrWhiteSpace(ResetCode)
            ? string.Empty
            : $"<p>Or enter this reset code in the Catchlogr app: " +
                $"<strong>{WebUtility.HtmlEncode(ResetCode)}</strong></p>";

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
                {{fallbackHtml}}
                <p style="color:#5c6f62">If you did not request this, you can ignore this email.</p>
              </div>
            </body>
            </html>
            """;
    }
}
