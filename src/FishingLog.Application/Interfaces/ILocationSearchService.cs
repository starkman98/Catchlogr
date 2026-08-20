using FishingLog.Contracts.LocationDTOs;

namespace FishingLog.Application.Interfaces;

/// <summary>
/// Searches a geocoding provider for named locations.
/// </summary>
public interface ILocationSearchService
{
    /// <summary>
    /// Searches for locations matching a user-entered name.
    /// </summary>
    /// <param name="query">The location name or postal code to search for.</param>
    /// <param name="ct">Token used to cancel the provider request.</param>
    /// <returns>Matching locations ordered by provider relevance.</returns>
    Task<IReadOnlyList<LocationSearchResult>> SearchAsync(
        string query,
        CancellationToken ct = default);
}
