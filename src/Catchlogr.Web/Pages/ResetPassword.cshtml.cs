using System.ComponentModel.DataAnnotations;
using Catchlogr.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Catchlogr.Web.Pages;

/// <summary>Handles public password-reset links and form submissions.</summary>
[ValidateAntiForgeryToken]
public sealed class ResetPasswordModel : PageModel
{
    private readonly IIdentityApiClient _identityApiClient;

    /// <summary>Initializes a new password-reset page model.</summary>
    /// <param name="identityApiClient">The Catchlogr Identity API client.</param>
    public ResetPasswordModel(IIdentityApiClient identityApiClient)
    {
        _identityApiClient = identityApiClient;
    }

    /// <summary>Gets or sets the email address carried by the reset link.</summary>
    [BindProperty(SupportsGet = true)]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-time reset code carried by the reset link.</summary>
    [BindProperty(SupportsGet = true)]
    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested replacement password.</summary>
    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the repeated replacement password.</summary>
    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Gets the completed reset result, when one is available.</summary>
    public IdentityActionResult? Result { get; private set; }

    /// <summary>Validates the password-reset link before displaying the form.</summary>
    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Code))
        {
            Result = IdentityActionResult.Rejected;
        }
    }

    /// <summary>Submits the new password to the Identity API.</summary>
    /// <param name="cancellationToken">Cancels the pending API request.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result = await _identityApiClient.ResetPasswordAsync(
            Email,
            Code,
            NewPassword,
            cancellationToken);

        return Page();
    }
}
