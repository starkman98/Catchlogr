using Catchlogr.Contracts.LocationDTOs;
using System.Net.Http.Json;

namespace Catchlogr.Mobile.Services;

/// <summary>
/// Searches locations through the Catchlogr REST API.
/// </summary>
public sealed class LocationSearchApiClient : ILocationSearchApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationSearchApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The configured Catchlogr API client.</param>
    public LocationSearchApiClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocationSearchResult>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var encodedQuery = Uri.EscapeDataString(query.Trim());
        using var response = await _httpClient.GetAsync(
            $"api/locations/search?query={encodedQuery}",
            ct);
        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<LocationSearchResult>>(cancellationToken: ct) ?? [];
    }
}
