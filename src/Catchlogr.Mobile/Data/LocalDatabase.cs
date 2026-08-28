using Catchlogr.Mobile.Configuration;
using Catchlogr.Sync.Entities;
using SQLite;

namespace Catchlogr.Mobile.Data;

/// <summary>
/// Manages the local SQLite database connection and table initialization.
/// Registered as a singleton — one instance for the entire app lifetime.
/// </summary>
public sealed class LocalDatabase : ILocalDatabase
{
    private readonly DatabaseSettings _settings;
    private readonly SemaphoreSlim _activationLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;
    private string? _activeAccountDirectory;

    /// <inheritdoc/>
    public SQLiteAsyncConnection Connection => _connection
        ?? throw new InvalidOperationException(
            "No account database is active. Sign in before accessing local data.");

    /// <inheritdoc/>
    public Guid? ActiveUserId { get; private set; }

    /// <inheritdoc/>
    public string ActiveAccountDirectory => _activeAccountDirectory
        ?? throw new InvalidOperationException(
            "No account storage is active. Sign in before accessing local files.");

    /// <summary>
    /// Initializes a new instance of <see cref="LocalDatabase"/>.
    /// The database filename comes from <see cref="DatabaseSettings"/>;
    /// the account directory is selected during activation.
    /// </summary>
    public LocalDatabase(DatabaseSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc/>
    public async Task ActivateAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "A valid user identifier is required.",
                nameof(userId));

        await _activationLock.WaitAsync(ct);
        try
        {
            if (ActiveUserId == userId && _connection is not null)
                return;

            await CloseConnectionAsync();

            var accountDirectory = Path.Combine(
                _settings.RootDirectory ?? FileSystem.AppDataDirectory,
                "accounts",
                userId.ToString("N"));
            Directory.CreateDirectory(accountDirectory);

            var fileName = Path.GetFileName(_settings.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException(
                    "A valid local database file name is required.");

            var connection = new SQLiteAsyncConnection(
                Path.Combine(accountDirectory, fileName),
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            try
            {
                await connection.CreateTableAsync<FishingTripLocalEntity>();
                await connection.CreateTableAsync<SyncMetadataEntity>();
                await connection.CreateTableAsync<CatchLocalEntity>();
            }
            catch
            {
                await connection.CloseAsync();
                throw;
            }

            _connection = connection;
            _activeAccountDirectory = accountDirectory;
            ActiveUserId = userId;
        }
        finally
        {
            _activationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task CloseAsync(CancellationToken ct = default)
    {
        await _activationLock.WaitAsync(ct);
        try
        {
            await CloseConnectionAsync();
        }
        finally
        {
            _activationLock.Release();
        }
    }

    private async Task CloseConnectionAsync()
    {
        var connection = _connection;
        _connection = null;
        _activeAccountDirectory = null;
        ActiveUserId = null;

        if (connection is not null)
            await connection.CloseAsync();
    }
}
