# Mobile architecture

Catchlogr.Mobile is the .NET MAUI shell for `Catchlogr.Mobile.Core`, a plain
`net10.0` library. Core owns ViewModels, presentation, configuration validation,
authentication, HTTP clients, and SQLite repositories. It treats account-scoped
SQLite as the offline working store and uses `Catchlogr.Sync` to reconcile data
with the authenticated API.

## Boundaries

```text
Catchlogr.Mobile: Pages/XAML and platform adapters
    ↓ bindings and commands
Catchlogr.Mobile.Core: ViewModels
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

ViewModels do not access SQLite or construct `HttpClient` directly. Core
references Application, Contracts, and Sync, but not Infrastructure. The MAUI
shell references Core; Core and Sync have no MAUI dependencies.

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
`SecureTokenStore` keeps access and refresh tokens through `ISecretStorage`;
`MauiSecretStorage` delegates to platform SecureStorage.
Registration and password reset show the server's
[password-policy guidance](IDENTITY_EMAIL.md#password-policy) and recognized policy
errors. Password entry offers visibility controls; the API owns policy enforcement.

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

`Catchlogr.Mobile.Core.csproj` uses `<LangVersion>preview</LangVersion>` for this
source-generator syntax. Field-based `[ObservableProperty] private T _value;`
must not be introduced.

Date/time picker values are converted at the ViewModel boundary only. Stored
entities and API contracts use UTC; pickers show local time:

```csharp
// Load UTC into local pickers.
var local = entity.StartTime.ToLocalTime();
StartDate = local.Date;
StartTimeOfDay = local.TimeOfDay;

// Save local picker values as UTC.
private static DateTime ToUtc(DateTime date, TimeSpan time)
    => DateTime.SpecifyKind(date.Date + time, DateTimeKind.Local).ToUniversalTime();
```

## Navigation

`AppRoutes` centralizes route names and `IAppNavigator` isolates ViewModels from
`Shell.Current` for all ViewModel navigation. `IAppInteraction` handles alerts,
UI dispatch, and external links. The MAUI pages implement `IQueryAttributable`
and forward Shell query values to Core ViewModels; Core contains no Shell
interfaces or attributes. AppShell.xaml defines
login and trip-list Shell content. AppShell.xaml.cs registers relative routes
for registration/check-email/password flows, trip details/add-edit, and catch
add-edit. The catch list is embedded in trip details, not a separate Shell page.

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

`MauiProgram.cs` loads exactly one embedded environment configuration and resolves
the platform API URL. `AddMobileCore` registers the shared database, repositories,
authentication, HTTP clients, synchronization, and ViewModels. MAUI and DevHost
both call it and supply their own platform adapters. MAUI also registers pages. The host passes its assembly and selected environment to
`AppSettings.Load`, and supplies `FileSystem.AppDataDirectory` as the default
`DatabaseSettings.RootDirectory`. Other hosts must configure a storage root.

`IConnectivityService`, `IDeviceLocationService`, `IPhotoPicker`, and
`ISecretStorage` isolate device APIs. Camera capture and secure-storage calls
remain in MAUI adapters; photo file handling and backend-scoped token logic
remain in Core so their account-isolation tests run on every host. The location
adapter translates MAUI permission/feature exceptions into portable exceptions,
preserving the existing messages displayed by the ViewModel.

See [Mobile Configuration](MOBILE_CONFIGURATION.md) for the environment matrix.

## Key directories

| Responsibility | Directory |
| --- | --- |
| Configuration and URL resolution | `src/Catchlogr.Mobile.Core/Configuration` |
| SQLite implementation and repositories | `src/Catchlogr.Mobile.Core/Data` |
| Cross-platform sync abstractions/entities/services | `src/Catchlogr.Sync` |
| Authentication, API clients, device contracts, and photo file handling | `src/Catchlogr.Mobile.Core/Services` |
| Device, secure storage, Shell, and dialog adapters | `src/Catchlogr.Mobile/Services` |
| Screen state and commands | `src/Catchlogr.Mobile.Core/ViewModels` |
| XAML views | `src/Catchlogr.Mobile/Pages` |
| Weather and moon display formatting | `src/Catchlogr.Mobile.Core/Presentation` |

## Portable development and remaining risks

Mobile logic tests build and run on Linux, macOS, or Windows with the .NET SDK;
no MAUI workload is needed. See [Validation](VALIDATION.md#commands) for commands.
This removes the Windows runner dependency for those tests and lets server-side
agents exercise the same implementation used by the app. No CI speedup is
claimed without runner measurements.

The extraction adds host interfaces and composition wiring that must stay in
step. Portable tests cover session cleanup, backend/account storage separation,
photo file ownership, ViewModel interactions, and configuration selection; they
do not establish XAML binding correctness, Shell query delivery, UI dispatch on
a real device, secure-storage encryption, or camera/GPS behavior. Build and
exercise affected platform flows when changing those adapters. The full solution
still includes MAUI and needs its platform toolchains; build Core or the test
project directly on a server. The console DevHost below exercises the portable pipeline.

For actual Android screen interaction, see
[Android emulator and Appium verification](VALIDATION.md#android-emulator-and-appium-verification).
An agent can inspect native controls, enter text, tap buttons, and capture
screenshots through Appium; the console DevHost does not render these screens.

## Console DevHost

`Catchlogr.Mobile.DevHost` is a plain `net10.0` executable for a local mobile/API
smoke test on Linux, macOS, or Windows. It uses the shared Core registration,
real Identity bearer authentication, temporary SQLite, and the real Sync services.
It does not start Docker, start the API, register accounts, send emails, bypass
confirmation, or apply migrations.

### Run against your local API

1. Follow [local setup](../README.md#local-setup), including the API's development
   configuration and database. `compose.local.yml` supplies **PostgreSQL only**;
   it does not contain an API service. Use a dedicated development database and
   account: a loopback API can still be configured to use a shared database.
2. Start the API separately, for example:

   ```sh
   dotnet run --project src/Catchlogr.Api --launch-profile http
   ```

3. Use an existing account, with email confirmed when required by the local API.
   Run the scenario and answer its email and hidden-password prompts:

   ```sh
   dotnet run --project src/Catchlogr.Mobile.DevHost -- --name "DevHost smoke trip"
   ```

The default API is `http://127.0.0.1:5001/`. To use the HTTPS launch endpoint:

