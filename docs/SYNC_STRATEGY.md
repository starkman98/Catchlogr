# Sync Strategy

Catchlogr uses an **offline-first, explicit sync** model. The mobile app stores all data locally in SQLite and syncs with the server API when connectivity is available.

---

## Core Concepts

| Concept | Description |
|---|---|
| **Dirty flag** | `IsDirty = true` on a local record means it has unsaved changes that need to be uploaded |
| **Sync cursor** | `LastSyncUtc` stored in `SyncMetadata` — the timestamp of the last successful download |
| **ServerId** | The server-assigned GUID stored on the local entity after first upload |
| **Last-write-wins** | Conflict resolution strategy: the record with the newest `LastModified` timestamp wins |

---

## Local Database Schema

Every synced entity has these extra columns in SQLite:

```
Id              int       Local auto-increment primary key
ServerId        string?   GUID from server (null until first upload)
LastModifiedUtc DateTime  UTC timestamp of last change (local or server)
IsDirty         bool      True = has local changes not yet uploaded
IsDeleted       bool      True = soft-deleted, pending server delete
```

The `SyncMetadata` table tracks one row per entity type:

```
EntityType      string    e.g. "FishingTrip"
LastSyncUtc     DateTime? UTC timestamp of last successful download (null = never synced)
```

---

## Sync Algorithm

`FishingTripSyncService.SyncAsync()` runs in two sequential phases:

### Phase 1 — Upload (Local → Server)

1. Query local DB for all records where `IsDirty = true`
2. For each dirty record:
   - **New record** (`ServerId == null`): call `POST /api/fishing-trips` → save the returned `ServerId` and clear `IsDirty`
   - **Updated record** (`ServerId != null`, `IsDeleted = false`): call `PUT /api/fishing-trips/{id}` → clear `IsDirty`
   - **Deleted record** (`IsDeleted = true`): call `DELETE /api/fishing-trips/{id}` → remove the local row permanently
3. Network errors are caught per-record and logged — the record stays dirty and will retry on the next sync

### Phase 2 — Download (Server → Local)

1. Read `LastSyncUtc` from `SyncMetadata` for `FishingTrip` (defaults to `2000-01-01` on first sync)
2. Call `GET /api/fishing-trips?modifiedSince={LastSyncUtc}` to fetch only incremental changes
3. For each remote record received:
   - **Not in local DB**: insert as a clean record (`IsDirty = false`)
   - **In local DB, local is dirty AND newer**: skip — local wins (the upload phase already queued it)
   - **In local DB, server is newer OR local is clean**: overwrite local with server data, clear `IsDirty`
4. After all records are applied, advance `LastSyncUtc` to `DateTime.UtcNow`

---

## Conflict Resolution

Strategy: **last-write-wins** based on `LastModified` timestamp.

```
Local dirty AND Local.LastModifiedUtc > Remote.LastModified  →  Local wins (skip download)
Server newer OR Local is clean                                →  Server wins (overwrite local)
```

This is the simplest viable strategy for an MVP. The trade-off is that if two devices edit the same trip while offline, the device that syncs last will overwrite the other. For a personal fishing log this is acceptable.

> **Future improvement**: Per-field merging or a conflict notification UI (see roadmap Phase 5+).

---

## API Endpoint Used

```
GET /api/fishing-trips?modifiedSince=2024-06-01T00:00:00Z
```

Returns only trips whose `LastModified` is after the given UTC timestamp. This keeps sync payloads small as the dataset grows.

---

## Error Handling

| Scenario | Behaviour |
|---|---|
| Network unavailable during upload | Per-record catch — record stays dirty, logged as warning, retried next sync |
| Network unavailable during download | Entire download phase aborted gracefully, cursor not advanced |
| Server returns 404 on update | Treated as unexpected — logged, record stays dirty |
| Timeout | Caught as `TaskCanceledException`, logged, sync continues where possible |

---

## Triggering a Sync

Sync is triggered:
- Manually via the **Sync button** in the `FishingTripsPage` toolbar
- On **pull-to-refresh** in the trips list
- Automatically on app resume (planned — Phase 3)

---

## Known Limitations (MVP)

- No per-field conflict merging — last-write-wins only
- Sync cursor is a single timestamp per entity type — no per-record cursors
- Deletions are permanent after sync; no recycle bin
- `FishingTripSyncService` lives in `Catchlogr.Mobile` — see roadmap for planned refactor into a testable class library
