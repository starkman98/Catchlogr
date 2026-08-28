using Catchlogr.Contracts.LocationDTOs;

namespace Catchlogr.Mobile.Services;

/// <summary>
/// Searches named locations through the Catchlogr API.
/// </summary>
public interface ILocationSearchApiClient
{
    /// <summary>
    /// Searches for locations matching a user-entered name.
    /// </summary>
    /// <param name="query">The location name or postal code.</param>
    /// <param name="ct">Token used to cancel the API request.</param>
    /// <returns>Matching locations ordered by provider relevance.</returns>
    Task<IReadOnlyList<LocationSearchResult>> SearchAsync(
        string query,
        CancellationToken ct = default);
}
