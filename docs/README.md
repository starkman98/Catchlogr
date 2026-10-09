# Documentation map

[AGENTS.md](../AGENTS.md) contains shared agent rules.
[README](../README.md) is the only local setup guide. Read individual guides
when needed; do not load the entire documentation directory for every task.

## Agent instruction maintenance

Root [AGENTS.md](../AGENTS.md) owns shared rules and the scoped-instruction map.
Project/test AGENTS.md files own local constraints and point to these guides.
Keep commands and detailed behavior in their existing owning guide; update it
in the same change as the behavior. Add a scope only when it needs distinct rules.

- Codex discovers instructions along the path to its working directory. Root
  instructions explicitly require reading affected scopes for sessions launched
  at the root. See [Codex discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md).
- Each scoped CLAUDE.md imports its adjacent AGENTS.md. Claude loads nested
  CLAUDE.md files on demand; the root import stays small. Check loaded files with
  `/context`. See [Claude memory](https://code.claude.com/docs/en/memory).
- Copilot's path-specific files in .github/instructions use applyTo globs and
  direct the agent to the owning AGENTS.md. Support varies by Copilot surface;
  confirm the adapter appears in instruction references where available. If
  linked files cannot be read, attach the root and relevant scoped instructions.
  See [Copilot instructions](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions).

Keep adapters as pointers/imports rather than copies of project rules. Link
validation checks their targets; it cannot establish that an installed agent
actually loads or follows them. After changes, check a session in each tool with
a focused request to identify the applicable instructions and smallest checks.

## Guide ownership

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
  Scoped AGENTS.md/CLAUDE.md files under src/tests and Copilot adapters are included.
  It does not prove that prose matches code; review the sources above.
