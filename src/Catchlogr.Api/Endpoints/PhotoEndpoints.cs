using Catchlogr.Application.Interfaces;
using Catchlogr.Application.Services;
using Catchlogr.Contracts.PhotoDTOs;
using Microsoft.AspNetCore.Mvc;

namespace Catchlogr.Api.Endpoints;

/// <summary>
/// Registers private catch-photo upload, download and deletion endpoints.
/// </summary>
public static class PhotoEndpoints
{
    /// <summary>Maps photo routes under <c>/api/photos</c>.</summary>
    public static void MapPhotoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/photos")
            .WithTags("Photos")
            .RequireAuthorization();

        group.MapGet("/{photoId:guid}", DownloadAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Download a private catch photo");

        group.MapDelete("/{photoId:guid}", DeleteAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a private catch photo");

        app.MapPost("/api/catches/{catchId:guid}/photos", UploadAsync)
            .WithTags("Photos")
            .RequireAuthorization()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<PhotoUploadResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .DisableAntiforgery()
            .WithSummary("Upload a catch photo");

    }

    private static async Task<IResult> UploadAsync(
        Guid catchId,
        IFormFile file,
        IPhotoService service,
        HttpRequest request,
        CancellationToken ct)
    {
        if (file.Length <= 0)
            return Results.BadRequest(new ProblemDetails { Detail = "The photo is empty." });

        if (file.Length > PhotoService.MaxPhotoSizeBytes)
            return Results.BadRequest(new ProblemDetails { Detail = "The photo must be 10 MiB or smaller." });

        try
        {
            await using var stream = file.OpenReadStream();
            var photoId = await service.UploadAsync(
                catchId,
                stream,
                file.ContentType,
                file.Length,
                ct);
            var photoUrl = $"{request.Scheme}://{request.Host}/api/photos/{photoId:D}";

            return Results.Created(
                photoUrl,
                new PhotoUploadResponse(photoId, photoUrl));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<IResult> DownloadAsync(
        Guid photoId,
        IPhotoService service,
        CancellationToken ct)
    {
        var photo = await service.OpenReadAsync(photoId, ct);
        return Results.File(
            photo.Stream,
            photo.ContentType,
            enableRangeProcessing: true);
    }

    private static async Task<IResult> DeleteAsync(
        Guid photoId,
        IPhotoService service,
        CancellationToken ct)
    {
        await service.DeleteAsync(photoId, ct);
        return Results.NoContent();
    }
}
