using Catchlogr.Web.Pages;
using Catchlogr.Web.Services;
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
        var apiClient = Substitute.For<IIdentityApiClient>();
        var sut = new ConfirmEmailModel(apiClient);

        await sut.OnGetAsync(userId, code, CancellationToken.None);

        sut.Result.Should().Be(IdentityActionResult.Rejected);
        await apiClient.DidNotReceive().ConfirmEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the page displays each result returned by the API client.
    /// </summary>
    [Theory]
    [InlineData(IdentityActionResult.Succeeded)]
    [InlineData(IdentityActionResult.Rejected)]
    [InlineData(IdentityActionResult.ServiceUnavailable)]
    public async Task OnGetAsync_ApiResult_DisplaysResult(
        IdentityActionResult expectedResult)
    {
        var apiClient = Substitute.For<IIdentityApiClient>();
        apiClient.ConfirmEmailAsync(
                "user-123",
                "abc_123",
                Arg.Any<CancellationToken>())
            .Returns(expectedResult);
        var sut = new ConfirmEmailModel(apiClient);

        await sut.OnGetAsync(
            "user-123",
            "abc_123",
            CancellationToken.None);

        sut.Result.Should().Be(expectedResult);
    }

    /// <summary>Verifies that request cancellation is passed to the API client.</summary>
    [Fact]
    public async Task OnGetAsync_RequestCancellation_ForwardsToken()
    {
        var apiClient = Substitute.For<IIdentityApiClient>();
        using var cancellationSource = new CancellationTokenSource();
        var sut = new ConfirmEmailModel(apiClient);

        await sut.OnGetAsync(
            "user-123",
            "abc_123",
            cancellationSource.Token);

        await apiClient.Received(1).ConfirmEmailAsync(
            "user-123",
            "abc_123",
            cancellationSource.Token);
    }
}
