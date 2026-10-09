---
name: change-feature
description: Implement Catchlogr features or fields that cross API, Contracts, persistence, Sync, and Mobile boundaries. Use when adding or changing behavior across projects; a local refactor or cosmetic UI edit does not need this workflow.
---

# Change a feature across project boundaries

Start with the requested observable behavior. Use the project map and scoped
instructions in [AGENTS.md](../../../AGENTS.md) to identify affected boundaries.
Trace an existing nearby feature before choosing the implementation shape.

- For a new or changed field, trace input, validation, domain storage, response,
  client mapping, local persistence, sync upload/download, and display/editing.
  Identify applicable layers first; do not create a layer or schema change merely
  because it appears in this list.
- Check creation and update separately. Define omission/null/default behavior,
  UTC conversion where relevant, and compatibility with existing records and
  older persisted local state. Preserve server GUID and local integer ID roles.
- Keep DTOs separate from domain entities and review manual mapping code in both
  directions. Server-populated values must reach the local model after upload as
  well as after download. Register new abstractions at the owning composition root.
- If persistence changes, read the change-schema workflow listed in root rules.
  If reconciliation or ownership behavior changes, read the relevant focused
  review workflow. Load only workflows that affect this task.
- Test the observable behavior at its owning layer, then cover the affected
  boundary mappings and failure cases. Reuse existing service, validator, API,
  Sync, and Mobile fixtures rather than writing matching tests in every project.

Use [Validation](../../../docs/VALIDATION.md) for check selection and
[guide ownership](../../../docs/README.md#guide-ownership) for documentation updates.
Report the resulting behavior, compatibility decisions, checks run, and any
device/database verification that remains. Implement within the requested scope;
this workflow does not authorize deployment or shared database migrations.
