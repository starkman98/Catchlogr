using System.Text.Json;

namespace Catchlogr.Contracts.AuthenticationDTOs;

/// <summary>Reads only recognized password codes from Identity validation responses.</summary>
public static class PasswordValidationErrors
{
    /// <summary>Returns recognized codes, or an empty list for unrelated or malformed responses.</summary>
    public static IReadOnlyList<string> Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("errors", out var errors) ||
                errors.ValueKind != JsonValueKind.Object)
                return [];

            return errors.EnumerateObject()
                .Select(error => error.Name)
                .Where(code => PasswordRequirements.GetErrorMessage(code) is not null)
                .Distinct()
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
