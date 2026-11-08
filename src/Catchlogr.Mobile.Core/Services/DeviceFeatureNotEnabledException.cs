namespace Catchlogr.Mobile.Core.Services;

/// <summary>Reports that a device feature is disabled.</summary>
public sealed class DeviceFeatureNotEnabledException(string message, Exception? innerException = null) : Exception(message, innerException);
