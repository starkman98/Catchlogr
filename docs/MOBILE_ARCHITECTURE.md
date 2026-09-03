# Mobile architecture

Catchlogr.Mobile is a .NET MAUI MVVM client. It treats account-scoped SQLite as
the offline working store and uses `Catchlogr.Sync` to reconcile data with the
authenticated API.

## Boundaries

```text
Pages/XAML
    ↓ bindings and commands
ViewModels
    ├── local repository abstractions ──> SQLite
    ├── application services
    ├── navigation abstraction
    └── ISyncOrchestrator
            ↓
        Catchlogr.Sync
            ├── local repository abstractions
            └── typed API abstractions
                    ↓ authenticated HttpClient
                Catchlogr.Api
```

ViewModels do not access SQLite or construct `HttpClient` directly. Mobile
references Application, Contracts, and Sync, but not Infrastructure.

## Local data and account isolation

`LocalDatabase` manages three SQLite tables:

- `FishingTripLocalEntity`;
- `CatchLocalEntity`; and
- `SyncMetadataEntity`.

Synced rows have a local integer `Id`, nullable server GUID string `ServerId`,
UTC `LastModifiedUtc`, `IsDirty`, and `IsDeleted`. Catches also retain parent
trip identifiers and private-photo synchronization state.

Database files differ by backend configuration. Authentication keys are also
prefixed by backend. Within a backend, the local database and captured photo
paths are isolated by authenticated account. Logout attempts a final sync,
closes the active account database, and clears the session tokens. The account's
offline files remain isolated on the device for the next login.

## Synchronization

`SyncOrchestrator` runs services in dependency order:

1. `FishingTripSyncService` uploads and downloads trips, then retries missing
   server weather for eligible clean trips.
2. `CatchSyncService` uploads and downloads catches and synchronizes private
   photos.

This order guarantees a new catch can obtain its parent's server ID. See
[Sync Strategy](SYNC_STRATEGY.md) for reconciliation and failure behavior.

Sync can be triggered from pull-to-refresh, page appearance, trip details, and
logout preparation. Network failures leave pending local work available for a
later attempt.

## Authentication and HTTP

`AuthenticationService` registers, signs in, refreshes tokens, requests email
confirmation and password-reset messages, and reads the current user.
`SecureTokenStore` keeps access and refresh tokens in platform SecureStorage.

`AuthenticationMessageHandler` attaches bearer tokens to protected API calls
and coordinates token refresh. Typed clients implement the abstractions used by
Sync and the location/photo services. The mobile app contains no PostgreSQL,
LocationIQ, Resend, or storage-provider credentials.

## MVVM conventions

ViewModels inherit `BaseViewModel`, use dependency injection, and expose
CommunityToolkit.Mvvm commands and observable properties. Observable properties
must use the AOT-safe partial-property syntax:

```csharp
public partial class ExampleViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        // Call an injected abstraction.
    }
}
```

`Catchlogr.Mobile.csproj` uses `<LangVersion>preview</LangVersion>` for this
source-generator syntax. Field-based `[ObservableProperty] private T _value;`
must not be introduced.

Date/time picker values are converted at the ViewModel boundary only. Stored
entities and API contracts use UTC; pickers show local time.

## Navigation

`AppRoutes` centralizes route names and `IAppNavigator` isolates ViewModels from
`Shell.Current`. `AppShell` registers routes for:

- login, registration, check-email, forgot-password, and reset-password pages;
- fishing-trip list, details, and add/edit pages; and
- catch list and add/edit pages.

Pages and ViewModels are transient registrations so navigation receives fresh
state. Query parameters carry local integer IDs, never internal database objects.

## Presentation features

- Fishing-trip details show provider weather and calculated moon phase when present.
- Add/edit trip supports device location and server-proxied LocationIQ search.
- Catch editing supports camera/gallery capture and retains a private local copy.
- Catch photos are downloaded through authenticated API endpoints for offline use.
- Busy, refresh, status, and error state are exposed by ViewModels without blocking
  the UI thread.

## Composition root

`MauiProgram.cs` loads exactly one embedded environment configuration, resolves
the platform API URL, configures authenticated typed clients, and registers
database, repository, sync, authentication, navigation, photo, ViewModel, and
page services.

See [Mobile Configuration](MOBILE_CONFIGURATION.md) for the environment matrix.

## Key directories

| Responsibility | Directory |
| --- | --- |
| Configuration and URL resolution | `src/Catchlogr.Mobile/Configuration` |
| SQLite implementation and repositories | `src/Catchlogr.Mobile/Data` |
| Cross-platform sync abstractions/entities/services | `src/Catchlogr.Sync` |
| Authentication, API, navigation, location, and photo clients | `src/Catchlogr.Mobile/Services` |
| Screen state and commands | `src/Catchlogr.Mobile/ViewModels` |
| XAML views | `src/Catchlogr.Mobile/Pages` |
| Weather and moon display formatting | `src/Catchlogr.Mobile/Presentation` |
