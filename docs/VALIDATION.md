# Validation

Choose checks by the affected behavior. Run from the repository root. Avoid a
whole-solution build for a focused change: it can require unrelated MAUI workloads.
The documentation script runs in Windows PowerShell 5.1 or PowerShell 7; CI uses
PowerShell 7. Invoke it directly from either PowerShell without installing tools.

## Select checks from changed files

```powershell
# Preview local staged, unstaged, and untracked changes; runs no checks
./scripts/validate-change.ps1

# Also include committed changes since the merge base with an existing ref
./scripts/validate-change.ps1 -BaseRef origin/dev

# Execute the selected automated checks
./scripts/validate-change.ps1 -Run
./scripts/validate-change.ps1 -BaseRef origin/dev -Run

# Check the helper itself without installing a test framework
./scripts/tests/validate-change.tests.ps1
```

The helper works from any working directory: its default repository is the one
containing the script. `-RepositoryRoot` can select another checkout or one of
its subdirectories. It does not fetch refs. An invalid base or missing merge base
fails explicitly. Without BaseRef, a clean working tree selects no checks even
if the branch contains commits; use BaseRef when validating a whole branch.

## Explicit validation profiles and environment diagnostics

`Changed` is the default profile described above. `Backend` adds the complete
unit and API integration suites; `Mobile` adds Mobile tests and a Windows app
build. Profiles work even in a clean checkout. They are additive: changed-file
checks, documentation suggestions, and manual review items remain in the plan.
A Backend profile can therefore also select Mobile checks when the diff needs them.
Commands and deduplication are shared with changed-file selection, not copied.

```powershell
# Preview first; add -Run to execute
./scripts/validate-change.ps1 -Profile Backend
./scripts/validate-change.ps1 -Profile Mobile -BaseRef origin/dev

# Inspect prerequisites without restoring, building, or repairing anything
./scripts/diagnose-environment.ps1
./scripts/diagnose-environment.ps1 -Profile Mobile
```

The diagnostic command defaults to Backend and accepts `-RepositoryRoot` pointing
to a checkout root containing global.json. It runs Git's version command and
`dotnet --version` from that root, allowing the CLI to enforce SDK selection and
roll-forward. Mobile also checks Windows and the installed MAUI Windows workload
with `dotnet workload list`. It reports restore-asset presence as information,
never as proof of freshness. These are inspection commands; no explicit network
probe, restore, build, install, cache deletion, or security change is performed.

