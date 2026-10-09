# Roadmap

Documentation reconciled with source and CI on 2026-10-08. This records intended
work, not a claim of production readiness. Only mark an item complete after
implementation and relevant verification; keep current behavior in the owning
guide from the [documentation map](README.md).

The implemented baseline is summarized in the [root README](../README.md#status).
Sync limitations are described in [Sync strategy](SYNC_STRATEGY.md#known-limitations).

## Current hardening backlog

- [ ] Define and test an intentional production password policy.
- [ ] Add API rate limiting and provider-aware caching for location search.
- [ ] Add weather provider metrics, caching, and rate-limit handling.
- [ ] Add structured centralized logging and operational alerting.
- [ ] Add image compression and lifecycle cleanup for orphaned files.
- [ ] Test SQLite schema upgrades from previously released app versions.
- [ ] Expand API integration coverage beyond identity/authorization-critical paths.
- [ ] Add PostgreSQL-backed constraint and migration verification; current API tests use InMemory.
- [ ] Decide on production object storage and migration from local photo storage.
- [ ] Add pagination before trip/catch collections become large.
- [ ] Add an interactive map-pin picker and optional reverse geocoding.
- [ ] Add user-facing conflict visibility or a stronger merge strategy.
- [ ] Add server deletion tombstones so remote deletes reconcile across devices.
- [ ] Make downloaded row application and cursor advancement atomic; test interruption/cancellation.

## Future product work

### Teams and sharing
- [ ] Team, membership, and invitation domain model.
- [ ] Owner/editor/viewer authorization rules.
- [ ] Shared-trip API and mobile experiences.

### Analytics and export
- [ ] Trip and catch statistics.
- [ ] Species, time, weather, moon, location, and bait analysis.
- [ ] CSV export and printable/PDF trip summaries.

### Production release
- [ ] Production deployment workflow with approval and rollback procedure.
- [ ] Production secret store, backups, and restore drills.
- [ ] Privacy policy, retention/deletion flow, and provider terms review.
- [ ] Accessibility and localization pass.
- [ ] Crash reporting and performance monitoring.
- [ ] Android/iOS signing, store metadata, and release publication.

## Agent workflow follow-up

- [x] Add focused feature-change and sync-review skills shared across the chosen tools.
- [ ] Add environment diagnostics and validation profiles reusing existing test commands.
- [ ] Check project reference boundaries automatically.
- [ ] Confirm Copilot and Claude load shared instructions in the installed clients.
- [ ] Review agent-file deployment exclusions before merging instruction-only changes to dev.
