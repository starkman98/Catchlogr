namespace Catchlogr.Contracts.PhotoDTOs;

/// <summary>
/// Describes a photo stored by the Catchlogr API.
/// </summary>
public sealed record PhotoUploadResponse(Guid Id, string Url);
