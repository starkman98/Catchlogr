namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>Describes the password policy and its stable API validation codes.</summary>
public static class PasswordRequirements
{
    /// <summary>The minimum number of Unicode code points in a new password.</summary>
    public const int MinimumLength = 15;

    /// <summary>The maximum number of Unicode code points in a new password.</summary>
    public const int MaximumLength = 128;

    /// <summary>Guidance shown when choosing a new password.</summary>
    public const string Guidance = "Use 15–128 characters. Spaces and Unicode are welcome. " +
        "No uppercase, number, or symbol is required. Avoid common passwords and account details.";

    /// <summary>The error code for a password outside the supported length range.</summary>
    public const string InvalidLength = "PasswordInvalidLength";

    /// <summary>The error code for a common or compromised password.</summary>
    public const string Blocklisted = "PasswordBlocklisted";

    /// <summary>The error code for a password derived from the service or account.</summary>
    public const string Predictable = "PasswordPredictable";

    /// <summary>Maps a recognized API code to safe guidance, without displaying server text.</summary>
    public static string? GetErrorMessage(string code) => code switch
    {
        InvalidLength => "Use a password with 15–128 characters.",
        Blocklisted => "This password is commonly used or known to be compromised. Choose a different passphrase.",
        Predictable => "Choose a password that is not based on Catchlogr or your email address.",
        _ => null
    };
}
