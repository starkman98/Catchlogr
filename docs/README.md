# Documentation map

[AGENTS.md](../AGENTS.md) contains shared agent rules.
[README](../README.md) is the only local setup guide. Read individual guides
when needed; do not load the entire documentation directory for every task.

| Guide owns | Document | Verify changes against |
| --- | --- | --- |
| Setup and implemented overview | [README](../README.md) | [SDK](../global.json), [solution](../Catchlogr.slnx), launch settings, Compose |
| Check selection and test limitations | [Validation](VALIDATION.md) | [CI](../.github/workflows/ci.yml), test projects/factories |
| MVVM, local storage, navigation, UTC examples | [Mobile architecture](MOBILE_ARCHITECTURE.md) | [Mobile composition](../src/Catchlogr.Mobile/MauiProgram.cs), Pages, ViewModels |
| Backend selection and device addresses | [Mobile configuration](MOBILE_CONFIGURATION.md) | [AppSettings](../src/Catchlogr.Mobile/Configuration/AppSettings.cs), [URL resolver](../src/Catchlogr.Mobile/Configuration/PlatformApiUrl.cs), Mobile project/settings |
| Reconciliation and photo state | [Sync strategy](SYNC_STRATEGY.md) | [Trip sync](../src/Catchlogr.Sync/Services/FishingTripSyncService.cs), [catch sync](../src/Catchlogr.Sync/Services/CatchSyncService.cs), local repositories |
| Weather/moon ownership and enrichment | [Weather](WEATHER_INTEGRATION.md) | [Trip service](../src/Catchlogr.Application/Services/FishingTripService.cs), provider adapter/options |
| Location search behavior | [Location](LOCATION_SEARCH_INTEGRATION.md) | [Endpoint](../src/Catchlogr.Api/Endpoints/LocationEndpoints.cs), [adapter](../src/Catchlogr.Infrastructure/Location/LocationIqLocationSearchService.cs) |
| Identity email and Web actions | [Identity email](IDENTITY_EMAIL.md) | [API composition](../src/Catchlogr.Api/Program.cs), Email, Web pages |
| CI triggers and deployment | [CI/CD](CI-CD.md) | [CI](../.github/workflows/ci.yml), [deployment](../.github/workflows/deploy-dev.yml), [Compose](../deploy/docker/compose.dev.yml) |
| Unimplemented product/hardening work | [Roadmap](ROADMAP.md) | Implementation and regression evidence before marking complete |

## Maintenance
- Update the owning guide in the same change as code or workflow changes.
- Describe current behavior separately from requirements and future work.
- Link to commands and configuration at their owning location; avoid copies.
- Keep backlog items in ROADMAP.md, not repeated at the end of feature guides.
- Remove obsolete documents; Git history preserves historical snapshots.
- Run `./scripts/validate-docs.ps1` in PowerShell after documentation/instruction edits.
  It checks local links, heading anchors, code fences, and Claude's local import.
  It does not prove that prose matches code; review the sources above.
