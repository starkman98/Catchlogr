namespace Catchlogr.Mobile.Core.Services.Photos;

/// <summary>Provides access to the host camera without platform types.</summary>
public interface IPhotoPicker
{
    /// <summary>Gets whether the host can capture a photo.</summary>
    bool IsCaptureSupported { get; }
    /// <summary>Captures a photo, or returns null if cancelled.</summary>
    Task<ICapturedPhoto?> CapturePhotoAsync();
}
