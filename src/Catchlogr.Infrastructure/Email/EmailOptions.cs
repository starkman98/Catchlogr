namespace Catchlogr.Infrastructure.Email;

/// <summary>
/// Configures transactional email delivery and the public API address used in
/// account-action links.
/// </summary>
/// <example>
/// Bind this type from the <c>Email</c> configuration section.
/// </example>
public sealed class EmailOptions
{
    /// <summary>The configuration section containing email settings.</summary>
    public const string SectionName = "Email";

    /// <summary>Gets or sets the Resend API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the verified sender email address.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender display name.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the externally reachable API base URL used for Identity
    /// confirmation and password-reset links.
    /// </summary>
    public Uri PublicApiBaseUrl { get; set; } = null!;
}
