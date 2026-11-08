namespace Catchlogr.Api.IntegrationTests;

/// <summary>Cancels the scenario after authentication and local trip creation, during upload.</summary>
internal sealed class CancelTripUploadHandler(HttpMessageHandler inner, CancellationTokenSource cancellation) : DelegatingHandler(inner)
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/fishing-trips")
        {
            cancellation.Cancel();
            return Task.FromCanceled<HttpResponseMessage>(cancellation.Token);
        }
        return base.SendAsync(request, ct);
    }
}