```sh
dotnet run --project src/Catchlogr.Mobile.DevHost -- --api https://localhost:7160 --name "HTTPS smoke trip"
```

Only loopback HTTP/HTTPS origins are accepted; credentials, paths, queries,
fragments, and remote/LAN hosts are rejected. HTTPS uses normal certificate
validation; the host never disables trust checks. HTTP redirects are disabled,
including redirects from login requests.

Automation can supply `CATCHLOGR_DEVHOST_EMAIL` and `CATCHLOGR_DEVHOST_PASSWORD`
through the process environment. Supply secrets through your runner's secret
facility or an interactive prompt, not literal shell commands or command-line
arguments. DevHost never accepts password arguments, prints tokens, enables HTTP
body logging, or persists credentials. `--help` prints usage without connecting.

### Scenario and results

The host logs in with Core's `AuthenticationService`, activates the account's
SQLite database, saves a new trip through `AddEditFishingTripViewModel`, runs
`ISyncOrchestrator`, and reads the trip back through the authenticated API.
Success requires the local record to have a server GUID, be clean, and match the
remote trip name. This catches upload failures that Sync deliberately retains
for retry instead of propagating.

Each invocation creates one trip. A generated unique name is used unless `--name`
is supplied. **The server trip remains**, even after local cleanup or a later
failure; a failed or cancelled run can already have uploaded it. Re-running is
not idempotent. Inspect or remove smoke trips through the normal app/API.

The host uses memory-only secret storage and a fresh private temporary directory
for SQLite and any downloaded account files. Normal exit, scenario failure, and
handled cancellation close SQLite, clear tokens, and delete the run's local
files. A forced kill/crash can leave a `Catchlogr.DevHost-*` directory under the
system temp directory; no credentials are written there. Cleanup failures cause
a nonzero exit. Pending local changes are discarded with this disposable session,
unlike the MAUI app's persistent offline storage.

Location is absent by default, so weather enrichment is not required. Add
`--with-location` to capture a fixed synthetic location through the device
adapter; this may invoke the API's configured weather provider. The camera is
unavailable, navigation is a no-op, and UI callbacks execute inline. Unexpected
alerts fail the scenario rather than silently accepting validation errors.

Exit codes: `0` verified upload and local cleanup; `1` API, scenario, or cleanup
failure; `2` invalid arguments or missing credentials; `130` handled cancellation.
Output deliberately omits exception bodies, private paths, and credentials. For
login rejection, check email confirmation and credentials; for HTTP failures,
check API startup, port, database configuration, and HTTPS trust. Local test
coverage is described in [Validation](VALIDATION.md#devhost-validation).

### Gains and risks

- Gains: agents can exercise the mobile-to-API flow without MAUI or an emulator;
  shared registration reduces wiring drift; temporary sessions make smoke runs
  independent of a developer's mobile data.
- Risks: the scenario writes server data and may download existing account data;
  use a dedicated local account/database. Temporary cleanup cannot undo an API
  write. The optional location path can use a live external weather service.
- Limits: console adapters do not verify Shell, XAML, real UI threading, camera,
  GPS, secure-storage encryption, database upgrades, or interruption recovery.
  The isolated API tests use EF InMemory, so PostgreSQL behavior still needs a
  separate disposable local smoke run.
