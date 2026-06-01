using FishingLog.Domain.Entities;
using FishingLog.Domain.Interfaces;
using FishingLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FishingLog.Infrastructure.Repositories;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="ICatchRepository"/>.
/// </summary>
public class CatchRepository : ICatchRepository
{
    private readonly FishingLogDbContext _context;

    public CatchRepository(FishingLogDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Catch catchToAdd, CancellationToken ct = default)
    {
        await _context.Catches.AddAsync(catchToAdd, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await GetByIdAsync(id, ct);

        if (trip is null)
            return;

        _context.Catches.Remove(trip);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<Catch>> GetAllAsync(CancellationToken ct = default)
        => await _context.Catches
        .AsNoTracking()
        .OrderByDescending(c => c.CaughtAt)
        .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Catch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Catches
        .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public async Task<List<Catch>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default)
        => await _context.Catches
        .AsNoTracking()
        .Where(c => c.TripId == tripId)
        .OrderByDescending(c => c.CaughtAt)
        .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<List<Catch>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
        => await _context.Catches
        .Where(c => c.LastModified > since)
        .OrderBy(c => c.LastModified)
        .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task UpdateAsync(Catch catchToUpdate, CancellationToken ct = default)
    {
        _context.Catches.Update(catchToUpdate);
        await _context.SaveChangesAsync();
    }
}
