namespace Catchlogr.Mobile.Core.Services;

/// <summary>Reports that a device permission was denied.</summary>
public sealed class DevicePermissionException(string message, Exception? innerException = null) : Exception(message, innerException);
