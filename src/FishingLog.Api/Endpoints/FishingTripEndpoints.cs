using FishingLog.Application.Interfaces;
using FishingLog.Contracts.FishingTripDTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FishingLog.Api.Endpoints;

/// <summary>
/// Registers all fishing trip Minimal API endpoints.
/// Call <see cref="MapFishingTripEndpoints"/> from Program.cs.
/// </summary>
public static class FishingTripEndpoints
{
    /// <summary>Maps all fishing trip routes under /api/fishing-trips.</summary>
    public static void MapFishingTripEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fishing-trips")
            .WithTags("FishingTrips");

        group.MapGet("/", GetAllTrips)
            .Produces<List<FishingTripResponse>>(StatusCodes.Status200OK)
            .WithSummary("Get all fishing trips")
            .WithDescription("Returns all trips. Pass ?modifiedSince=UTC timestamp to get only incremental changes (used by mobile sync).");

        group.MapGet("/{id:guid}", GetTripById)
            .Produces<FishingTripResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get a fishing trip by ID");

        group.MapPost("/", CreateTrip)
            .Produces<FishingTripResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Create a new fishing trip");

        group.MapPut("/{id:guid}", UpdateTrip)
            .Produces<FishingTripResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Update an existing fishing trip");

        group.MapDelete("/{id:guid}", DeleteTrip)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a fishing trip");
    }

    /// <summary>
    /// GET /api/fishing-trips
    /// Optional query parameter: ?modifiedSince=2024-01-01T00:00:00Z
    /// When modifiedSince is provided only trips changed after that timestamp are returned.
    /// This is the endpoint the mobile sync service calls on each sync.
    /// </summary>
    private static async Task<IResult> GetAllTrips(
        IFishingTripService service,
        DateTime? modifiedSince,
        CancellationToken ct)
    {
        var trips = modifiedSince.HasValue
            ? await service.GetModifiedSinceAsync(modifiedSince.Value, ct)
            : await service.GetAllAsync(ct);

        return Results.Ok(trips);
    }

    /// <summary>GET /api/fishing-trips/{id}</summary>
    private static async Task<IResult> GetTripById(
        Guid id,
        IFishingTripService service,
        CancellationToken ct)
    {
        var trip = await service.GetByIdAsync(id, ct);
        return Results.Ok(trip);
    }

    /// <summary>POST /api/fishing-trips → 201 Created</summary>
    private static async Task<IResult> CreateTrip(
        CreateFishingTripRequest request,
        IValidator<CreateFishingTripRequest> validator,
        IFishingTripService service,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var created = await service.CreateAsync(request, ct);
        return Results.Created($"/api/fishing-trips/{created.Id}", created);
    }

    /// <summary>PUT /api/fishing-trips/{id} → 200 OK or 404</summary>
    private static async Task<IResult> UpdateTrip(
        Guid id,
        UpdateFishingTripRequest request,
        IValidator<UpdateFishingTripRequest> validator,
        IFishingTripService service,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var updated = await service.UpdateAsync(id, request, ct);
        return Results.Ok(updated);
    }

    /// <summary>DELETE /api/fishing-trips/{id} → 204 No Content or 404</summary>
    private static async Task<IResult> DeleteTrip(
        Guid id,
        IFishingTripService service,
        CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Results.NoContent();
    }
}
