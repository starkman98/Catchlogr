# Sync strategy

Catchlogr uses explicit, offline-first synchronization. User edits are committed
to account-scoped SQLite first. The API and PostgreSQL are the cross-device
system of record once changes synchronize.

## Components and order

`ISyncOrchestrator.SyncAsync` executes:

1. `FishingTripSyncService.SyncAsync`;
2. `CatchSyncService.SyncAsync`.

Trips run first because catches require a parent trip server ID. Both services
live in the platform-neutral `Catchlogr.Sync` project; Mobile supplies SQLite
repositories and authenticated typed API clients through dependency injection.

## Local metadata

Every synchronized row includes:

| Field | Purpose |
| --- | --- |
| `Id` | Local auto-increment primary key; never sent to the API |
| `ServerId` | Server GUID stored as a string; null before first upload |
| `LastModifiedUtc` | UTC conflict timestamp |
| `IsDirty` | Local work still needs upload |
| `IsDeleted` | Local soft deletion still needs propagation |

`SyncMetadataEntity` stores an independent cursor for `FishingTrip` and `Catch`.
The first download starts at `2000-01-01T00:00:00Z`.

## Reconciliation algorithm

Each entity service uploads dirty rows before downloading remote changes.

### Upload

- A row without `ServerId` is created on the API.
- An existing non-deleted row is updated on the API.
- A deleted row is deleted remotely and then permanently removed locally.
- A newly created catch waits until its parent trip has a server ID.
- Successful responses are applied in full, including server timestamps and
  server-owned enrichment fields.
- Network or timeout failures are logged and leave the row pending for a later sync.

### Download

- The client calls the entity collection endpoint with `modifiedSince=<cursor>`.
- A missing local row is inserted clean.
- A dirty local row newer than the response is retained.
- Otherwise the server response replaces the local synchronized fields.
- When results exist, the cursor advances to the maximum server
  `LastModified` value in that response. It does not advance to the device clock.
- A failed download leaves the cursor unchanged.

The current conflict rule is last-write-wins by UTC modification timestamp.
Clock skew and whole-record overwrites remain known trade-offs.

## Weather retry

Before uploading trips, trip sync captures eligible clean trips that already have
a server ID and coordinates but no provider-weather sample. After normal upload
and download, it revalidates those candidates and calls:

```http
POST /api/fishing-trips/{id}/weather/retry
```

Capturing candidates before upload avoids retrying a newly uploaded trip during
the same sync. Provider/network failure does not fail the completed reconciliation;
the trip stays eligible next time.

## Private-photo synchronization

Catch photo state includes a private local path, the current server photo URL,
an upload-pending flag, and an optional old URL pending deletion.

- New catches are created first so an upload has a catch server ID.
- Pending files upload to `POST /api/catches/{catchId}/photos`.
- The returned authenticated photo URL is persisted before the catch update so a
  retry can reuse it.
- Remote photos are downloaded through the authenticated API for offline display.
- Replaced or deleted photos are removed after the related catch synchronization
  succeeds.

## API endpoints used

```http
GET    /api/fishing-trips?modifiedSince=<utc>
POST   /api/fishing-trips
PUT    /api/fishing-trips/{id}
DELETE /api/fishing-trips/{id}

GET    /api/catches?modifiedSince=<utc>
POST   /api/fishing-trips/{tripId}/catches
PUT    /api/catches/{id}
DELETE /api/catches/{id}

POST   /api/catches/{catchId}/photos
GET    /api/photos/{photoId}
DELETE /api/photos/{photoId}
```

All listed endpoints require bearer authentication and are scoped to the current
user.

## Triggers

Full synchronization currently occurs from:

- pull-to-refresh on the trip list;
- automatic trip-list refresh on page appearance;
- refresh on the trip-details page; and
- logout preparation, when pending work exists and connectivity permits.

## Known limitations

- Conflict resolution is whole-record last-write-wins, without a conflict UI.
- Cursors are timestamps rather than opaque server-issued tokens.
- Deletions are permanent after successful propagation; there is no recycle bin.
- The API hard-deletes rows and exposes no deletion tombstones, so a deletion
  made on one client is not currently discoverable by another client that
  already cached the row.
- Upload loops continue past per-record network failures, so a single sync can
  partially succeed.
- There is no general-purpose scheduled background sync service.
