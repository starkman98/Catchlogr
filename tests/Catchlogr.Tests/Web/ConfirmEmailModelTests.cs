using System.Net;
using Catchlogr.Tests.TestDoubles;
using Catchlogr.Web.Pages;
using FluentAssertions;
using NSubstitute;

namespace Catchlogr.Tests.Web;

/// <summary>Tests the public email-confirmation page.</summary>
public sealed class ConfirmEmailModelTests
{
    /// <summary>
    /// Verifies that required query parameters are rejected without calling the API.
    /// </summary>
    [Theory]
    [InlineData(null, "code")]
    [InlineData("user", null)]
    [InlineData("", "code")]
    [InlineData("user", " ")]
    public async Task OnGetAsync_MissingParameter_DoesNotCallApi(
        string? userId,
        string? code)
    {
        var (sut, handler) = CreateModel(HttpStatusCode.OK);

        await sut.OnGetAsync(userId, code);

        sut.Success.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that a successful API confirmation displays the success state.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_ApiAcceptsConfirmation_Succeeds()
    {
        var (sut, handler) = CreateModel(HttpStatusCode.OK);

        await sut.OnGetAsync("user-123", "abc_123");

        sut.Success.Should().BeTrue();
        handler.RequestCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that an API rejection displays the failure state.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_ApiRejectsConfirmation_Fails()
    {
        var (sut, handler) = CreateModel(HttpStatusCode.BadRequest);

        await sut.OnGetAsync("user-123", "expired-code");

        sut.Success.Should().BeFalse();
        handler.RequestCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that query values are encoded exactly once for the API request.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_QueryValues_EncodesApiRequest()
    {
        var (sut, handler) = CreateModel(HttpStatusCode.OK);

        await sut.OnGetAsync("user/123 +?", "abc+/=_ token");

        handler.LastRequestUri.Should().Be(
            "https://api.catchlogr.test/api/auth/confirmEmail" +
            "?userId=user%2F123%20%2B%3F&code=abc%2B%2F%3D_%20token");
    }

    private static (ConfirmEmailModel Model, StubHttpMessageHandler Handler)
        CreateModel(HttpStatusCode responseStatusCode)
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(responseStatusCode)));
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.catchlogr.test")
        };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("CatchlogrApi").Returns(client);

        return (new ConfirmEmailModel(factory), handler);
    }
}
