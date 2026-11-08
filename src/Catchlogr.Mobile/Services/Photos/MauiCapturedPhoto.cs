using Catchlogr.Mobile.Core.Services.Photos;

namespace Catchlogr.Mobile.Services.Photos;

/// <summary>Adapts a MAUI file result to a portable captured photo.</summary>
internal sealed class MauiCapturedPhoto(FileResult result) : ICapturedPhoto
{
    /// <inheritdoc/>
    public string FileName => result.FileName;
    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync() => result.OpenReadAsync();
}
