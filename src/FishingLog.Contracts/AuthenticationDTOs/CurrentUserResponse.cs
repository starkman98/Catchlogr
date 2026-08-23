namespace FishingLog.Contracts.AuthenticationDTOs;

/// <summary>
/// Describes the currently authenticated FishingLog account.
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Email">The account email address.</param>
/// <param name="CreatedUtc">When the account was created, in UTC.</param>
/// <param name="DisplayName">The accounts optional displayname.</param>
public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    DateTime CreatedUtc,
    string? DisplayName);
