---
name: review-sync
description: Review or change Catchlogr synchronization, cursor advancement, dirty records, conflict handling, deletion, photo retries, and interruption recovery. Use when a task changes reconciliation behavior or investigates missing or duplicated synced data.
---

# Review synchronization behavior

Read [Sync rules](../../../src/Catchlogr.Sync/AGENTS.md) and the relevant sections
of [Sync strategy](../../../docs/SYNC_STRATEGY.md). Follow the affected entity
through its sync service, API client, local repository, and server mapping;
read Mobile or API rules when those implementations are involved.

- Trace the persisted state before and after each remote call. For changed
  transitions, consider success, network failure, cancellation, and the next retry.
  Check dirty/deleted flags, server IDs, parent IDs, timestamps, and pending photo state.
- Verify trip-before-catch and upload-before-download ordering. A local catch
  whose parent has no server ID must remain recoverable until its parent syncs.
- Compare the rows actually applied with the cursor being stored, including
  skipped rows, missing parents, equal timestamps, and interrupted downloads.
  Current application of rows and cursor advancement is not atomic; remote hard
  deletes have no tombstones. Treat those as current limits, not guarantees.
- For photo changes, trace upload, attachment, download, replacement, deletion,
  and retries separately. Check whether a failure could lose pending work or
  repeat a remote operation without preserving its successful result.
- Verify backend/account scope for repositories, cursors, and photo files when
  the task changes their lifetime or activation. Do not assume mock isolation
  establishes real SQLite behavior.

Reuse [catch sync tests](../../../tests/Catchlogr.Tests/Services/CatchSyncServiceTests.cs)
and [trip sync tests](../../../tests/Catchlogr.Tests/Services/FishingTripSyncServiceTests.cs).
Add focused behavioral cases for changed transitions; use
[Validation](../../../docs/VALIDATION.md) to select final checks and manual limits.

For a review, report a concrete failure sequence, affected code, and whether
existing tests cover it. For implementation, fix only the requested behavior and
update the owning guide. Do not turn a focused task into a sync redesign.
