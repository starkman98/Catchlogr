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
- DevHost and shared Core service registration select Mobile and API integration
  tests. These use the existing isolated API fixture and temporary SQLite.
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

## DevHost validation

Build the console host without MAUI:

```sh
dotnet build src/Catchlogr.Mobile.DevHost/Catchlogr.Mobile.DevHost.csproj --configuration Release
dotnet run --project src/Catchlogr.Mobile.DevHost -- --help
```

`DevHostOptionsTests` and `DevHostSessionTests` run in the Mobile suite and cover
loopback endpoint restrictions, credential-argument rejection, temporary storage
permissions, token/file cleanup, and synthetic GPS. `DevHostScenarioTests` run in
the API integration suite and use real confirmed-account authentication, Core
HTTP clients, SQLite, and Sync. They cover successful upload/read-back, separate
accounts, rejected login, cancellation during upload, and an upload outage that
must not report success.
The API fixture substitutes EF InMemory and captured email; these tests need no
MAUI, Docker, provider keys, or real credentials and do not establish PostgreSQL
behavior. See [DevHost usage](MOBILE_ARCHITECTURE.md#console-devhost) for a manual
local API run and its persistent server-side effects.

Observed on Linux with SDK 10.0.302 on 2026-11-08: DevHost built in Release with
zero warnings/errors; 277 unit tests, 95 Mobile tests, 44 API integration tests,
and 93 helper assertions passed. CLI help returned 0; a remote endpoint and
password argument returned 2 without printing the supplied synthetic secret.
Documentation validation passed. The Windows MAUI app build remained blocked on
Linux. No Docker/PostgreSQL application smoke run or device verification was
performed for this DevHost change.

## Isolated authentication and sync E2E

`tests/e2e/run.py` runs on a dedicated Linux Android host (recommended:
`agent-server`), separate from the deployed development VM and the general CI
runner. It creates a uniquely named Compose project using
`deploy/e2e/compose.yml`. PostgreSQL uses tmpfs, migrations run against only that
project's `catchlogr_e2e` database, and email goes to Mailpit. Photos use a
project-scoped disposable volume so bytes survive the tested API restart;
`down --volumes` removes that volume afterward.
The project uses its own bridge network. Weather/location origins point to
reserved `.invalid` names and account email goes only to Mailpit. No real provider
keys are supplied. This is not a network egress firewall; provider enrichment is
not covered by these scenarios.

Host prerequisites: Docker Engine with Compose v2 and daemon access, the .NET
SDK selected by global.json, Python 3.10+, Node.js 20+, OpenSSL, and (for the
Android flow) JDK 21, Android SDK, MAUI Android workload, a booted x86_64 emulator,
and the localhost Appium service described in [Android UI testing](ANDROID_UI_TESTING.md). No production credentials or
Resend key are needed. Install the pinned browser dependency once:

```sh
npm ci --prefix tests/e2e
npm exec --prefix tests/e2e -- playwright install chromium
```

If Chromium reports missing OS libraries, use Playwright's documented
`playwright install-deps chromium` on the dedicated host; this may require
administrator access. Do not install a second Appium or Android SDK over an
existing working host setup.

Recording is off by default. Set `E2E_RECORD_VIDEO=1` before invoking `ci.py`
or `run.py` to capture Android interactions and the first account's real Web
confirmation page. The Android host must support `screenrecord --time-limit 0`
(as the current API 36 emulator does). Recording begins after backend readiness
and is finalized on success or failure, before app/stack cleanup. Videos are saved
under `artifacts/e2e/<run-id>/`: `android.mp4` and `confirmation-video/*.webm`.
Backend-only runs produce only the browser clip. Playwright holds the confirmation
success page for three seconds; the Mailpit inbox API lookup is not visualized.
See [Android E2E CI](CI-CD.md#android-e2e-ci) for the manual recording checkbox
and artifact download. Videos contain unredacted disposable test UI data.

The runner's cleanup safety checks need no Docker or emulator:

```sh
python3 -m unittest discover -s tests/e2e -p 'test_*.py'
```

Run from the repository root:

```sh
# API + PostgreSQL + captured email + actual Web page in headless Chromium
python3 tests/e2e/run.py --backend-only

# Android registration, login, data sync and recovery; export JAVA_HOME and ANDROID_HOME first
python3 tests/e2e/run.py --device emulator-5554
```

On the existing agent-server installation, load
`~/.local/share/catchlogr-android/env.sh` and start its emulator script before the Android command. Start Appium with
`appium --address 127.0.0.1 --port 4723 --log-level warn --log-no-colors` so
WebDriver command bodies are not written into routine logs. The runner builds and installs its own Local
Catchlogr E2E APK, resolves the launch activity, and resets only
`com.catchlogr.e2e`. Keep this emulator dedicated to testing. The runner does
not start or stop the emulator/Appium, or modify the regular Catchlogr package.
It currently supports Linux x86_64 Android hosts; Windows execution needs a
portable runner adaptation.

The full flow registers a unique account through MAUI, observes the check-email
screen, verifies API and UI rejection before confirmation, finds that recipient's
email, opens its real Web confirmation URL in Chromium, signs in through MAUI,
and observes the empty trip list. It also verifies real access/refresh tokens,
authenticated collection access, and anonymous rejection through the API.
Backend-only mode performs registration/login through HTTP and omits Android;
it does not establish UI, synchronization, or camera behavior.

Ports are fixed and bound to loopback: API HTTP **15001** (Android Local
exception), API HTTPS **17160**, Web HTTPS **17056**, and Mailpit **18025**.
The runner refuses occupied ports and serializes runs per checkout. Across
checkouts, the fixed ports still permit only one active stack. The E2E mobile
package's address is documented in [Mobile configuration](MOBILE_CONFIGURATION.md#dedicated-e2e-package).
PostgreSQL is not published. Generated TLS certificates last two days and are
removed after the run; Web trusts only the generated CA for its API calls.
Only the disposable Chromium context ignores certificate errors and restricts
requests to this test Web origin. Host trust and production TLS settings are
not changed.

Normal success/failure and interrupt cleanup removes that generated Compose project's
containers/network and clears E2E app data. Build images and browser downloads
are retained for reuse. SIGKILL or a host crash can leave containers: inspect
`docker compose ls` and remove only the exact `catchlogr-e2e-<run-id>` project.
Do not reset the ordinary local or deployed database. Run status and, on UI
failure, a redacted hierarchy and best-effort screenshot go under ignored
`artifacts/e2e/<run-id>/`. Browser errors are sanitized because they can contain
confirmation URLs. Do not publish raw Appium logs: command bodies include test
passwords. Inbox bodies and tokens are not included in runner artifacts.

After authentication, the Android scenario creates a trip and catch through
MAUI, captures the emulator camera scene, and checks the uploaded API records,
parent GUID, fields, image bytes, and anonymous photo rejection. It then stops
only the generated Compose project's API, creates another trip and catch,
relaunches the app while the API is stopped to verify SQLite persistence, restores
the API, and returns to the trip list to run the ordinary synchronization flow.
This simulates backend unavailability; it does not disable the emulator's network.

The scenario clears only the E2E package, signs in again, and verifies both trips
and their catches download into fresh local storage. A SHA-256 comparison against
the private photo cache proves image bytes were downloaded. Another sync must
leave exactly two trips and two catches. A second normally registered/confirmed
account must see empty collections and receive 404 for the first account's trip,
catch, and private photo. These checks exercise real PostgreSQL migrations, HTTP,
Identity, account-scoped SQLite, and the production sync orchestrator.

The test uses stable AutomationIds and Android toolbar navigation. The camera
selectors target the existing API 36 emulator's `com.android.camera2` app; other
camera applications need corresponding selectors. It does not establish physical
camera behavior, photo replacement/deletion, arbitrary interrupted requests,
conflict resolution, deletion propagation, provider enrichment, or real Resend
delivery. The known reconciliation limits remain in [Sync strategy](SYNC_STRATEGY.md).

`tests/e2e/ci.py` starts missing emulator/Appium processes with warning-only
Appium logging, invokes the same runner, and stops only processes it started.
Existing host processes are reused. Keep this host dedicated and avoid concurrent
manual E2E runs across checkouts. See [Android E2E CI](CI-CD.md#android-e2e-ci)
for workflow and runner registration status.

Observed on agent-server on 2026-11-13: the complete scenario passed through
`tests/e2e/ci.py`, including native camera capture, owner/anonymous photo access,
API-outage persistence/recovery, fresh app download with matching photo bytes,
repeat sync without duplicates, and second-account trip/catch/photo rejection.
Cleanup removed the generated containers, network, and photo volume; the ordinary
PostgreSQL container remained running. The Mobile suite passed all 97 tests,
including two new photo-client cache cases; four harness ownership/cleanup checks,
47-file documentation validation, 93 helper assertions, Compose configuration,
and workflow YAML validation also passed. Earlier backend validation passed 288
unit tests and 44 API integration tests. The Windows build remains unavailable
on Linux; the actual Android target builds with existing XAML warnings. No shared
database is used or reset. The host owner subsequently installed the Actions runner, which was verified
online and listening for jobs. The scenario result above is a local execution
of the CI entry point, not a completed GitHub Actions job.

## Android emulator and Appium verification

For emulator installation, Windows PowerShell setup, Appium screen interaction,
standalone APK builds, and the Linux agent-server baseline, see
[Android UI testing](ANDROID_UI_TESTING.md). These ad hoc checks are separate
from the automated Android E2E workflow above; the validation helper does not
launch a device.
