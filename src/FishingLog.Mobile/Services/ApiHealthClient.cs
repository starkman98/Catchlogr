using FishingLog.Sync.Abstractions;

namespace FishingLog.Mobile.Services;

public class ApiHealthClient : IApiHealthClient
{
    private readonly HttpClient _httpClient;

    public ApiHealthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("health", ct);
            return response.IsSuccessStatusCode;
        } 
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var message = ex.Message;
            var inner = ex.InnerException?.Message;
            return false;
        }
    }
}
