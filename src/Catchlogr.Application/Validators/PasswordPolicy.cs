using System.Text;
using Catchlogr.Contracts.AuthenticationDTOs;

namespace Catchlogr.Application.Validators;

/// <summary>Validates new passwords without transforming the submitted secret.</summary>
public static class PasswordPolicy
{
    /// <summary>Returns stable error codes for length, blocklist, and account-specific checks.</summary>
    public static IReadOnlyList<string> Validate(string? password, string? email, bool isBlocklisted)
    {
        var length = password?.EnumerateRunes().Count() ?? 0;
        if (string.IsNullOrWhiteSpace(password) ||
            length < PasswordRequirements.MinimumLength || length > PasswordRequirements.MaximumLength ||
            !IsWellFormed(password))
            return [PasswordRequirements.InvalidLength];

        if (isBlocklisted)
            return [PasswordRequirements.Blocklisted];

        // Compare complete, predictable values; do not reject words inside a passphrase.
        var bases = new[] { "catchlogr", email, email?.Split('@')[0] };
        var comparison = password.TrimEnd('!', '.', '?');
        if (bases.Where(value => !string.IsNullOrEmpty(value))
            .Any(value => IsPredictableValue(comparison, value!)))
            return [PasswordRequirements.Predictable];

        return [];
    }

    private static bool IsPredictableValue(string password, string accountValue)
    {
        if (!password.StartsWith(accountValue, StringComparison.OrdinalIgnoreCase))
            return false;
        var suffix = password[accountValue.Length..];
        if (suffix.StartsWith("password", StringComparison.OrdinalIgnoreCase))
            suffix = suffix["password".Length..];
        return suffix.All(character => character is >= '0' and <= '9');
    }

    private static bool IsWellFormed(string password)
    {
        for (var index = 0; index < password.Length; index++)
        {
            if (!char.IsSurrogate(password[index]))
                continue;
            if (!char.IsHighSurrogate(password[index]) || index + 1 >= password.Length ||
                !char.IsLowSurrogate(password[++index]))
                return false;
        }
        return true;
    }
}