Exit 1 means a prerequisite is blocked or inspection failed; exit 0 means only
the inspected prerequisites passed. Missing restore assets are informational:
normal validation restores them. NuGet connectivity, cached runtime packs,
generated import correctness, and build/test access remain unverified. Raw
command output is suppressed; if inspection is blocked, run the indicated version
or workload command directly and inspect its first actionable error. For the
observed Mobile import problem, follow [restore recovery](#historical-maui-restore-and-import-recovery).
For access failures, use supported approval mechanisms rather than changing policy.

See Microsoft's [SDK selection rules](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
for how repository working directory affects SDK resolution.

## Selection and execution behavior

Preview prints changed paths, selected commands/reasons, detectable prerequisites,
and manual review items. It includes both sides of renames and deleted files,
deduplicates checks, and ignores Git-ignored untracked files. It reads Git and
checks tool availability; it does not restore/build/test or inspect workloads.

The executable mapping lives in [Validation.psm1](../scripts/Validation.psm1):

- Markdown/agent instructions select documentation checks only.
- Application, API, and Infrastructure select unit/API suites; Application
  interfaces also select Mobile. Web selects the unit suite.
- Sync services select the unit suite; local entities/abstractions also select
  Mobile. Mobile.Core and Mobile changes select portable Mobile tests, with a Windows
  app build for XAML, code-behind, resources, platform adapters, and MauiProgram.
- Contracts, Domain, project/dependency/SDK/build inputs select all three suites.
- Changes to the helper select its own checks and documentation validation.
- Workflow/deployment changes select all suites plus documentation validation.
  Unknown paths select these and helper checks, with an explicit review warning.

Rules are additive except Markdown, which always selects documentation only.
Suite selection is deliberately conservative; it does not infer affected test
methods from filenames. Focused filters remain useful while iterating, but the
selected suites are the baseline for final validation. Review indirect callers,
API/DTO mappings, persisted fields, and shared behavior before reducing checks.
Code-only changes can also require an owning-guide update; the helper cannot
determine semantic documentation accuracy.

The helper also suggests owning guides for source, configuration, test, and
workflow paths. Each suggestion lists its triggering files and whether the guide
is already changed. Review affected claims against implementation and tests;
update the guide only when documented behavior changes. An already changed guide
still needs accuracy review. Suggestions are advisory, do not select additional
automated checks or affect exit status, and cannot cover every semantic dependency.
The mapping lives alongside check selection in Validation.psm1; the
[documentation map](README.md#guide-ownership) owns guide responsibilities.

Run mode executes each selected command once, sequentially, from the Git root
and reports status and elapsed time. Failed or blocked checks produce exit 1;
other selected checks still run. Mobile tests run on plain net10.0 without MAUI;
only the MAUI app build requires Windows and the MAUI Windows workload. It never installs tools/workloads, pushes, deploys, or runs
migrations. Missing/unsupported SDKs and other command errors remain failures.
Manual database/UI/deployment reviews are not executed or marked passed; exit 0
means only that selected automated checks passed (or preview succeeded).

The helper's tests cover profile selection, diagnostic failures with fake probes,
and representative instruction-only/mixed deployment paths. The filter fixture
checks the simple glob forms in the workflow; it does not run GitHub Actions.
They capture expected command and Git failures in disposable
fixtures, printing only their assertion summary when successful. An unexpected
entry-point assertion includes captured stdout/stderr for diagnosis. Real
validation failures still print their details and return a nonzero exit code.
CI runs these helper tests alongside documentation validation, using fake runners
rather than nesting real application test suites.

Both helper and documentation scripts support Windows PowerShell 5.1 and
PowerShell 7. If script execution or filesystem access is blocked, use the
client's supported approval mechanism or an authorized shell; do not change
execution policy or filesystem permissions just to avoid an approval.

## Broaden checks when behavior crosses boundaries

| Change | Relevant check |
| --- | --- |
| Markdown or agent instructions only | Documentation command below |
| Business services, validators, provider adapters, Sync, Web client/page logic | Catchlogr.Tests; use a focused filter while iterating |
| API routes, authentication, ownership | API integration tests plus affected unit tests |
| Mobile ViewModels, authentication, configuration | Mobile.Core tests on Linux, macOS, or Windows |
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

# Linux, macOS, or Windows; no MAUI workload
dotnet test tests/Catchlogr.Mobile.Tests/Catchlogr.Mobile.Tests.csproj
dotnet build src/Catchlogr.Mobile.Core/Catchlogr.Mobile.Core.csproj

# Example: focus on synchronization while iterating
dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj --filter "FullyQualifiedName~SyncServiceTests"
```

Once the affected suite passes, repeat it only after relevant changes or new
evidence. Use `--no-restore` only when dependencies are already restored; use
`--no-build` only when the matching configuration has been built since the last
code change. Report missing prerequisites instead of reporting a skipped check
as passing.

## Historical MAUI restore and import recovery

Before the Core extraction, Mobile tests referenced MAUI and could fail on Windows
with the Windows App SDK `CustomBeforeMicrosoftCommonTargets` import error.
A diagnostic restore had used a global `-p:TargetFrameworks=...` override across
a mixed single/multi-target project graph, leaving incompatible generated imports.
A normal restore with the original project properties repaired that observed case.
Current portable Mobile tests have no WinUI imports.

Avoid global TargetFrameworks overrides across mixed project graphs. Do not edit
NuGet-generated props/targets under obj. Missing runtime packs, denied package
access, and missing platform workloads are separate prerequisites. See
[NuGet build imports](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)
and [restore options](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore).

## Measured local baseline

Observed on Windows with SDK 10.0.401 (allowed by global.json roll-forward),
Release configuration, on 2026-10-09. Dependencies were already cached and
restore reported up-to-date; these are single observations, not cold-cache
benchmarks or CI timings. Test times below include the dotnet/test-host startup.

| Suite | Restore | Build without restore | Test without build | Result |
| --- | --- | --- | --- | --- |
| Unit | 1.97 s | 14.14 s | 4.82 s | 250 passed |
| API integration | 1.23 s | 5.65 s | 15.14 s | 23 passed outside sandbox |

A subsequent incremental restore/build took 1.34/3.02 s for Unit and
1.30/2.15 s for API. No source changed between the builds; shared assets were
restored again after the Mobile prerequisite investigation. The reduced build
times support retaining normal incremental builds instead of a custom cache.

The first API attempt inside the Windows sandbox failed 20 of 23 tests with
Windows Event Log access errors and took 100.48 s. The supported approved retry
passed without source changes. Do not treat sandbox failures as product
regressions or silently disable logging/security to make tests pass.

Mobile initially failed with NETSDK1112 (missing win-x64 runtime pack). An
approved diagnostic restore downloaded the dependencies but used a global
TargetFrameworks override, leaving incompatible generated imports for normal builds.
Follow-up on the same date repaired those imports with a normal restore and
successfully built the Windows Mobile app and test project in Release:
**58 tests passed, zero failed or skipped**, with test execution reported as 776 ms.
The build emitted five XC0025 warnings for bindings with explicit Source whose
compilation is not enabled; they did not prevent the build or tests from passing.
No application source, package versions, or build properties changed. This was
not a cold-cache run; runtime packs downloaded during the earlier attempt remained
available. See [import recovery](#historical-maui-restore-and-import-recovery).

The helper's own checks take approximately 7 s including disposable Git fixtures
and child-shell checks; documentation validation takes approximately 0.5 s here.
Use the timings printed by Run mode to track subsequent local observations.

Priorities supported by these observations:

1. Select only relevant suites; documentation changes need no MAUI/backend build.
2. Use normal incremental builds by default. Avoid repeating passing suites
   after unrelated edits; use explicit focused filters during iteration.
3. Diagnose sandbox/network/runtime prerequisites before blind retries. Reuse
   existing builds only with the configuration/target and freshness rules above.

The earlier CI review found separate unit/API jobs, a Windows Mobile job that installed MAUI,
and deployment tests that validate the deployed commit again. Their separation
provides isolation; the Core extraction keeps separate suites while moving Mobile tests to Linux. Historical runner/queue timings
were not available locally, so no CI speedup is claimed. Measure those before
changing job layout, caching, or required checks; deployment tests remain intact.

## Linux server verification

Observed on 2026-11-08 with user-local SDK 10.0.302, PowerShell 7.6.9, and EF CLI
10.0.14. The Backend profile restored and built successfully: **277 unit tests
and 40 API integration tests passed, with zero failures or skips**. Documentation
validation passed for 41 Markdown files; validation-helper checks passed with
89 assertions. These are observations, not pinned test counts or guarantees for
later commits. No live application/provider smoke check was performed.

Rootless Docker 29.1.3 and Compose 2.40.3 were verified separately with
`hello-world` and a disposable PostgreSQL 16 Compose service. Its health check,
SQL writes/reads, and host-to-container PostgreSQL protocol handshake passed.
The temporary container, network, and volume were removed. The Docker user
service was enabled and active with lingering enabled. This did not validate
application migrations or existing database upgrades.

The sandbox denied rootless socket access and initially blocked NuGet DNS and
PowerShell cache initialization. Supported approved execution outside the sandbox
succeeded without changing security settings. Future sessions must still check
their own permissions and prerequisites. See [local setup](../README.md#local-setup)
for shared secrets, worktrees, and rootless Docker. That backend-only verification did not cover Mobile tests or device behavior.
The subsequent Mobile.Core verification below covers portable mobile logic.

## Mobile.Core Linux verification

After the Core extraction, on Linux with SDK 10.0.302, all **75 Mobile tests
passed in Release**, with zero failures or skips. This includes the 65 existing
tests and ten new cases for configuration resources, trip saving/validation,
location permissions, offline refresh dispatch, and photo capture file handling.
The same 75 tests passed in Debug through the branch validation helper. The
standalone Core Release build completed with zero warnings or errors. The branch
also passed 277 unit tests, 40 API integration tests, documentation validation,
and 91 helper assertions. No MAUI workloads were installed. The helper reported
the Windows MAUI app build as blocked on Linux; camera/GPS/Shell/device UI checks
remain separate and were not established by these tests.

## What the tests establish

- Catchlogr.Tests uses xUnit, FluentAssertions, and NSubstitute. It includes
  application, provider, synchronization, and Web tests; it does not require MAUI.
- API integration tests host the API through CatchlogrApiFactory. It replaces
  PostgreSQL with EF Core InMemory, email with a capturing sender, and photos
  with temporary private storage. No real database or email credentials are
  needed. These tests do not verify PostgreSQL SQL, constraints, or migrations.
- Mobile tests target net10.0 and reference Mobile.Core, without a runtime identifier
  or MAUI workload. Passing them does not validate MAUI UI or device behavior.
- Documentation validation is offline and does not check external URLs or
  semantic accuracy. Review changed prose against its owning source files.
  It includes scoped AGENTS.md/CLAUDE.md under src/tests and Copilot instruction
  adapters; generated bin/obj/node_modules and directory reparse points under
  src/tests are skipped. It does not verify runtime instruction loading in an
  installed agent. See the [documentation map](README.md#agent-instruction-maintenance)
  for adapter maintenance and manual loading checks.

See [CI/CD](CI-CD.md) for where these checks run and
[README](../README.md) for local database setup and migration commands.
