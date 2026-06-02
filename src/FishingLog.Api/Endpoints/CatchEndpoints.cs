using FishingLog.Application.Interfaces;
using FishingLog.Contracts.CatchDTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FishingLog.Api.Endpoints;

/// <summary>
/// Registers all catches Minimal API endpoints.
/// Call <see cref="MapCatchEndpoints"/> from Program.cs.
/// </summary>
public static class CatchEndpoints
{
    /// <summary>Maps all catch routes under /api/fishing-trips/{tripId}/catches and /api/catches.</summary>
    public static void MapCatchEndpoints(this IEndpointRouteBuilder app)
    {
        // Nested routes — require tripId context
        var tripsGroup = app.MapGroup("/api/fishing-trips/{tripId:guid}/catches")
            .WithTags("Catches");

        tripsGroup.MapGet("/", GetByTripId)
            .Produces<List<CatchResponse>>(StatusCodes.Status200OK)
            .WithSummary("Get all catches for a trip");

        tripsGroup.MapPost("/", CreateCatch)
            .Produces<CatchResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Create a catch on a trip");

        // Flat routes — act on a single known catch or sync
        var catchesGroup = app.MapGroup("/api/catches")
            .WithTags("Catches");

        catchesGroup.MapGet("/", GetAllCatches)
            .Produces<List<CatchResponse>>(StatusCodes.Status200OK)
            .WithSummary("Get all catches, optionally filtered by ?modifiedSince= for sync");

        catchesGroup.MapGet("/{id:guid}", GetCatchById)
            .Produces<CatchResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get a catch by ID");

        catchesGroup.MapPut("/{id:guid}", UpdateCatch)
            .Produces<CatchResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Update an existing catch");

        catchesGroup.MapDelete("/{id:guid}", DeleteAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a catch");
    }

    private static async Task<IResult> GetAllCatches(
        ICatchService service,
        DateTime? modifiedSince,
        CancellationToken ct = default
        )
    {
        var catches = modifiedSince.HasValue
            ? await service.GetModifiedSinceAsync(modifiedSince.Value, ct)
            : await service.GetAllAsync(ct);

        return Results.Ok(catches);
    }

    /// <summary>GET /api/catches/{id}</summary>
    private static async Task<IResult> GetCatchById(
        Guid id,
        ICatchService service,
        CancellationToken ct
        )
    {
        var currentCatch = await service.GetByIdAsync(id, ct);

        return Results.Ok(currentCatch);
    }

    private static async Task<IResult> GetByTripId(
        Guid tripId,
        ICatchService service,
        CancellationToken ct
        )
    {
        var trips = await service.GetByTripIdAsync(tripId, ct);

        return Results.Ok(trips);
    }

    /// <summary>POST /api/fishing-trips/{tripId}/catches → 201 Created</summary>
    private static async Task<IResult> CreateCatch(
        Guid tripId,
        CreateCatchRequest request,
        IValidator<CreateCatchRequest> validator,
        ICatchService service,
        CancellationToken ct
        )
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var created = await service.CreateAsync(tripId, request, ct);

        return Results.Created($"/api/catches/{created.Id}", created);
    }

    private static async Task<IResult> UpdateCatch(
        Guid id,
        UpdateCatchRequest request,
        IValidator<UpdateCatchRequest> validator,
        ICatchService service,
        CancellationToken ct
        )
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var updated = await service.UpdateAsync(id, request, ct);

        return Results.Ok(updated);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ICatchService service,
        CancellationToken ct
        )
    {
        await service.DeleteAsync(id, ct);

        return Results.NoContent();
    }
}
