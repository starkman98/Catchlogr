using FishingLog.Contracts.PhotoDTOs;
using FishingLog.Mobile.Data;
using FishingLog.Sync.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FishingLog.Mobile.Services.Photos;

/// <summary>
/// Sends multipart photo requests to the FishingLog API.
/// </summary>
public sealed class PhotoApiClient : IPhotoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAccountStorageContext _accountStorage;

    /// <summary>Initializes the client with its configured HTTP transport.</summary>
    public PhotoApiClient(
        HttpClient httpClient,
        IAccountStorageContext accountStorage)
    {
        _httpClient = httpClient;
        _accountStorage = accountStorage;
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(
        Guid catchId,
        string localFilePath,
        CancellationToken ct = default)
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

        using var response = await _httpClient.PostAsync(
            $"api/catches/{catchId:D}/photos",
            form,
            ct);
        response.EnsureSuccessStatusCode();

        var uploaded = await response.Content.ReadFromJsonAsync<PhotoUploadResponse>(cancellationToken: ct)
            ?? throw new HttpRequestException("The photo API returned an empty response.");
        return uploaded.Url;
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAsync(
        string photoUrl,
        CancellationToken ct = default)
    {
        var photoId = ParsePhotoId(photoUrl);
        var directory = Path.Combine(
            _accountStorage.ActiveAccountDirectory,
            "photos",
            "server");
        Directory.CreateDirectory(directory);

        using var response = await _httpClient.GetAsync(
            $"api/photos/{photoId:D}",
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        response.EnsureSuccessStatusCode();

        var extension = response.Content.Headers.ContentType?.MediaType switch
        {
            "image/png" => ".png",
            "image/heic" => ".heic",
            "image/heif" => ".heif",
            _ => ".jpg"
        };
        var localPath = Path.Combine(directory, $"{photoId:N}{extension}");
        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var destination = new FileStream(
            localPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await source.CopyToAsync(destination, ct);
        return localPath;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string photoUrl, CancellationToken ct = default)
    {
        var photoId = ParsePhotoId(photoUrl);

        using var response = await _httpClient.DeleteAsync(
            $"api/photos/{photoId:D}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;
        response.EnsureSuccessStatusCode();
    }

    private static Guid ParsePhotoId(string photoUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoUrl);
        var path = Uri.TryCreate(photoUrl, UriKind.Absolute, out var absolute)
            ? absolute.AbsolutePath
            : photoUrl.Split('?', '#')[0];
        return Guid.TryParse(Path.GetFileName(path), out var photoId)
            ? photoId
            : throw new ArgumentException(
                "The protected photo URL does not contain a valid photo identifier.",
                nameof(photoUrl));
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".heif" => "image/heif",
        _ => "image/jpeg"
    };
}
