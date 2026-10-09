# Validation

Choose checks by the affected behavior. Run from the repository root. Avoid a
whole-solution build for a focused change: it can require unrelated MAUI workloads.
The documentation script runs in Windows PowerShell 5.1 or PowerShell 7; CI uses
PowerShell 7. Invoke it directly from either PowerShell without installing tools.

| Change | Relevant check |
| --- | --- |
| Markdown or agent instructions only | Documentation command below |
| Business services, validators, provider adapters, Sync, Web client/page logic | Catchlogr.Tests; use a focused filter while iterating |
| API routes, authentication, ownership | API integration tests plus affected unit tests |
| Mobile ViewModels, authentication, configuration | Mobile tests on Windows with MAUI |
| XAML or platform behavior | Build the affected target; manually check the affected UI/device flow |
| Contracts or persisted fields | Affected server, sync, and mobile suites; verify all mappings |
| PostgreSQL schema | Review migration/snapshot and apply to a disposable local PostgreSQL database |
| SQLite schema | Exercise upgrade from an existing database; unit mocks do not establish compatibility |
| Workflow/Compose | Review triggers, jobs, paths, and service configuration; update CI-CD.md |

## Commands

```powershell
./scripts/validate-docs.ps1

dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj
dotnet test tests/Catchlogr.Api.IntegrationTests/Catchlogr.Api.IntegrationTests.csproj

# Windows with the MAUI workload
dotnet test tests/Catchlogr.Mobile.Tests/Catchlogr.Mobile.Tests.csproj

# Example: focus on synchronization while iterating
dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj --filter "FullyQualifiedName~SyncServiceTests"
```

Once the affected suite passes, repeat it only after relevant changes or new
evidence. Use `--no-restore` only when dependencies are already restored; use
`--no-build` only when the matching configuration has been built since the last
code change. Report missing prerequisites instead of reporting a skipped check
as passing.

## What the tests establish

- Catchlogr.Tests uses xUnit, FluentAssertions, and NSubstitute. It includes
  application, provider, synchronization, and Web tests; it does not require MAUI.
- API integration tests host the API through CatchlogrApiFactory. It replaces
  PostgreSQL with EF Core InMemory, email with a capturing sender, and photos
  with temporary private storage. No real database or email credentials are
  needed. These tests do not verify PostgreSQL SQL, constraints, or migrations.
- Mobile tests target net10.0-windows10.0.19041.0 / win-x64 and reference the MAUI
  project. Passing them does not validate Android/iOS UI or device behavior.
- Documentation validation is offline and does not check external URLs or
  semantic accuracy. Review changed prose against its owning source files.
  It includes scoped AGENTS.md/CLAUDE.md under src/tests and Copilot instruction
  adapters; generated bin/obj/node_modules and directory reparse points under
  src/tests are skipped. It does not verify runtime instruction loading in an
  installed agent. See the [documentation map](README.md#agent-instruction-maintenance)
  for adapter maintenance and manual loading checks.

See [CI/CD](CI-CD.md) for where these checks run and
[README](../README.md) for local database setup and migration commands.
