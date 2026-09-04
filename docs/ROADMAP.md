# Catchlogr roadmap

Last reconciled with the codebase: 2026-09-04.

This roadmap separates the implemented baseline from future work. Checked items
exist in the repository; they are not a claim that production hardening or store
release is complete.

## Guiding principles

- Mobile works offline against account-scoped SQLite.
- The authenticated API and PostgreSQL are the cross-device system of record.
- Synchronization is explicit, dependency ordered, and retryable.
- Domain entities and API contracts remain separate.
- Server integrations and secrets never move into the mobile client.

## Implemented baseline

### Architecture and development environment

- [x] .NET 10 solution with Domain, Application, Contracts, Infrastructure, API,
      Sync, Mobile, and Web projects
- [x] PostgreSQL through EF Core migrations
- [x] Local and development Docker Compose definitions
- [x] Local/Development/Production mobile backend configurations
- [x] Minimal APIs, FluentValidation, problem details, health checks, and Swagger
- [x] Unit, API integration, and Windows-targeted MAUI test projects

### Fishing trips and catches

- [x] Authenticated, user-scoped trip CRUD
- [x] Authenticated, user-scoped catch CRUD
- [x] Offline SQLite entities and repositories
- [x] Trip-before-catch sync orchestration
- [x] UTC timestamps, dirty flags, entity cursors, and last-write-wins conflicts
- [x] Trip list/details and catch list/add/edit mobile flows
- [x] Bait, measurement, note, time, and coordinate fields

### Location, weather, and moon phase

- [x] Device-location capture
- [x] Server-proxied LocationIQ search with water-feature ranking
- [x] Open-Meteo current, historical-forecast, and archive selection
- [x] Server-owned weather snapshot synchronized for offline display
- [x] Best-effort weather retry during later sync
- [x] Server-calculated moon phase with mobile presentation

### Private catch photos

- [x] Camera/gallery capture and private local storage
- [x] Authenticated upload at `/api/catches/{catchId}/photos`
- [x] Authorized download and deletion at `/api/photos/{photoId}`
- [x] Offline download cache, retry-safe upload state, replacement, and deletion
- [x] Local filesystem storage provider and persistence metadata

### Identity and account isolation

- [x] ASP.NET Core Identity with EF Core stores
- [x] Registration, login, bearer access tokens, and refresh tokens
- [x] Required email confirmation and login lockout policy
- [x] Resend transactional email integration
- [x] Forgot/reset password and resend-confirmation flows
- [x] Razor Pages Web app for confirmation and reset links
- [x] Authorization on trips, catches, location search, and photos
- [x] Per-user PostgreSQL queries and per-account local data/photo isolation
- [x] Logout flow with pending-data handling

### Delivery

- [x] Pull-request CI for `dev` and `main`
- [x] Platform-neutral unit and API integration test jobs
- [x] Restriction that only `dev` may merge into `main`
- [x] Automatic `dev` deployment through Docker Compose
- [x] Automated EF migration step and post-deployment health check

## Current hardening backlog

- [x] Add a Windows CI job for `Catchlogr.Mobile.Tests`
- [x] Add documentation link/path validation to CI
- [ ] Define and test an intentional production password policy
- [ ] Add API rate limiting and provider-aware caching for location search
- [ ] Add structured centralized logging and operational alerting
- [ ] Add image compression and lifecycle cleanup for orphaned files
- [ ] Test SQLite schema upgrades from previously released app versions
- [ ] Expand API integration coverage beyond identity/authorization-critical paths
- [ ] Decide on production object storage and migration from local photo storage
- [ ] Add pagination before trip/catch collections become large
- [ ] Add an interactive map-pin picker and optional reverse geocoding
- [ ] Add user-facing conflict visibility or a stronger merge strategy
- [ ] Add server deletion tombstones so remote deletes reconcile across devices

## Future product work

### Teams and sharing

- [ ] Team, membership, and invitation domain model
- [ ] Owner/editor/viewer authorization rules
- [ ] Shared-trip API and mobile experiences

### Analytics and export

- [ ] Trip and catch statistics
- [ ] Species, time, weather, moon, location, and bait analysis
- [ ] CSV export
- [ ] Printable/PDF trip summaries

### Production release

- [ ] Production deployment workflow with approval and rollback procedure
- [ ] Production secret store, backups, and restore drills
- [ ] Privacy policy, retention/deletion flow, and provider terms review
- [ ] Accessibility and localization pass
- [ ] Crash reporting and performance monitoring
- [ ] Android/iOS signing, store metadata, and release publication

## Future ideas

- Social sharing and leaderboards
- Tide predictions and regulatory information
- Fish-finder integrations
- Recommendation models based on private user history
- Widgets and faster field-entry workflows

## Success criteria

- Offline edits survive restarts and synchronize without account data leakage.
- A user can record trips, catches, location, weather context, bait, and photos.
- Provider outages do not prevent core logging workflows.
- Production releases have monitored migrations, backups, and rollback procedures.
