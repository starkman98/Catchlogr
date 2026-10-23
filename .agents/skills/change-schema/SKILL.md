---
name: change-schema
description: Change or review Catchlogr persisted fields, PostgreSQL EF migrations, SQLite tables, and existing-data compatibility. Use when persistence structure changes or a database upgrade fails, rather than for ordinary queries with unchanged storage.
---

# Change persistence without losing existing data

Identify the affected database and owning models before editing. Server models
and local models have different IDs and lifecycles; a DTO change alone does not
establish that either schema needs changing.

- For PostgreSQL, read [Infrastructure rules](../../../src/Catchlogr.Infrastructure/AGENTS.md).
  Review domain entities, EF configuration, generated migration operations, and
  the model snapshot together. Use the existing [migration commands](../../../README.md)
  and verify renames, nullable/default transitions, indexes, ownership relationships,
  cascades, and data preservation where affected.
- For SQLite, read [Mobile rules](../../../src/Catchlogr.Mobile/AGENTS.md) and inspect
  [LocalDatabase](../../../src/Catchlogr.Mobile/Data/LocalDatabase.cs), Sync entities,
  and repositories. Existing CreateTableAsync initialization does not establish
  arbitrary upgrade compatibility. Define the upgrade behavior for the changed
  field instead of assuming a fresh database test covers it.
- Follow changed fields through Contracts and local/server mappings where relevant.
  Preserve dirty records, pending photos, server IDs, and cursor metadata during
  local upgrades, with backend/account storage still isolated.
- Verify an affected upgrade using representative existing data in a disposable
  local database. Check preserved values and required transformations. A downgrade
  can be lossy; describe the limit rather than promising reversibility.
- API tests use EF InMemory, so they cannot establish PostgreSQL constraints or
  migration SQL. Existing local account-isolation tests do not establish upgrades
  from a previous schema. State any missing fixture or database prerequisite.

Use [Validation](../../../docs/VALIDATION.md) for commands and test limits.
Report migration/upgrade behavior, preservation evidence, and remaining checks.
Never apply a migration to a shared database without the user's authorization.
