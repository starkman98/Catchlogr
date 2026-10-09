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

## Shared agent workflows

Agents select applicable workflows from the task table in root AGENTS.md without
waiting for the user to name them. Read only the relevant skill bodies:

| Workflow | Shared source | Example request |
| --- | --- | --- |
| Focused validation and failure diagnosis | [validate-change](../.agents/skills/validate-change/SKILL.md) | Validate this branch against origin/dev and explain remaining manual checks. |
| Documentation accuracy and pruning | [maintain-docs](../.agents/skills/maintain-docs/SKILL.md) | Review the documentation affected by this sync change. |
| Features across project boundaries | [change-feature](../.agents/skills/change-feature/SKILL.md) | Add water temperature to catches, including offline editing and sync. |
| Sync transitions and failure recovery | [review-sync](../.agents/skills/review-sync/SKILL.md) | Investigate catches disappearing after an interrupted sync. |
| Persistence and upgrade compatibility | [change-schema](../.agents/skills/change-schema/SKILL.md) | Add this persisted field while preserving existing databases. |
| Authentication and data isolation | [review-account-isolation](../.agents/skills/review-account-isolation/SKILL.md) | Fix account switching showing the previous user's photos. |

Codex discovers repository skills in .agents/skills and allows implicit selection
by default. Copilot supports this directory in skill-enabled surfaces and selects
skills by their descriptions. Describe the task normally; explicit skill names
are optional. See the official
[Codex skills](https://learn.chatgpt.com/docs/build-skills) and
[Copilot skills](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills)
documentation. Discovery and invocation depend on the installed client.

Claude Code uses a thin .claude/commands adapter for each skill. This supported
format allows automatic selection from its description as well as explicit
`/skill-name` invocation. Each adapter directs Claude to read the shared workflow,
so its body stays in one place without Windows symlinks or duplicate SKILL.md
discovery. See [Claude skills and command compatibility](https://code.claude.com/docs/en/skills).

Keep meaningful trigger descriptions in both the shared skill and its Claude
adapter. Preserve default automatic invocation: do not add Codex's
allow_implicit_invocation: false or Claude's disable-model-invocation: true for
these workflows. Selecting a workflow does not authorize external mutations or
bypass tool permissions.

Start a fresh session after adding workflows if the client does not refresh its
discovery list. Check each tool with the example requests above, without naming
skills, and confirm it reads the shared source, chooses appropriate checks, and
reports manual limits. Root instructions provide direct-file routing when native
discovery is unavailable. Client settings can still disable skills or repository
instructions; offline link validation cannot establish live client behavior.

## Guide ownership

| Guide owns | Document | Verify changes against |
| --- | --- | --- |
| Setup and implemented overview | [README](../README.md) | [SDK](../global.json), [solution](../Catchlogr.slnx), launch settings, Compose |
| Check selection and test limitations | [Validation](VALIDATION.md) | [Selection helper](../scripts/Validation.psm1), [CI](../.github/workflows/ci.yml), test projects/factories |
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
