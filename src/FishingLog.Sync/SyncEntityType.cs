namespace FishingLog.Sync;

/// <summary>
/// String constants for entity type keys used in <see cref="Entities.SyncMetadataEntity"/>.
/// Always use these instead of raw strings to avoid typos.
/// </summary>
public static class SyncEntityType
{
    /// <summary>Key for fishing trip sync metadata.</summary>
    public const string FishingTrip = "FishingTrip";
    public const string Catch = "Catch";
}
