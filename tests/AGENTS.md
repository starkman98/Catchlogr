# Tests

Applies to this directory and descendants, together with the root instructions.

- Preview suites with `./scripts/validate-change.ps1` from the repository root;
  use [Validation](../docs/VALIDATION.md) for commands and broadening rules.
  Start with affected tests while iterating; broaden when a boundary changes.
- Test observable behavior and failure recovery. Reuse existing fixtures and
  test doubles; avoid tests that merely mirror implementation details.
- Catchlogr.Tests covers services, validators, provider adapters, Sync, and Web
  without MAUI. TestDoubles contains a fixed clock and stub HTTP handler.
- API integration tests use
  [CatchlogrApiFactory](Catchlogr.Api.IntegrationTests/CatchlogrApiFactory.cs):
  isolated EF InMemory, captured email, and temporary private photos. Preserve
  the real authentication pipeline; use separate accounts for ownership checks.
  These tests do not verify PostgreSQL constraints, SQL, or migrations.
- Mobile tests reference the real MAUI project and require Windows/MAUI. Their
  passing does not establish Android/iOS UI or device behavior, or an existing
  SQLite database's upgrade compatibility.
- Keep tests independent of real credentials and live provider services.
  Preserve temporary-file cleanup and per-test account/database isolation.

When changing a test, also read the scoped instructions for the production
project whose behavior it exercises. Record checks actually run and any missing
prerequisites; never report a skipped or blocked suite as passing.
