using Catchlogr.Application.Validators;
using Catchlogr.Contracts.AuthenticationDTOs;
using FluentAssertions;

namespace Catchlogr.Tests.Validators;

/// <summary>Tests length and predictable-password rules independently of Identity.</summary>
public sealed class PasswordPolicyTests
{
    /// <summary>Counts Unicode code points rather than UTF-16 units and enforces both boundaries.</summary>
    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(64, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Validate_UnicodeLength_EnforcesBoundaries(int count, bool valid)
    {
        var password = string.Concat(Enumerable.Repeat("🐟", count));
        var errors = PasswordPolicy.Validate(password, "angler@example.com", false);
        errors.Should().BeEquivalentTo(valid ? Array.Empty<string>() : [PasswordRequirements.InvalidLength]);
    }

    /// <summary>Allows spaces, lowercase-only phrases, and words embedded in longer phrases.</summary>
    [Theory]
    [InlineData("reeds beside quiet water")]
    [InlineData("  reeds beside quiet water  ")]
    [InlineData("catchlogr beside quiet water")]
    [InlineData("密码可以很长而且包含许多不同的字符")]
    public void Validate_Passphrase_AcceptsWithoutCompositionRules(string password)
        => PasswordPolicy.Validate(password, "angler@example.com", false).Should().BeEmpty();

    /// <summary>Rejects whole predictable values derived from the account or service.</summary>
    [Theory]
    [InlineData("catchlogr123456789!")]
    [InlineData("ANGLER@EXAMPLE.COM")]
    [InlineData("angler1234567890")]
    [InlineData("catchlogrpassword2026!")]
    public void Validate_AccountOrServiceValue_Rejects(string password)
        => PasswordPolicy.Validate(password, "angler@example.com", false)
            .Should().ContainSingle().Which.Should().Be(PasswordRequirements.Predictable);

    /// <summary>Rejects an invalid surrogate and a whitespace-only secret.</summary>
    [Fact]
    public void Validate_InvalidText_Rejects()
    {
        PasswordPolicy.Validate(new string(' ', 20), null, false).Should().NotBeEmpty();
        PasswordPolicy.Validate(new string('a', 15) + '\uD800', null, false).Should().NotBeEmpty();
    }
}
