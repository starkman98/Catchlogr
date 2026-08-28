using Catchlogr.Application.Exceptions;
using Catchlogr.Application.Interfaces;
using Catchlogr.Application.Photos;
using Catchlogr.Domain.Entities;
using Catchlogr.Domain.Interfaces;

namespace Catchlogr.Application.Services;

/// <summary>
/// Manages private catch photos and enforces the current user ownership.
/// </summary>
public sealed class PhotoService : IPhotoService
{
    /// <summary>Maximum accepted photo size in bytes (10 MiB).</summary>
    public const long MaxPhotoSizeBytes = 10 * 1024 * 1024;

    private readonly ICatchRepository _catchRepository;
    private readonly ICatchPhotoRepository _photoRepository;
    private readonly IPhotoObjectStorage _storage;
    private readonly ICurrentUserContext _currentUser;

    /// <summary>Initializes the private photo service.</summary>
    public PhotoService(
        ICatchRepository catchRepository,
        ICatchPhotoRepository photoRepository,
        IPhotoObjectStorage storage,
        ICurrentUserContext currentUser)
    {
        _catchRepository = catchRepository;
        _photoRepository = photoRepository;
        _storage = storage;
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public async Task<Guid> UploadAsync(
        Guid catchId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        var ownedCatch = await _catchRepository.GetByIdAsync(catchId, _currentUser.UserId, ct)
            ?? throw new NotFoundException($"Catch {catchId} not found.");
        var existing = await _photoRepository.GetByCatchIdAsync(catchId, _currentUser.UserId, ct);
        var photo = new CatchPhoto
        {
            CatchId = catchId,
            StorageKey = Guid.NewGuid().ToString("N"),
            ContentType = contentType,
            SizeBytes = contentLength,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _storage.SaveAsync(photo.StorageKey, content, contentType, contentLength, ct);
        try
        {
            ownedCatch.PhotoUrl = $"/api/photos/{photo.Id:D}";
            ownedCatch.LastModified = DateTime.UtcNow;
            await _photoRepository.ReplaceAsync(
                existing,
                photo,
                ownedCatch,
                ct);
        }
        catch
        {
            await _storage.DeleteAsync(photo.StorageKey, ct);
            throw;
        }

        if (existing is not null)
            await _storage.DeleteAsync(existing.StorageKey, ct);
        return photo.Id;
    }

    /// <inheritdoc/>
    public async Task<PhotoContent> OpenReadAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await _photoRepository.GetByIdAsync(photoId, _currentUser.UserId, ct)
            ?? throw new NotFoundException($"Photo {photoId} not found.");
        var stream = await _storage.OpenReadAsync(photo.StorageKey, ct)
            ?? throw new NotFoundException($"Photo {photoId} not found.");
        return new PhotoContent(stream, photo.ContentType);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await _photoRepository.GetByIdAsync(photoId, _currentUser.UserId, ct)
            ?? throw new NotFoundException($"Photo {photoId} not found.");
        await DeleteOwnedPhotoAsync(photo, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteForCatchAsync(Guid catchId, CancellationToken ct = default)
    {
        var ownedCatch = await _catchRepository.GetByIdAsync(catchId, _currentUser.UserId, ct)
            ?? throw new NotFoundException($"Catch {catchId} not found.");
        var photo = await _photoRepository.GetByCatchIdAsync(catchId, _currentUser.UserId, ct);
        if (photo is null)
            return;

        ownedCatch.PhotoUrl = null;
        ownedCatch.LastModified = DateTime.UtcNow;
        await _photoRepository.DeleteAsync(photo, ownedCatch, ct);
        await _storage.DeleteAsync(photo.StorageKey, ct);
    }

    private async Task DeleteOwnedPhotoAsync(CatchPhoto photo, CancellationToken ct)
    {
        photo.Catch.PhotoUrl = null;
        photo.Catch.LastModified = DateTime.UtcNow;
        await _photoRepository.DeleteAsync(photo, photo.Catch, ct);
        await _storage.DeleteAsync(photo.StorageKey, ct);
    }
}
