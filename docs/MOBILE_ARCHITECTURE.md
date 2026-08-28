# Mobile Architecture — Sync Flow, ViewModels & Navigation

This document covers the three most important architectural areas of `Catchlogr.Mobile`:
1. [Offline-First Sync Flow](#offline-first-sync-flow)
2. [ViewModel Patterns](#viewmodel-patterns)
3. [Shell Navigation & Routing](#shell-navigation--routing)

---

## Offline-First Sync Flow

### Design Goals
- The mobile app works fully offline; all reads and writes go to local SQLite first.
- A manual (or triggered) sync reconciles local changes with the server.
- Conflict resolution is **last-write-wins** based on `LastModifiedUtc`.

### Local Entity Schema

Every local entity that participates in sync carries these extra columns:

| Column | Type | Purpose |
|---|---|---|
| `Id` | `int` (PK, auto-increment) | Local-only surrogate key |
| `ServerId` | `string?` | GUID from server; `null` until first upload |
| `LastModifiedUtc` | `DateTime` | Timestamp of last change (UTC) |
| `IsDirty` | `bool` | `true` when the row has unsynchronised local changes |
| `IsDeleted` | `bool` | Soft-delete flag; propagated to server on next sync |

```csharp
// FishingTripLocalEntity.cs
[PrimaryKey, AutoIncrement]
public int Id { get; set; }

[Indexed]
public string? ServerId { get; set; }   // server GUID stored as string

public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
public bool IsDirty { get; set; } = true;   // new rows start dirty
public bool IsDeleted { get; set; }
```

### Sync Cursor

`SyncMetadataEntity` stores the timestamp of the last successful download per entity type:

```csharp
// SyncMetadataEntity.cs
[PrimaryKey]
public string EntityType { get; set; } = string.Empty;  // e.g. "FishingTrip"
public DateTime LastSyncedAtUtc { get; set; }
```

The cursor value is sent to the server as `?modifiedSince=<UTC ISO-8601>` so only changed records are returned.

### Sync Algorithm — Step by Step

```
SyncAsync()
│
├─ 1. UPLOAD DIRTY ROWS
│   ├─ GetDirtyAsync() → local rows where IsDirty = true
│   └─ For each dirty row:
│       ├─ If ServerId is null → POST /api/fishing-trips   (creates on server)
│       │   └─ On 201: store returned ServerId, IsDirty = false, update LastModifiedUtc
│       ├─ If IsDeleted → DELETE /api/fishing-trips/{serverId}
│       │   └─ On 204 or 404: remove row from local DB
│       └─ Else → PUT /api/fishing-trips/{serverId}
│           └─ On 200: IsDirty = false, update LastModifiedUtc
│
└─ 2. DOWNLOAD REMOTE CHANGES
    ├─ Read cursor: GetLastSyncedAtAsync("FishingTrip")
    ├─ GET /api/fishing-trips?modifiedSince=<cursor>
    └─ For each returned DTO:
        ├─ GetByServerIdAsync(serverId)
        ├─ If not found locally → insert (SaveFromServerAsync, IsDirty = false)
        └─ If found → last-write-wins:
            ├─ Server.LastModifiedUtc >= Local.LastModifiedUtc → overwrite
            └─ Local is newer → skip (local wins)
    └─ Update cursor: SaveLastSyncedAtAsync("FishingTrip", utcNow)
```

### Key Interfaces

```csharp
// Services/IFishingTripSyncService.cs
Task SyncAsync(CancellationToken ct = default);

// Data/Repositories/IFishingTripLocalRepository.cs
Task<List<FishingTripLocalEntity>> GetDirtyAsync(CancellationToken ct = default);
Task MarkAsSyncedAsync(int localId, string serverId, DateTime serverTimestamp, CancellationToken ct = default);
Task<FishingTripLocalEntity?> GetByServerIdAsync(string serverId, CancellationToken ct = default);
Task SaveFromServerAsync(FishingTripLocalEntity entity, CancellationToken ct = default);

// Data/Repositories/ISyncMetadataRepository.cs
Task<DateTime?> GetLastSyncedAtAsync(string entityType, CancellationToken ct = default);
Task SaveLastSyncedAtAsync(string entityType, DateTime utc, CancellationToken ct = default);
```

### API Endpoint — Sync-Friendly GET

The server exposes an optional `modifiedSince` filter so the mobile client downloads only the delta:

```
GET /api/fishing-trips?modifiedSince=2025-01-01T00:00:00Z
```

This is backed by an EF Core index on `LastModified` in `FishingTripConfiguration`.

### Platform API URL Resolution

`PlatformApiUrl.Resolve(configuredUrl)` picks the correct base URL at runtime (DEBUG only):

| Target | URL used |
|---|---|
| Android Emulator | `http://10.0.2.2:5001` |
| iOS Simulator / macOS | `https://localhost:5001` |
| Windows (WinUI) | `https://localhost:5001` |
| Physical device | `configuredUrl` from `appsettings.json` |

---

## ViewModel Patterns

### BaseViewModel

All ViewModels inherit `BaseViewModel` which extends CommunityToolkit.Mvvm `ObservableObject`:

```csharp
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;
}
```

### Code-gen Attributes (CommunityToolkit.Mvvm)

| Attribute | What it generates |
|---|---|
| `[ObservableProperty]` on a `private` field | A public property with `INotifyPropertyChanged` wiring |
| `[NotifyPropertyChangedFor(nameof(X))]` | Also raises `PropertyChanged` for `X` when this property changes |
| `[RelayCommand]` on a method | A public `ICommand` property (e.g. `LoadTripsCommand`) |
| `[QueryProperty(nameof(Prop), "key")]` | Populates `Prop` when Shell navigates with `?key=value` |

> **Note:** Classes using these attributes must be `partial` and the field name must start with `_`.

### FishingTripsViewModel (List Page)

```
Commands
├─ LoadTripsCommand    → Calls _localRepo.GetAllAsync() → populates Trips collection
├─ SyncCommand         → Calls _syncService.SyncAsync() → then reloads Trips
│                         (HttpRequestException swallowed silently — offline is OK)
├─ AddTripCommand      → GoToAsync("AddEditFishingTripPage")
├─ SelectTripCommand   → GoToAsync($"AddEditFishingTripPage?localId={trip.Id}")
└─ DeleteTripCommand   → Soft-deletes locally (IsDirty = true, IsDeleted = true)
                         → reloads list
```

### AddEditFishingTripViewModel (Form Page)

```
Initialisation
└─ [QueryProperty(nameof(LocalIdQuery), "localId")]
    └─ setter parses int → calls LoadTripAsync(id) in edit mode
       (no query param → new trip / add mode)

DateTime handling
├─ StartDate  (DateTime)  — bound to DatePicker
├─ StartTimeOfDay (TimeSpan) — bound to TimePicker
└─ SaveAsync combines: StartDate.Date + StartTimeOfDay → StartTime (UTC)

Commands
├─ SaveCommand  → validates → AddAsync (new) or UpdateAsync (existing)
│               → IsDirty = true, LastModifiedUtc = UtcNow
│               → GoToAsync("..")
└─ CancelCommand → GoToAsync("..")
```

---

## Shell Navigation & Routing

### Route Registration

Routes that are **not** in the Shell XAML hierarchy must be registered in the `AppShell` constructor:

```csharp
// AppShell.xaml.cs
public AppShell()
{
    InitializeComponent();
    Routing.RegisterRoute("AddEditFishingTripPage", typeof(AddEditFishingTripPage));
}
```

### Shell XAML Structure

```
AppShell
└─ ShellContent  (FishingTripsPage — root / tab 1)
```

`FishingTripsPage` is the root. All other pages are pushed modally or on the navigation stack via `GoToAsync`.

### Navigation Reference

| Action | Code |
|---|---|
| Push Add page | `await Shell.Current.GoToAsync("AddEditFishingTripPage")` |
| Push Edit page | `await Shell.Current.GoToAsync($"AddEditFishingTripPage?localId={id}")` |
| Pop / back | `await Shell.Current.GoToAsync("..")` |

### Receiving Navigation Parameters

```csharp
// AddEditFishingTripViewModel.cs
[QueryProperty(nameof(LocalIdQuery), "localId")]
public partial class AddEditFishingTripViewModel : BaseViewModel
{
    public string LocalIdQuery
    {
        set
        {
            if (int.TryParse(value, out var id))
                LoadTripAsync(id).SafeFireAndForget(); // or just call without await in setter
        }
    }
}
```

### DI Registration (MauiProgram.cs)

```csharp
// Data
builder.Services.AddSingleton<ILocalDatabase, LocalDatabase>();
builder.Services.AddTransient<IFishingTripLocalRepository, FishingTripLocalRepository>();
builder.Services.AddTransient<ISyncMetadataRepository, SyncMetadataRepository>();

// HTTP + sync
builder.Services.AddHttpClient<IFishingTripApiClient, FishingTripApiClient>(client =>
    client.BaseAddress = new Uri(PlatformApiUrl.Resolve(configuredUrl)));
builder.Services.AddTransient<IFishingTripSyncService, FishingTripSyncService>();

// UI (Transient so each navigation push gets a fresh VM)
builder.Services.AddTransient<FishingTripsViewModel>();
builder.Services.AddTransient<AddEditFishingTripViewModel>();
builder.Services.AddTransient<FishingTripsPage>();
builder.Services.AddTransient<AddEditFishingTripPage>();
```

> **Why transient for ViewModels and Pages?**  
> Shell re-uses page instances by default unless you `AddTransient` them. Transient registration ensures a fresh ViewModel (and cleared state) on every navigation push.

---

## File Map

```
src/Catchlogr.Mobile/
├─ Configuration/
│   ├─ AppSettings.cs
│   └─ PlatformApiUrl.cs          ← platform URL resolver
├─ Data/
│   ├─ ILocalDatabase.cs
│   ├─ LocalDatabase.cs           ← SQLiteAsyncConnection singleton
│   ├─ SyncEntityType.cs          ← string constants ("FishingTrip", …)
│   ├─ Entities/
│   │   ├─ FishingTripLocalEntity.cs
│   │   └─ SyncMetadataEntity.cs
│   └─ Repositories/
│       ├─ IFishingTripLocalRepository.cs
│       ├─ FishingTripLocalRepository.cs
│       ├─ ISyncMetadataRepository.cs
│       └─ SyncMetadataRepository.cs
├─ Services/
│   ├─ IFishingTripApiClient.cs
│   ├─ FishingTripApiClient.cs    ← typed HttpClient
│   ├─ IFishingTripSyncService.cs
│   └─ FishingTripSyncService.cs  ← two-way sync orchestrator
├─ ViewModels/
│   ├─ BaseViewModel.cs
│   ├─ FishingTripsViewModel.cs   ← list page VM
│   └─ AddEditFishingTripViewModel.cs
├─ Pages/
│   ├─ FishingTripsPage.xaml(.cs)
│   └─ AddEditFishingTripPage.xaml(.cs)
├─ AppShell.xaml(.cs)             ← route registration
├─ App.xaml.cs                    ← CreateWindow() override
└─ MauiProgram.cs                 ← DI composition root
```
