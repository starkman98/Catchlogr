using FishingLog.Domain.Entities;
using FishingLog.Domain.Interfaces;
using FishingLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FishingLog.Infrastructure.Repositories;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="IFishingTripRepository"/>.
/// </summary>
public class FishingTripRepository : IFishingTripRepository
{
    private readonly FishingLogDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripRepository"/>.
    /// </summary>
    public FishingTripRepository(FishingLogDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public Task<List<FishingTrip>> GetAllAsync(Guid userId ,CancellationToken ct = default)
        => _context.FishingTrips
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public Task<FishingTrip?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
        => _context.FishingTrips
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

    /// <inheritdoc/>
    public Task<List<FishingTrip>> GetModifiedSinceAsync(Guid userId, DateTime since, CancellationToken ct = default)
        => _context.FishingTrips
            .Where(t => t.LastModified > since && t.UserId == userId)
            .OrderBy(t => t.LastModified)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task AddAsync(FishingTrip trip, CancellationToken ct = default)
    {
        await _context.FishingTrips.AddAsync(trip, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(FishingTrip trip, CancellationToken ct = default)
    {
        _context.FishingTrips.Update(trip);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var trip = await GetByIdAsync(id, userId, ct);
        if (trip is null)
            return;

        _context.FishingTrips.Remove(trip);
        await _context.SaveChangesAsync(ct);
    }
}