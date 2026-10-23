using System.Collections.Frozen;
using Catchlogr.Application.Validators;
using Catchlogr.Contracts.AuthenticationDTOs;
using Microsoft.AspNetCore.Identity;

namespace Catchlogr.Infrastructure.Identity;

/// <summary>Applies the application policy to every Identity password-setting operation.</summary>
public sealed class ApplicationPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly FrozenSet<string> Blocklist = LoadBlocklist();

    /// <inheritdoc/>
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);
        var codes = PasswordPolicy.Validate(password, user.Email,
            password is not null && Blocklist.Contains(password));
        return Task.FromResult(codes.Count == 0 ? IdentityResult.Success :
            IdentityResult.Failed(codes.Select(code => new IdentityError
            {
                Code = code,
                Description = PasswordRequirements.GetErrorMessage(code)!
            }).ToArray()));
    }

    private static FrozenSet<string> LoadBlocklist()
    {
        using var stream = typeof(ApplicationPasswordValidator).Assembly.GetManifestResourceStream(
            "Catchlogr.Infrastructure.Identity.PasswordBlocklist.common-passwords.txt")
            ?? throw new InvalidOperationException("The bundled password blocklist is missing.");
        using var reader = new StreamReader(stream);
        var passwords = new List<string>();
        while (reader.ReadLine() is { } password)
            passwords.Add(password);
        if (passwords.Count == 0)
            throw new InvalidOperationException("The bundled password blocklist is empty.");
        return passwords.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
