using FishingLog.Mobile.Configuration;
using FishingLog.Sync.Entities;
using SQLite;

namespace FishingLog.Mobile.Data;

/// <summary>
/// Manages the local SQLite database connection and table initialization.
/// Registered as a singleton — one instance for the entire app lifetime.
/// </summary>
public class LocalDatabase : ILocalDatabase
{
    private readonly SQLiteAsyncConnection _connection;
    private bool _initialized;

    /// <inheritdoc/>
    public SQLiteAsyncConnection Connection => _connection;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDatabase"/>.
    /// The database file path comes from <see cref="DatabaseSettings.FullPath"/>.
    /// </summary>
    public LocalDatabase(DatabaseSettings settings)
    {
        _connection = new SQLiteAsyncConnection(
            settings.FullPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        // Add a CreateTableAsync call here for every new entity you add later
        await _connection.CreateTableAsync<FishingTripLocalEntity>();
        await _connection.CreateTableAsync<SyncMetadataEntity>();
        await _connection.CreateTableAsync<CatchLocalEntity>();

        _initialized = true;
    }
}