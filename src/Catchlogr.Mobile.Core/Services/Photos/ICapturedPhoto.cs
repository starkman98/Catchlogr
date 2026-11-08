namespace Catchlogr.Mobile.Core.Services.Photos;

/// <summary>Provides the name and readable contents of a captured photo.</summary>
public interface ICapturedPhoto
{
    /// <summary>Gets the original photo filename.</summary>
    string FileName { get; }
    /// <summary>Opens the photo; the caller owns the returned stream.</summary>
    Task<Stream> OpenReadAsync();
}
