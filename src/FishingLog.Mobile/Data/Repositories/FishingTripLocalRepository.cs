using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using SQLite;

namespace FishingLog.Mobile.Data.Repositories;

/// <summary>
/// sqlite-net-pcl implementation of the local fishing trip repository.
/// </summary>
public class FishingTripLocalRepository : IFishingTripLocalRepository
{
    private readonly ILocalDatabase _localDatabase;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripLocalRepository"/>.
    /// Receives the connection from the singleton <see cref="ILocalDatabase"/>.
    /// </summary>
    public FishingTripLocalRepository(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    /// <inheritdoc/>
    public Task<List<FishingTripLocalEntity>> GetAllAsync(CancellationToken ct = default)
        => _localDatabase.Connection.Table<FishingTripLocalEntity>()
              .Where(x => !x.IsDeleted)
              .ToListAsync();

    /// <inheritdoc/>
    public async Task<FishingTripLocalEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _localDatabase.Connection.Table<FishingTripLocalEntity>()
              .Where(x => x.Id == id && !x.IsDeleted)
              .FirstOrDefaultAsync();

    /// <inheritdoc/>
    public async Task<FishingTripLocalEntity?> GetByServerIdAsync(Guid serverId, CancellationToken ct = default)
    {
        var serverIdAsString = serverId.ToString();
        return await _localDatabase.Connection.Table<FishingTripLocalEntity>()
            .Where(x => x.ServerId == serverIdAsString)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public Task<List<FishingTripLocalEntity>> GetDirtyAsync(CancellationToken ct = default)
        => _localDatabase.Connection.Table<FishingTripLocalEntity>()
              .Where(x => x.IsDirty)
              .ToListAsync();

    /// <inheritdoc/>
    public async Task<int> AddAsync(FishingTripLocalEntity trip, CancellationToken ct = default)
    {
        trip.IsDirty = true;
        trip.LastModifiedUtc = DateTime.UtcNow;
        await _localDatabase.Connection.InsertAsync(trip);
        return trip.Id;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(FishingTripLocalEntity trip, CancellationToken ct = default)
    {
        trip.IsDirty = true;
        trip.LastModifiedUtc = DateTime.UtcNow;
        await _localDatabase.Connection.UpdateAsync(trip);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var trip = await GetByIdAsync(id, ct);
        if (trip is null)
            return;

        trip.IsDeleted = true;
        trip.IsDirty = true;
        trip.LastModifiedUtc = DateTime.UtcNow;
        await _localDatabase.Connection.UpdateAsync(trip);
    }

    /// <inheritdoc/>
    public Task PermanentlyDeleteAsync(int id, CancellationToken ct = default)
        => _localDatabase.Connection.DeleteAsync<FishingTripLocalEntity>(id);

    /// <inheritdoc/>
    public async Task SaveFromServerAsync(FishingTripLocalEntity trip, CancellationToken ct = default)
    {
        // Id == 0 means sqlite-net-pcl has not assigned a local key yet → new record
        if (trip.Id == 0)
            await _localDatabase.Connection.InsertAsync(trip);
        else
            await _localDatabase.Connection.UpdateAsync(trip);
    }
}
