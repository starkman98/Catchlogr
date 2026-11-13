using System.Net;

namespace Catchlogr.Infrastructure.Email;

/// <summary>Builds account-action URLs without changing Identity token encoding.</summary>
internal sealed class IdentityEmailLinks(EmailOptions options)
{
    private readonly EmailOptions _options = options;

    /// <summary>Rewrites an Identity confirmation URL to the public Web page.</summary>
    public string Confirmation(string encodedLink)
        => RewriteIdentityLink(
            encodedLink,
            "/api/auth/confirmEmail",
            "confirm-email");

    /// <summary>Rewrites an Identity password reset URL to the public Web page.</summary>
    public string PasswordReset(string encodedLink)
        => RewriteIdentityLink(
            encodedLink,
            "/api/auth/resetPassword",
            "reset-password");

    private string RewriteIdentityLink(
        string encodedLink,
        string expectedIdentityPath,
        string publicPath)
    {
        var decodedLink = WebUtility.HtmlDecode(encodedLink);
        if (!Uri.TryCreate(decodedLink, UriKind.Absolute, out var generatedUri))
        {
            throw new InvalidOperationException(
                "Identity generated an invalid account-action URL.");
        }

        if (!string.Equals(
                generatedUri.AbsolutePath,
                expectedIdentityPath,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported Identity account-action URL: {generatedUri.AbsolutePath}");
        }

        var publicBase = new Uri(
            _options.PublicWebBaseUrl.AbsoluteUri.TrimEnd('/') + "/");

        var publicUri = new Uri(
            publicBase,
            publicPath);

        var builder = new UriBuilder(publicUri)
        {
            Query = generatedUri.Query.TrimStart('?')
        };

        return builder.Uri.AbsoluteUri;
    }

    /// <summary>Builds the public reset page URL with its encoded email and code.</summary>
    public string ResetCode(string email, string resetCode)
    {
        var publicBase = new Uri(
            _options.PublicWebBaseUrl.AbsoluteUri.TrimEnd('/') + "/");
        var builder = new UriBuilder(new Uri(publicBase, "reset-password"))
        {
            Query = $"email={Uri.EscapeDataString(email)}" +
                $"&code={Uri.EscapeDataString(resetCode)}"
        };

        return builder.Uri.AbsoluteUri;
    }

}
