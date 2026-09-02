using System.Net;
using System.Text.Json;
using Catchlogr.Tests.TestDoubles;
using Catchlogr.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Catchlogr.Tests.Web;

/// <summary>Tests HTTP communication with the Catchlogr Identity API.</summary>
public sealed class IdentityApiClientTests
{
    /// <summary>Verifies exact encoding of the confirmation API request.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_QueryValues_EncodesRequest()
    {
        var (sut, handler) = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await sut.ConfirmEmailAsync(
            "user/123 +?",
            "abc+/=_ token",
            CancellationToken.None);

        result.Should().Be(IdentityActionResult.Succeeded);
        handler.LastRequestUri.Should().Be(
            "https://api.catchlogr.test/api/auth/confirmEmail" +
            "?userId=user%2F123%20%2B%3F&code=abc%2B%2F%3D_%20token");
    }

    /// <summary>Verifies that invalid tokens are reported as rejected.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_BadRequest_ReturnsRejected()
    {
        var (sut, _) = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        var result = await sut.ConfirmEmailAsync(
            "user-123",
            "expired-code",
            CancellationToken.None);

        result.Should().Be(IdentityActionResult.Rejected);
    }

    /// <summary>Verifies that connection failures produce a safe page result.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ConnectionFailure_ReturnsUnavailable()
    {
        var (sut, _) = CreateClient((_, _) =>
            Task.FromException<HttpResponseMessage>(
                new HttpRequestException("Connection failed.")));

        var result = await sut.ConfirmEmailAsync(
            "user-123",
            "abc_123",
            CancellationToken.None);

        result.Should().Be(IdentityActionResult.ServiceUnavailable);
    }

    /// <summary>Verifies that HTTP timeouts produce a safe page result.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_Timeout_ReturnsUnavailable()
    {
        var (sut, _) = CreateClient((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException()));

        var result = await sut.ConfirmEmailAsync(
            "user-123",
            "abc_123",
            CancellationToken.None);

        result.Should().Be(IdentityActionResult.ServiceUnavailable);
    }

    /// <summary>Verifies the reset request shape sent to the Identity API.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ValidInput_SendsIdentityRequest()
    {
        string? requestJson = null;
        HttpMethod? requestMethod = null;
        var (sut, handler) = CreateClient(async (request, cancellationToken) =>
        {
            requestMethod = request.Method;
            requestJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await sut.ResetPasswordAsync(
            " angler@example.com ",
            "abc+123",
            "NewPassword1!",
            CancellationToken.None);

        result.Should().Be(IdentityActionResult.Succeeded);
        requestMethod.Should().Be(HttpMethod.Post);
        handler.LastRequestUri.Should().Be(
            "https://api.catchlogr.test/api/auth/resetPassword");
        using var document = JsonDocument.Parse(requestJson!);
        document.RootElement.GetProperty("email").GetString()
            .Should().Be("angler@example.com");
        document.RootElement.GetProperty("resetCode").GetString()
            .Should().Be("abc+123");
        document.RootElement.GetProperty("newPassword").GetString()
            .Should().Be("NewPassword1!");
    }

    private static (IdentityApiClient Client, StubHttpMessageHandler Handler)
        CreateClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
                responseFactory)
    {
        var handler = new StubHttpMessageHandler(responseFactory);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.catchlogr.test")
        };
        var client = new IdentityApiClient(
            httpClient,
            Substitute.For<ILogger<IdentityApiClient>>());
        return (client, handler);
    }
}
