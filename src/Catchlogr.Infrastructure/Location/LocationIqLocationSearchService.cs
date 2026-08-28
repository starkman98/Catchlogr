using Catchlogr.Application.Interfaces;
using Catchlogr.Contracts.LocationDTOs;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace Catchlogr.Infrastructure.Location;

/// <summary>
/// Searches named locations and water features using LocationIQ.
/// </summary>
/// <example>
/// Register this type as the typed HTTP implementation of
/// <see cref="ILocationSearchService"/> in the API dependency container.
/// </example>
public sealed class LocationIqLocationSearchService : ILocationSearchService
{
    private const int ProviderResultLimit = 10;
    private const int ResultLimit = 5;

    private static readonly HashSet<string> PrimaryWaterTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "lake",
            "reservoir",
            "pond",
            "basin",
            "water"
        };

    private static readonly HashSet<string> SecondaryWaterTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "river",
            "stream",
            "canal"
        };

    private readonly HttpClient _httpClient;
    private readonly LocationIqOptions _options;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="LocationIqLocationSearchService"/> class.
    /// </summary>
    /// <param name="httpClient">Client used for LocationIQ requests.</param>
    /// <param name="options">Configured LocationIQ endpoint and access token.</param>
    public LocationIqLocationSearchService(
        HttpClient httpClient,
        IOptions<LocationIqOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocationSearchResult>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length is < 2 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Location query must contain between 2 and 100 characters.");
        }

        var apiKey = _options.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "LocationIQ API key is not configured.");
        }

        using var response = await _httpClient.GetAsync(
            BuildRequestUri(trimmedQuery, apiKey),
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return [];

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<LocationIqSearchResult[]>(cancellationToken: ct);

        return (payload ?? [])
            .Select((result, index) => new { Result = result, Index = index })
            .Where(item => IsValid(item.Result))
            .OrderBy(item => GetWaterPriority(item.Result))
            .ThenBy(item => item.Index)
            .Take(ResultLimit)
            .Select(item => MapResult(item.Result))
            .ToArray();
    }

    private Uri BuildRequestUri(string query, string apiKey)
    {
        var parameters = new Dictionary<string, string>
        {
            ["key"] = apiKey,
            ["q"] = query,
            ["format"] = "json",
            ["addressdetails"] = "1",
            ["normalizeaddress"] = "1",
            ["accept-language"] = "en",
            ["limit"] = ProviderResultLimit.ToString(CultureInfo.InvariantCulture),
            ["source"] = "nom"
        };

        var queryString = string.Join(
            "&",
            parameters.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}=" +
                $"{Uri.EscapeDataString(pair.Value)}"));

        return new UriBuilder(new Uri(_options.BaseUri, "/v1/search"))
        {
            Query = queryString
        }.Uri;
    }

    private static bool IsValid(LocationIqSearchResult result)
        => !string.IsNullOrWhiteSpace(result.DisplayName) &&
           TryGetCoordinates(result, out _, out _);

    private static int GetWaterPriority(LocationIqSearchResult result)
    {
        var featureClass = result.FeatureClass?.Trim();
        var featureType = result.FeatureType?.Trim();

        if (PrimaryWaterTypes.Contains(featureType ?? string.Empty) ||
            string.Equals(featureClass, "natural", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(featureType, "water", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(featureClass, "waterway", StringComparison.OrdinalIgnoreCase) ||
            SecondaryWaterTypes.Contains(featureType ?? string.Empty))
        {
            return 1;
        }

        return 2;
    }

    private static LocationSearchResult MapResult(LocationIqSearchResult result)
    {
        _ = TryGetCoordinates(result, out var latitude, out var longitude);

        var displayName = result.DisplayName!.Trim();
        var name = string.IsNullOrWhiteSpace(result.Name)
            ? displayName.Split(',', 2)[0].Trim()
            : result.Name.Trim();

        return new LocationSearchResult(
            name,
            displayName,
            latitude,
            longitude);
    }

    private static bool TryGetCoordinates(
        LocationIqSearchResult result,
        out double latitude,
        out double longitude)
    {
        longitude = default;

        return double.TryParse(
                   result.Latitude,
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out latitude) &&
               latitude is >= -90 and <= 90 &&
               double.TryParse(
                   result.Longitude,
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out longitude) &&
               longitude is >= -180 and <= 180;
    }
}
