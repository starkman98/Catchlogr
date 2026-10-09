---
name: validate-change
description: Select and run focused Catchlogr checks, diagnose validation failures, and report remaining manual checks. Use when validating a change or investigating failed validation.
---

# Validate a Catchlogr change

Resolve commands from the repository root, even if the session starts in a
subdirectory. [Validation](../../../docs/VALIDATION.md) owns commands, prerequisites,
test limitations, and the measured baseline; read the sections needed for this change.

- Inspect staged, unstaged, and untracked changes. Preserve unrelated edits.
  For a branch review, include committed changes using an existing base ref;
  a clean working tree alone does not establish that a branch needs no checks.
- Preview with the change helper before executing. Review its reasons, suggested
  guides, and manual items against the actual diff and indirect consumers.
  Path selection cannot establish all behavioral effects.
- Run the selected checks once. During implementation, focused test filters can
  shorten iteration; final validation still covers the affected suites.
- If a check fails, distinguish a product regression from missing SDK/workload,
  network access, permissions, or build imports. Use the first actionable error.
  Retry only after a relevant change or an authorized environment correction;
  avoid repeated identical restores and workload installations.
- Follow the guide's build freshness rules before reusing outputs. Report blocked
  checks and remaining database/device review separately from automated passes.

Example request: "Validate this branch against origin/dev and explain anything
that still needs manual review." Preview the branch with BaseRef before Run.

Finish with checks actually run, their results, and material limits. Use supported
approval mechanisms for access failures; this workflow does not grant permission
to push, deploy, migrate shared databases, or alter security settings.
