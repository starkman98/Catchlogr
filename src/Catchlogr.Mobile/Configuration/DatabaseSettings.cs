namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Defines local database settings.
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Gets or sets the root directory under which account storage is created.
    /// </summary>
    public string? RootDirectory { get; set; }

    /// <summary>Gets or sets the SQLite database file name.</summary>
    public string FileName { get; set; } = "catchlogr.db3";
}
