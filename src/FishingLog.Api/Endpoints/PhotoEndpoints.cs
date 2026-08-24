using FishingLog.Application.Interfaces;
using FishingLog.Contracts.PhotoDTOs;
using FishingLog.Infrastructure.Photos;
using Microsoft.AspNetCore.Mvc;

namespace FishingLog.Api.Endpoints;

/// <summary>
/// Registers catch-photo upload and deletion endpoints.
/// </summary>
public static class PhotoEndpoints
{
    /// <summary>Maps photo routes under <c>/api/photos</c>.</summary>
    public static void MapPhotoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/photos")
            .WithTags("Photos")
            .RequireAuthorization();

        group.MapPost("/", UploadAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<PhotoUploadResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .DisableAntiforgery()
            .WithSummary("Upload a catch photo");

        group.MapDelete("/{fileName}", DeleteAsync)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete a catch photo");
    }

    private static async Task<IResult> UploadAsync(
        IFormFile file,
        IPhotoStorage storage,
        HttpRequest request,
        CancellationToken ct)
    {
        if (file.Length <= 0)
            return Results.BadRequest(new ProblemDetails { Detail = "The photo is empty." });

        if (file.Length > LocalPhotoStorage.MaxPhotoSizeBytes)
            return Results.BadRequest(new ProblemDetails { Detail = "The photo must be 10 MiB or smaller." });

        try
        {
            await using var stream = file.OpenReadStream();
            var fileName = await storage.SaveAsync(stream, file.ContentType, file.Length, ct);
            var photoUrl = $"{request.Scheme}://{request.Host}/uploads/{Uri.EscapeDataString(fileName)}";

            return Results.Created(photoUrl, new PhotoUploadResponse(photoUrl));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAsync(
        string fileName,
        IPhotoStorage storage,
        CancellationToken ct)
    {
        await storage.DeleteAsync(fileName, ct);
        return Results.NoContent();
    }
}
