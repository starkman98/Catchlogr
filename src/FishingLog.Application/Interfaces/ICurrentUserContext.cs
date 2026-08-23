namespace FishingLog.Application.Interfaces;

/// <summary>
/// Provides the authenticated account for the current operation.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// Gets the authenticated account identifier.
    /// </summary>
    Guid UserId { get; }
}