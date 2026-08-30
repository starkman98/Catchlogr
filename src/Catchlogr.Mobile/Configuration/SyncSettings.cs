namespace Catchlogr.Mobile.Configuration;

/// <summary>
/// Defines mobile synchronization settings.
/// </summary>
public class SyncSettings
{
    /// <summary>Gets or sets whether synchronization runs on startup.</summary>
    public bool AutoSyncOnStartup { get; set; } = true;

    /// <summary>Gets or sets the synchronization interval in minutes.</summary>
    public int SyncIntervalMinutes { get; set; } = 15;
}
