using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using SQLite;

namespace FishingLog.Mobile.Data.Repositories;

public class CatchLocalRepository : ICatchLocalRepository
{
    private readonly ILocalDatabase _localDatabase;

    /// <summary>
    /// Initializes a new instance of <see cref="CatchLocalRepository"/>.
    /// Receives the connection from the singleton <see cref="ILocalDatabase"/>.
    /// </summary>
    public CatchLocalRepository(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    /// <inheritdoc/>
    public Task<List<CatchLocalEntity>> GetAllAsync(CancellationToken ct = default)
        => _localDatabase.Connection.Table<CatchLocalEntity>()
              .Where(x => !x.IsDeleted)
              .ToListAsync();

    /// <inheritdoc/>
    public Task<CatchLocalEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => _localDatabase.Connection.Table<CatchLocalEntity?>()
              .Where(x => x.Id == id && !x.IsDeleted)
              .FirstOrDefaultAsync();

    /// <inheritdoc/>
    public Task<CatchLocalEntity?> GetByServerIdAsync(Guid serverId, CancellationToken ct = default)
    {
        var serverIdAsString = serverId.ToString();
        return _localDatabase.Connection.Table<CatchLocalEntity?>()
            .Where(x => x.ServerId == serverIdAsString)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public Task<List<CatchLocalEntity>> GetByTripIdAsync(int localTripId, CancellationToken ct = default)
        => _localDatabase.Connection.Table<CatchLocalEntity>()
        .Where(x => x.FishingTripLocalId == localTripId && !x.IsDeleted)
        .ToListAsync();

    /// <inheritdoc/>
    public Task<List<CatchLocalEntity>> GetDirtyAsync(CancellationToken ct = default)
        => _localDatabase.Connection.Table<CatchLocalEntity>()
              .Where(x => x.IsDirty)
              .ToListAsync();

    /// <inheritdoc/>
    public async Task<int> AddAsync(CatchLocalEntity localCatch, CancellationToken ct = default)
    {
        localCatch.IsDirty = true;
        localCatch.LastModifiedUtc = DateTime.UtcNow;
        await _localDatabase.Connection.InsertAsync(localCatch);
        return localCatch.Id;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(CatchLocalEntity localCatch, CancellationToken ct = default)
    {
        localCatch.IsDirty = true;
        localCatch.LastModifiedUtc = DateTime.UtcNow;
        await _localDatabase.Connection.UpdateAsync(localCatch);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var localCatch = await GetByIdAsync(id, ct);
        if (localCatch is null)
            return;

        localCatch.IsDeleted = true;
        localCatch.IsDirty = true;
        localCatch.LastModifiedUtc = DateTime.UtcNow;
        await _localDatabase.Connection.UpdateAsync(localCatch);
    }

    /// <inheritdoc/>
    public Task PermanentlyDeleteAsync(int id, CancellationToken ct = default)
        => _localDatabase.Connection.DeleteAsync<CatchLocalEntity>(id);

    /// <inheritdoc/>
    public async Task SaveFromServerAsync(CatchLocalEntity localCatch, CancellationToken ct = default)
    {
        // Id == 0 means sqlite-net-pcl has not assigned a local key yet → new record
        if (localCatch.Id == 0)
            await _localDatabase.Connection.InsertAsync(localCatch);
        else
            await _localDatabase.Connection.UpdateAsync(localCatch);
    }
}
