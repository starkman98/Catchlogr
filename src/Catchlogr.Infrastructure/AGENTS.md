# Infrastructure

Applies to this directory and descendants, together with the root instructions.

- Implement provider-neutral Application/Domain interfaces here. Clients and
  Sync must not reference this project or access PostgreSQL/providers directly.
- Keep repositories thin and preserve user-scoped queries, including catch
  ownership through its parent trip. Propagate cancellation through EF calls.
- Use EF Core migrations for server schema changes. Keep migrations and the
  model snapshot consistent; review data preservation and database constraints.
- Preserve private photo storage and ownership boundaries. Never expose storage
  paths or credentials through public responses or provider error messages.
- Keep provider configuration in the existing options/registration patterns;
  wire server services through Api/Program.cs. Do not add secrets to settings.

Start in Persistence for EF configuration/migrations, Repositories for queries,
or the provider directory for adapters. Consult only the affected guide:
[weather](../../docs/WEATHER_INTEGRATION.md),
[location](../../docs/LOCATION_SEARCH_INTEGRATION.md), or
[email](../../docs/IDENTITY_EMAIL.md).

Run affected adapter/storage unit tests using
[Validation](../../docs/VALIDATION.md). For schema changes, review the migration
and apply it to disposable local PostgreSQL. API tests use EF InMemory and do
not establish PostgreSQL SQL, constraint, or migration correctness. Migration
commands live in the [README](../../README.md).
