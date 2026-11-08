using Catchlogr.Mobile.Core.Services.Photos;

using Microsoft.Maui.Media;

namespace Catchlogr.Mobile.Services.Photos;

/// <summary>Captures photos through the MAUI camera picker.</summary>
public sealed class MauiPhotoPicker : IPhotoPicker
{
    /// <inheritdoc/>
    public bool IsCaptureSupported => MediaPicker.Default.IsCaptureSupported;
    /// <inheritdoc/>
    public async Task<ICapturedPhoto?> CapturePhotoAsync()
    {
        var result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Photograph catch"
        });
        return result is null ? null : new MauiCapturedPhoto(result);
    }
}
