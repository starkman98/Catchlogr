using Microsoft.AspNetCore.Identity;

namespace FishingLog.Infrastructure.Identity;

/// <summary>
/// Represents an authenticated FishingLog account.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets when the account was created, in UTC.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name displayed in the application.
    /// </summary>
    public string? DisplayName { get; set; }
}
