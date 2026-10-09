# Sync

Applies to this directory and descendants, together with the root instructions.

- Keep this project platform-neutral: Contracts references are allowed; MAUI,
  SQLite implementations, and server infrastructure are not.
- Preserve upload-before-download and trip-before-catch ordering. See
  [SyncOrchestrator](Services/SyncOrchestrator.cs) and the entity sync services.
- Preserve dirty-record conflict handling and pending photo upload/deletion
  state across failures. Review retry behavior before changing state transitions.
- Preserve backend/account isolation through the repository abstractions and
  their Mobile implementations; cursor state must remain in the correct scope.
- Read [Sync strategy](../../docs/SYNC_STRATEGY.md) before changing cursors,
  conflict resolution, deletion, or photos. Current reconciliation is not atomic
  with cursor advancement, and remote hard deletes do not propagate tombstones.
  Do not describe either guarantee as implemented.

Start in Services for reconciliation, Entities for local state, and Abstractions
for storage/API boundaries. For persisted fields or DTO changes, inspect both
Mobile repository/client mappings and server mappings.

Run the affected FishingTripSyncServiceTests or CatchSyncServiceTests in
Catchlogr.Tests using [Validation](../../docs/VALIDATION.md). Broaden to Mobile
or API checks when those boundaries change; sync mocks do not prove SQLite
upgrade compatibility or real network behavior.
