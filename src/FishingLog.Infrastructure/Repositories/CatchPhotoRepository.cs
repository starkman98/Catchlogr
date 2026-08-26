using FishingLog.Domain.Entities;
using FishingLog.Domain.Interfaces;
using FishingLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FishingLog.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ownership-scoped catch-photo persistence.
/// </summary>
public sealed class CatchPhotoRepository : ICatchPhotoRepository
{
    private readonly FishingLogDbContext _context;

    /// <summary>Initializes the repository with its database context.</summary>
    public CatchPhotoRepository(FishingLogDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public Task<CatchPhoto?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
        => QueryForUser(userId).FirstOrDefaultAsync(photo => photo.Id == id, ct);

    /// <inheritdoc/>
    public Task<CatchPhoto?> GetByCatchIdAsync(Guid catchId, Guid userId, CancellationToken ct = default)
        => QueryForUser(userId).FirstOrDefaultAsync(photo => photo.CatchId == catchId, ct);

    /// <inheritdoc/>
    public async Task ReplaceAsync(
        CatchPhoto? existing,
        CatchPhoto replacement,
        Catch owner,
        CancellationToken ct = default)
    {
        if (existing is not null)
            _context.CatchPhotos.Remove(existing);
        await _context.CatchPhotos.AddAsync(replacement, ct);
        _context.Catches.Update(owner);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        CatchPhoto photo,
        Catch owner,
        CancellationToken ct = default)
    {
        _context.CatchPhotos.Remove(photo);
        _context.Catches.Update(owner);
        await _context.SaveChangesAsync(ct);
    }

    private IQueryable<CatchPhoto> QueryForUser(Guid userId)
        => _context.CatchPhotos
            .Include(photo => photo.Catch)
            .Where(photo => _context.FishingTrips.Any(trip =>
                trip.Id == photo.Catch.TripId &&
                trip.UserId == userId));
}
