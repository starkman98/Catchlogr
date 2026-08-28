using Catchlogr.Application.Interfaces;
using System.Security.Claims;

namespace Catchlogr.Api.Authentication;

/// <summary>
/// Reads the authenticated account from the current HTTP principal.
/// </summary>
public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes the current-user context.
    /// </summary>
    public HttpCurrentUserContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public Guid UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out var userId))
            {
                throw new InvalidOperationException(
                    "An authenticated user identifier is required.");
            }

            return userId;
        }
    }
}