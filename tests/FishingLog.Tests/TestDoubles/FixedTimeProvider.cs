namespace FishingLog.Tests.TestDoubles;

/// <summary>
/// Provides a fixed UTC time for deterministic tests.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedTimeProvider"/> class.
    /// </summary>
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow.ToUniversalTime();
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
