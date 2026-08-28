namespace Catchlogr.Mobile.Data;

/// <summary>
/// Describes the account whose private on-device storage is currently active.
/// </summary>
public interface IAccountStorageContext
{
    /// <summary>Gets the active account identifier, or null while signed out.</summary>
    Guid? ActiveUserId { get; }

    /// <summary>
    /// Gets the private directory for the active account.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no account storage has been activated.
    /// </exception>
    string ActiveAccountDirectory { get; }
}
