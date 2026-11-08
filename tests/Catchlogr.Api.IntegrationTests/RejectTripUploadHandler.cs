using System.Net;

namespace Catchlogr.Api.IntegrationTests;

/// <summary>Simulates a trip-upload outage while allowing real authentication and downloads.</summary>
internal sealed class RejectTripUploadHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/fishing-trips"
            ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
            : base.SendAsync(request, ct);
}
