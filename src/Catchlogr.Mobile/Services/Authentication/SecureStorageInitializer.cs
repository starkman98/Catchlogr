using Microsoft.Maui.Storage;

namespace Catchlogr.Mobile.Services.Authentication;

/// <summary>
/// Initializes secure storage for the current application installation.
/// </summary>
public static class SecureStorageInitializer
{
    private const string AppInitializedKey = "catchlogr.app_initialized";

    /// <summary>
    /// Clears secure values left by a previous installation when the application
    /// starts for the first time after being installed.
    /// </summary>
    public static void Initialize()
    {
        if (Preferences.Default.ContainsKey(AppInitializedKey))
        {
            return;
        }

        SecureStorage.Default.RemoveAll();
        Preferences.Default.Set(AppInitializedKey, true);
    }
}
