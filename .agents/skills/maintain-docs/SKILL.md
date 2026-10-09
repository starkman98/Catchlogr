---
name: maintain-docs
description: Keep Catchlogr guides accurate when behavior, routes, commands, configuration, or agent workflows change. Use for documentation updates or a targeted documentation drift review.
---

# Maintain Catchlogr documentation

[The documentation map](../../../docs/README.md#guide-ownership) identifies each
guide's owning sources. Use it to select relevant documents; do not load all guides.

- Start from the requested change or suspected inaccurate claim. Inspect its
  executable source, relevant tests, configuration, and callers before editing prose.
- Use the validation helper's documentation suggestions as leads, then follow
  semantic effects beyond those path hints. A guide appearing in the diff does
  not prove its affected claims were reviewed.
- Update the existing owning guide in the same change. For a behavioral change,
  cover the affected command, route, configuration, or failure behavior with
  concrete evidence. For an internal refactor, leave accurate prose alone.
- Separate implemented behavior from planned work. Keep backlog items in the
  roadmap. When evidence conflicts, report the discrepancy; do not change product
  behavior merely to match documentation or invent a guarantee.
- Keep each maintained fact in one place. Replace duplicated explanations with
  links; remove obsolete guides after checking inbound references. Preserve
  useful setup and troubleshooting details that still match current behavior.
- Run documentation validation and relevant behavioral checks for any accompanying
  code change. Link checks establish document structure, not semantic accuracy.

Example request: "Review the documentation affected by this sync change."
Compare ordering, cursor, retry, and photo claims with the changed implementation;
update the sync guide only where those claims are affected.

Report guides reviewed, substantive corrections, checks run, and unresolved
discrepancies. If documented behavior is unchanged, say why briefly rather than
adding a meaningless Markdown edit.
