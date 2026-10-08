# Working in Catchlogr

Shared instructions for Codex, GitHub Copilot, and Claude Code.

## Work efficiently
- Give a short plan before editing, then proceed within the requested scope.
- Inspect the working-tree diff; preserve unrelated changes.
- Search targeted paths with `rg`. Read the relevant guide below, not all docs.
- Ask only when a missing decision materially affects the result. Choose simple
  implementation details yourself; do not repeatedly ask for the same approval.
- Repository instructions do not override tool permissions. Explain access
  failures accurately; never disable safeguards to avoid an approval.
- Do not push, merge, deploy, or apply migrations to a shared database unless
  requested. A push to `dev` can deploy the application and run migrations.

## Project map
- Mobile: MAUI MVVM, device features, SQLite repositories, typed API clients.
- Sync: platform-neutral local entities, abstractions, and reconciliation.
- Api: Minimal API endpoints, authentication, and server DI composition.
- Application: business services, validation, and provider-neutral interfaces.
- Domain: domain entities and repository interfaces.
- Contracts: API DTOs, separate from domain entities; prefer immutable records.
- Infrastructure: PostgreSQL/EF Core, Identity, provider adapters, photo storage.
- Web: Razor Pages for account confirmation and password recovery.

Mobile references Application, Contracts, and Sync; never Infrastructure.
Sync references Contracts and must not depend on MAUI or server infrastructure.
Keep references acyclic. Clients use the API, never PostgreSQL or server
providers directly. HTTPS is required except the configured Local mobile backend.

## Implementation rules
- Prefer small, composable classes; use DI and thin repositories.
- One type per file; filename matches the type. Add XML summaries to public
  types and members; add usage examples where they clarify non-obvious behavior.
- Keep endpoint handlers thin and use Minimal APIs. Validate inputs with
  FluentValidation or focused helpers; return meaningful validation errors.
- Use appropriate HTTP statuses: 201 creation, 400 validation, 404 missing or
  inaccessible resource, 409 conflict.
- Enforce resource ownership. Preserve ASP.NET Core Identity bearer/refresh-token
  authentication; do not assume tokens are JWTs or add auth shortcuts.
- Preserve backend/account isolation for SQLite, tokens, cursors, and photos.
- Use async/await for I/O, with CancellationToken parameters; never block using
  `.Result` or `.Wait()`. Propagate cancellation where supported.
- Use nullable reference types, input guards, and structured error handling.
  Log useful errors without secrets, tokens, connection strings, precise
  coordinates, or provider URLs containing credentials.
- Use C# naming: PascalCase public members, _camelCase private fields, I-prefixed
  interfaces. Register services in Program.cs or MauiProgram.cs as appropriate.
- Store timestamps as UTC. Convert local picker values at the ViewModel boundary.
- ViewModels use injected abstractions, not SQLite or HttpClient directly.
- Use partial-property ObservableProperty syntax; keep Mobile's preview language
  setting. Marshal background UI updates to the main thread; expose busy state.
- Use EF Core migrations for server schema changes. External identifiers are
  GUIDs; local integer IDs stay local. Never commit secrets; keep production
  CORS restricted to explicit origins.
- Preserve upload-before-download and trip-before-catch sync ordering.
  Do not claim atomic reconciliation or remote-delete propagation already exists.

## Read only what the task needs
| Task | Guide |
| --- | --- |
| First-time setup, project layout | [README](README.md) |
| Select checks, understand test limits | [Validation](docs/VALIDATION.md) |
| Mobile MVVM, UTC examples, navigation | [Mobile architecture](docs/MOBILE_ARCHITECTURE.md) |
| Backend selection, emulator/device URLs | [Mobile configuration](docs/MOBILE_CONFIGURATION.md) |
| Reconciliation, photos, failure behavior | [Sync strategy](docs/SYNC_STRATEGY.md) |
| Weather/moon enrichment | [Weather](docs/WEATHER_INTEGRATION.md) |
| Named location search | [Location](docs/LOCATION_SEARCH_INTEGRATION.md) |
| Identity email and Web flows | [Identity email](docs/IDENTITY_EMAIL.md) |
| Branches, CI, deployment | [CI/CD](docs/CI-CD.md) |
| Product scope and known hardening work | [Roadmap](docs/ROADMAP.md) |

## Finish
- Run the smallest relevant checks; broaden for affected boundaries or failures.
  Documentation-only work needs documentation validation, not a solution build.
- Update the existing owning guide in the same change when behavior, routes,
  commands, or configuration change. Keep each fact in one place and link to it.
- Executable source and tests establish current behavior. Flag a discrepancy
  rather than inventing a guarantee or silently changing product behavior.
- Report what changed, checks actually run, and material limits. Do not claim
  tests passed when they were skipped or blocked.
