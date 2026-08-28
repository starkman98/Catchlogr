namespace Catchlogr.Tests.TestDoubles;

/// <summary>
/// Handles HTTP requests with a test-provided response delegate.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
        _responseFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.
    /// </summary>
    public StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
            responseFactory)
    {
        _responseFactory = responseFactory;
    }

    /// <summary>
    /// Gets the URI of the most recent request.
    /// </summary>
    public Uri? LastRequestUri { get; private set; }

    /// <summary>
    /// Gets the number of handled requests.
    /// </summary>
    public int RequestCount { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        RequestCount++;

        return _responseFactory(request, cancellationToken);
    }
}
