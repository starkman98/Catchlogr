using FishingLog.Contracts;
using FishingLog.Sync.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace FishingLog.Mobile.Services;

/// <summary>
/// HttpClient implementation of <see cref="IFishingTripApiClient"/>.
/// Registered as a typed HttpClient in MauiProgram.cs.
/// <para>
/// Error handling strategy:
/// - 404 Not Found  → return null / false (expected, not an exception)
/// - 400 / 409      → return null / false (validation or conflict)
/// - 5xx            → throws HttpRequestException (unexpected, let it propagate)
/// </para>
/// </summary>
public class FishingTripApiClient : IFishingTripApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="FishingTripApiClient"/>.
    /// BaseAddress and Timeout are configured in MauiProgram.cs.
    /// </summary>
    public FishingTripApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/fishing-trips", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<FishingTripResponse>>(ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<List<FishingTripResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        // Use ISO 8601 round-trip format and URL-encode so the + in timezone offset survives
        var encoded = Uri.EscapeDataString(since.ToString("O"));
        var response = await _httpClient.GetAsync($"api/fishing-trips?modifiedSince={encoded}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<FishingTripResponse>>(ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/fishing-trips/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FishingTripResponse>(ct);
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse?> CreateAsync(CreateFishingTripRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/fishing-trips", request, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<FishingTripResponse>(ct);
    }

    /// <inheritdoc/>
    public async Task<FishingTripResponse?> UpdateAsync(Guid id, UpdateFishingTripRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/fishing-trips/{id}", request, ct);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FishingTripResponse>(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/fishing-trips/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}
