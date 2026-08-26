namespace FishingLog.Contracts.PhotoDTOs;

/// <summary>
/// Describes a photo stored by the FishingLog API.
/// </summary>
public sealed record PhotoUploadResponse(Guid Id, string Url);
