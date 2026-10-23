using Catchlogr.Contracts.AuthenticationDTOs;
using FluentAssertions;

namespace Catchlogr.Tests.Web;

/// <summary>Checks that client feedback accepts only known validation codes.</summary>
public sealed class PasswordValidationErrorsTests
{
    /// <summary>Ignores arbitrary response text and unknown Identity errors.</summary>
    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"errors\":null}")]
    [InlineData("{\"errors\":{\"DuplicateEmail\":[\"private data\"]}}")]
    public void Read_UnknownResponse_DoesNotExposeText(string json)
        => PasswordValidationErrors.Read(json).Should().BeEmpty();

    /// <summary>Uses recognized codes instead of the server's message contents.</summary>
    [Fact]
    public void Read_KnownCode_ReturnsOnlyRecognizedCode()
        => PasswordValidationErrors.Read(
            "{\"errors\":{\"PasswordBlocklisted\":[\"untrusted text\"],\"Other\":[\"secret\"]}}")
            .Should().Equal(PasswordRequirements.Blocklisted);
}
