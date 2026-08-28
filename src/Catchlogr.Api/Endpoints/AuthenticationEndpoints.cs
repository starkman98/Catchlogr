using Catchlogr.Contracts.AuthenticationDTOs;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Catchlogr.Api.Endpoints;

/// <summary>
/// Registers authentication and account-management endpoints.
/// </summary>
public static class AuthenticationEndpoints
{
    /// <summary>
    /// Maps authentication routes under <c>/api/auth</c>.
    /// </summary>
    public static void MapAuthenticationEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapIdentityApi<ApplicationUser>();

        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new CurrentUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.CreatedUtc,
            user.DisplayName));
    }
}