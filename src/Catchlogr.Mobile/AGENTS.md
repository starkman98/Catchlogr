# Mobile

Applies to this directory and descendants, together with the root instructions.

- ViewModels use injected abstractions, never SQLite or HttpClient directly.
- Use partial-property ObservableProperty syntax; retain the project's preview
  language setting. Expose busy state and marshal background UI updates to the
  main thread.
- Preserve backend/account isolation for SQLite, tokens, cursors, and photos,
  including login, refresh, logout, and backend switches.
- Convert local picker values to UTC at the ViewModel boundary. Persist UTC.
- Keep platform/device APIs behind abstractions; register services in
  [MauiProgram.cs](MauiProgram.cs). Preserve the configured Local HTTP exception.

Start in Pages for UI or Services for platform adapters. Portable ViewModels,
API clients, SQLite, and configuration live in [Mobile.Core](../Catchlogr.Mobile.Core/AGENTS.md). Read only the relevant guide:
[architecture](../../docs/MOBILE_ARCHITECTURE.md),
[configuration](../../docs/MOBILE_CONFIGURATION.md), or
[sync](../../docs/SYNC_STRATEGY.md).

For code changes, select affected Mobile tests using
[Validation](../../docs/VALIDATION.md) and the root validation helper; they
run without MAUI. Building the MAUI shell still needs the affected platform toolchain.
XAML/platform changes also need an affected-target build and UI/device checks.
SQLite schema changes need an upgrade check against an existing local database.
