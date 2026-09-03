using Microsoft.AspNetCore.Mvc.RazorPages;
using Catchlogr.Web.Services;

namespace Catchlogr.Web.Pages;

/// <summary>Handles public email-confirmation links.</summary>
public sealed class ConfirmEmailModel : PageModel
{
    private readonly IIdentityApiClient _identityApiClient;

    /// <summary>Initializes a new email-confirmation page model.</summary>
    /// <param name="identityApiClient">The Catchlogr Identity API client.</param>
    public ConfirmEmailModel(IIdentityApiClient identityApiClient)
    {
        _identityApiClient = identityApiClient;
    }

    /// <summary>Gets the result displayed by the page.</summary>
    public IdentityActionResult Result { get; private set; } =
        IdentityActionResult.Rejected;

    /// <summary>Confirms the email address represented by the query string.</summary>
    /// <param name="userId">The Identity user identifier.</param>
    /// <param name="code">The one-time confirmation code.</param>
    /// <param name="cancellationToken">Cancels the pending API request.</param>
    public async Task OnGetAsync(
        string? userId,
        string? code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(code))
        {
            Result = IdentityActionResult.Rejected;
            return;
        }

        Result = await _identityApiClient.ConfirmEmailAsync(
            userId,
            code,
            cancellationToken);
    }
}
