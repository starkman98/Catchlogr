namespace Catchlogr.Web.Services;

/// <summary>Describes the result of a public Identity account action.</summary>
public enum IdentityActionResult
{
    /// <summary>The action completed successfully.</summary>
    Succeeded,

    /// <summary>The API rejected the submitted account action.</summary>
    Rejected,

    /// <summary>The Identity API could not currently process the action.</summary>
    ServiceUnavailable
}
