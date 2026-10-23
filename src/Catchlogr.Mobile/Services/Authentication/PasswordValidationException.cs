using System.Net;
using Catchlogr.Contracts.AuthenticationDTOs;

namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>Reports recognized password-policy errors without exposing server response text.</summary>
public sealed class PasswordValidationException : HttpRequestException
{
    /// <summary>Creates a validation error from recognized Identity error codes.</summary>
    public PasswordValidationException(IReadOnlyList<string> codes)
        : base(string.Join(" ", codes.Select(PasswordRequirements.GetErrorMessage).OfType<string>()),
            null, HttpStatusCode.BadRequest)
    {
    }
}
