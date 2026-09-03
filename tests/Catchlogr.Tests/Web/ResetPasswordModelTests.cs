using Catchlogr.Web.Pages;
using Catchlogr.Web.Services;
using FluentAssertions;
using NSubstitute;

namespace Catchlogr.Tests.Web;

/// <summary>Tests the public password-reset page.</summary>
public sealed class ResetPasswordModelTests
{
    /// <summary>Verifies that an incomplete reset link is rejected.</summary>
    [Theory]
    [InlineData("", "code")]
    [InlineData("angler@example.com", "")]
    public void OnGet_MissingLinkValue_RejectsLink(string email, string code)
    {
        var sut = CreateModel(Substitute.For<IIdentityApiClient>());
        sut.Email = email;
        sut.Code = code;

        sut.OnGet();

        sut.Result.Should().Be(IdentityActionResult.Rejected);
    }

    /// <summary>Verifies that a complete reset link displays the reset form.</summary>
    [Fact]
    public void OnGet_CompleteLink_DisplaysForm()
    {
        var sut = CreateModel(Substitute.For<IIdentityApiClient>());
        sut.Email = "angler@example.com";
        sut.Code = "abc_123";

        sut.OnGet();

        sut.Result.Should().BeNull();
    }

    /// <summary>Verifies that invalid form state does not call the API.</summary>
    [Fact]
    public async Task OnPostAsync_InvalidForm_DoesNotCallApi()
    {
        var apiClient = Substitute.For<IIdentityApiClient>();
        var sut = CreateModel(apiClient);
        sut.ModelState.AddModelError(nameof(sut.NewPassword), "Required");

        await sut.OnPostAsync(CancellationToken.None);

        await apiClient.DidNotReceive().ResetPasswordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the page displays each API reset result.</summary>
    [Theory]
    [InlineData(IdentityActionResult.Succeeded)]
    [InlineData(IdentityActionResult.Rejected)]
    [InlineData(IdentityActionResult.ServiceUnavailable)]
    public async Task OnPostAsync_ApiResult_DisplaysResult(
        IdentityActionResult expectedResult)
    {
        var apiClient = Substitute.For<IIdentityApiClient>();
        apiClient.ResetPasswordAsync(
                "angler@example.com",
                "abc_123",
                "NewPassword1!",
                Arg.Any<CancellationToken>())
            .Returns(expectedResult);
        var sut = CreateModel(apiClient);

        await sut.OnPostAsync(CancellationToken.None);

        sut.Result.Should().Be(expectedResult);
    }

    private static ResetPasswordModel CreateModel(IIdentityApiClient apiClient)
        => new(apiClient)
        {
            Email = "angler@example.com",
            Code = "abc_123",
            NewPassword = "NewPassword1!",
            ConfirmPassword = "NewPassword1!"
        };
}
