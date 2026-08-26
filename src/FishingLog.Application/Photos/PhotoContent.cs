namespace FishingLog.Application.Photos;

/// <summary>
/// Contains an authorized private photo stream and response metadata.
/// </summary>
/// <param name=Stream>The readable photo stream.</param>
/// <param name=ContentType>The validated media type.</param>
public sealed record PhotoContent(Stream Stream, string ContentType);
