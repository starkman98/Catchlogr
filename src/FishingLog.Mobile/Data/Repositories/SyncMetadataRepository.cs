using FishingLog.Sync.Abstractions;
using FishingLog.Sync.Entities;
using SQLite;

namespace FishingLog.Mobile.Data.Repositories;

/// <summary>
/// sqlite-net-pcl implementation of <see cref="ISyncMetadataRepository"/>.
/// Performs an upsert: inserts a new row on first sync, updates it on subsequent syncs.
/// </summary>
public class SyncMetadataRepository : ISyncMetadataRepository
{
    private readonly SQLiteAsyncConnection _db;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncMetadataRepository"/>.
    /// </summary>
    public SyncMetadataRepository(ILocalDatabase localDatabase)
    {
        _db = localDatabase.Connection;
    }

    /// <inheritdoc/>
    public async Task<DateTime?> GetLastSyncAsync(string entityType, CancellationToken ct = default)
    {
        var record = await _db.Table<SyncMetadataEntity>()
            .Where(x => x.EntityType == entityType)
            .FirstOrDefaultAsync();

        return record?.LastSyncUtc;
    }

    /// <inheritdoc/>
    public async Task SetLastSyncAsync(string entityType, DateTime syncTime, CancellationToken ct = default)
    {
        var existing = await _db.Table<SyncMetadataEntity>()
            .Where(x => x.EntityType == entityType)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            await _db.InsertAsync(new SyncMetadataEntity
            {
                EntityType = entityType,
                LastSyncUtc = syncTime
            });
        }
        else
        {
            existing.LastSyncUtc = syncTime;
            await _db.UpdateAsync(existing);
        }
    }
}
