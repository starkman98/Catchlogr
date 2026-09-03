using Catchlogr.Domain.Entities;
using Catchlogr.Domain.Interfaces;
using Catchlogr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catchlogr.Infrastructure.Repositories;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="ICatchRepository"/>.
/// </summary>
public class CatchRepository : ICatchRepository
{
    private readonly CatchlogrDbContext _context;

    public CatchRepository(CatchlogrDbContext context)
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
    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var catchToDelete = await GetByIdAsync(id, userId, ct);

        if (catchToDelete is null)
            return;

        _context.Catches.Remove(catchToDelete);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<List<Catch>> GetAllAsync(Guid userId, CancellationToken ct = default)
        => QueryForUser(userId)
        .AsNoTracking()
        .OrderByDescending(c => c.CaughtAt)
        .ToListAsync(ct);

    /// <inheritdoc/>
    public Task<Catch?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
        => QueryForUser(userId)
        .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public Task<List<Catch>> GetByTripIdAsync(Guid tripId, Guid userId, CancellationToken ct = default)
        => QueryForUser(userId)
        .AsNoTracking()
        .Where(c => c.TripId == tripId)
        .OrderByDescending(c => c.CaughtAt)
        .ToListAsync(ct);

    /// <inheritdoc/>
    public Task<List<Catch>> GetModifiedSinceAsync(Guid userId, DateTime since, CancellationToken ct = default)
        => QueryForUser(userId)
        .Where(c => c.LastModified > since)
        .OrderBy(c => c.LastModified)
        .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task UpdateAsync(Catch catchToUpdate, CancellationToken ct = default)
    {
        _context.Catches.Update(catchToUpdate);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Catch> QueryForUser(Guid userId)
          => from currentCatch in _context.Catches
             join trip in _context.FishingTrips
                 on currentCatch.TripId equals trip.Id
             where trip.UserId == userId
             select currentCatch;
}