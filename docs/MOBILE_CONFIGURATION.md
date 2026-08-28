# Mobile App Configuration

## Overview
The Catchlogr mobile app uses embedded JSON configuration files similar to ASP.NET Core apps.

## Configuration Files

### `appsettings.json`
Default configuration used in **Release** builds.

```json
{
  "Api": {
    "BaseUrl": "https://api.catchlogr.com",  // Production API URL
    "Timeout": 30
  },
  "Sync": {
    "AutoSyncOnStartup": true,
    "SyncIntervalMinutes": 15
  },
  "Database": {
    "FileName": "catchlogr.db3"
  }
}
```

### `appsettings.Development.json`
Configuration overrides used in **Debug** builds.

```json
{
  "Api": {
    "BaseUrl": "https://localhost:5001",  // Local development API
    "Timeout": 60
  },
  "Sync": {
    "AutoSyncOnStartup": false,
    "SyncIntervalMinutes": 5
  },
  "Database": {
    "FileName": "catchlogr_dev.db3"
  },
  "Logging": {
    "LogLevel": "Debug"
  }
}
```

## How It Works

1. **Files are embedded** as resources in the compiled app (see `.csproj`)
2. **AppSettings.Load()** reads the JSON at runtime:
   - In **Debug** builds: Loads `appsettings.Development.json` first, falls back to `appsettings.json`
   - In **Release** builds: Loads `appsettings.json`
3. **Settings are registered** in `MauiProgram.cs` for dependency injection

## Using Settings in Your Code

### Option 1: Inject the whole AppSettings object
```csharp
public class MyViewModel
{
    private readonly AppSettings _settings;

    public MyViewModel(AppSettings settings)
    {
        _settings = settings;
        var apiUrl = _settings.Api.BaseUrl;
    }
}
```

### Option 2: Inject specific settings sections
```csharp
public class MyApiClient
{
    private readonly ApiSettings _apiSettings;

    public MyApiClient(ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;
        var client = new HttpClient
        {
            BaseAddress = new Uri(_apiSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_apiSettings.Timeout)
        };
    }
}
```

## Changing API URL

### For Local Development
Edit `src/Catchlogr.Mobile/appsettings.Development.json`:
```json
{
  "Api": {
    "BaseUrl": "https://localhost:5001"  // Change to your local API URL
  }
}
```

### For Different Environments (Staging, Production)
You can create additional files:
- `appsettings.Staging.json`
- `appsettings.Production.json`

And modify the `AppSettings.Load()` method to check for environment variables or build configurations.

## Important Notes

- ⚠️ **Don't put secrets in these files** - They are embedded in the app binary
- ✅ For sensitive data (API keys, tokens), use:
  - Platform-specific secure storage (e.g., `SecureStorage.SetAsync()`)
  - Environment variables
  - Remote configuration (e.g., Azure App Configuration)
- 🔄 Changes to JSON files require **rebuilding the app** (they're embedded at compile time)

## Database Location

The SQLite database is stored at:
```csharp
var dbPath = appSettings.Database.FullPath;
// Example: /data/user/0/com.companyname.catchlogr.mobile/files/catchlogr_dev.db3
```

This path is automatically determined by MAUI's `FileSystem.AppDataDirectory`.
