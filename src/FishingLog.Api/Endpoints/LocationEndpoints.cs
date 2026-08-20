using FishingLog.Application.Interfaces;
using FishingLog.Contracts.LocationDTOs;
using Microsoft.AspNetCore.Mvc;

namespace FishingLog.Api.Endpoints;

/// <summary>
/// Registers endpoints for privacy-friendly named-location searches.
/// </summary>
public static class LocationEndpoints
{
    /// <summary>Maps location-search routes under /api/locations.</summary>
    public static void MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/locations")
            .WithTags("Locations");

        group.MapGet("/search", SearchLocations)
            .Produces<IReadOnlyList<LocationSearchResult>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Search for a named location")
            .WithDescription(
                "Returns coordinates for a user-entered location name without accessing device location.");
    }

    private static async Task<IResult> SearchLocations(
        string? query,
        ILocationSearchService service,
        CancellationToken ct)
    {
        var trimmedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery) ||
            trimmedQuery.Length is < 2 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["query"] =
                ["Location query must contain between 2 and 100 characters."]
            });
        }

        var results = await service.SearchAsync(trimmedQuery, ct);
        return Results.Ok(results);
    }
}
