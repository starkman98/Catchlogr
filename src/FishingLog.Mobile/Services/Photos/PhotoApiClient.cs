using FishingLog.Contracts.PhotoDTOs;
using FishingLog.Sync.Abstractions;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FishingLog.Mobile.Services.Photos;

/// <summary>
/// Sends multipart photo requests to the FishingLog API.
/// </summary>
public sealed class PhotoApiClient : IPhotoApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>Initializes the client with its configured HTTP transport.</summary>
    public PhotoApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(string localFilePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);

        await using var stream = new FileStream(
            localFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(localFilePath));

        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", Path.GetFileName(localFilePath));

        using var response = await _httpClient.PostAsync("api/photos", form, ct);
        response.EnsureSuccessStatusCode();

        var uploaded = await response.Content.ReadFromJsonAsync<PhotoUploadResponse>(cancellationToken: ct)
            ?? throw new HttpRequestException("The photo API returned an empty response.");
        return uploaded.Url;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string photoUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri))
            return;

        var fileName = Path.GetFileName(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        using var response = await _httpClient.DeleteAsync(
            $"api/photos/{Uri.EscapeDataString(fileName)}",
            ct);
        response.EnsureSuccessStatusCode();
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".heif" => "image/heif",
        _ => "image/jpeg"
    };
}
