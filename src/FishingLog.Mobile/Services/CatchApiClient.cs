using FishingLog.Contracts.CatchDTOs;
using FishingLog.Sync.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace FishingLog.Mobile.Services;

/// <summary>
/// HttpClient implementation of <see cref="ICatchApiClient"/>.
/// Registered as a typed HttpClient in MauiProgram.cs.
/// <para>
/// Error handling strategy:
/// - 404 Not Found  → return null / false (expected, not an exception)
/// - 400 / 409      → return null / false (validation or conflict)
/// - 5xx            → throws HttpRequestException (unexpected, let it propagate)
/// </para>
/// </summary>
public class CatchApiClient : ICatchApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="CatchApiClient"/>.
    /// BaseAddress and Timeout are configured in MauiProgram.cs.
    /// </summary>
    public CatchApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<List<CatchResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/catches", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CatchResponse>>(ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<List<CatchResponse>> GetModifiedSinceAsync(DateTime since, CancellationToken ct = default)
    {
        // Use ISO 8601 round-trip format and URL-encode so the + in timezone offset survives
        var encoded = Uri.EscapeDataString(since.ToString("O"));
        var response = await _httpClient.GetAsync($"api/catches?modifiedSince={encoded}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CatchResponse>>(ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<CatchResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/catches/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatchResponse>(ct);
    }

    public async Task<List<CatchResponse>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/fishing-trips/{tripId}/catches", ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CatchResponse>>(ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<CatchResponse?> CreateAsync(Guid tripId, CreateCatchRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/fishing-trips/{tripId}/catches", request, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CatchResponse>(ct);
    }

    /// <inheritdoc/>
    public async Task<CatchResponse?> UpdateAsync(Guid id, UpdateCatchRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/catches/{id}", request, ct);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatchResponse>(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/catches/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}