namespace Catchlogr.Mobile.Core.Services;

/// <summary>Reports that a device feature is unsupported.</summary>
public sealed class DeviceFeatureNotSupportedException(string message, Exception? innerException = null) : Exception(message, innerException);
