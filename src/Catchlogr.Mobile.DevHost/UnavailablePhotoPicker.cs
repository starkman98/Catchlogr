using Catchlogr.Mobile.Core.Services.Photos;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Explicitly disables camera access in the console host.</summary>
public sealed class UnavailablePhotoPicker : IPhotoPicker
{
    /// <inheritdoc/>
    public bool IsCaptureSupported => false;
    /// <inheritdoc/>
    public Task<ICapturedPhoto?> CapturePhotoAsync() => throw new NotSupportedException("DevHost has no camera.");
}
