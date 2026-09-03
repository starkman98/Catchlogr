using SQLite;

namespace Catchlogr.Mobile.Data;

/// <summary>
/// Abstraction for the local SQLite database.
/// Manages the connection and table initialization.
/// </summary>
public interface ILocalDatabase : IAccountStorageContext
{
    /// <summary>
    /// Gets the underlying SQLite async connection.
    /// Repositories use this to run queries.
    /// </summary>
    SQLiteAsyncConnection Connection { get; }

    /// <summary>
    /// Activates and initializes the private database for an account.
    /// </summary>
    /// <param name="userId">The authenticated Identity user identifier.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    Task ActivateAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Closes the active account database.</summary>
    /// <param name="ct">A token that can cancel the operation.</param>
    Task CloseAsync(CancellationToken ct = default);
}
