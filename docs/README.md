# Catchlogr documentation

The root [README](../README.md) is the canonical entry point for local setup.
This index lists the maintained supporting documents.

## Getting started

- [Quick Start](QUICK_START.md) — shortest local startup path
- [Setup Checklist](SETUP_CHECKLIST.md) — environment and smoke-test checklist
- [Mobile Configuration](MOBILE_CONFIGURATION.md) — backend selection, URLs,
  local state isolation, and embedded settings

## Architecture and features

- [Mobile Architecture](MOBILE_ARCHITECTURE.md) — MVVM, local storage,
  authentication, navigation, and project boundaries
- [Sync Strategy](SYNC_STRATEGY.md) — trip/catch ordering, conflict handling,
  weather retry, and private photos
- [Weather Integration](WEATHER_INTEGRATION.md) — server enrichment and offline display
- [Location Search Integration](LOCATION_SEARCH_INTEGRATION.md) — LocationIQ boundary
- [Identity Email](IDENTITY_EMAIL.md) — Resend, confirmation, and password recovery

## Delivery and planning

- [CI/CD](CI-CD.md) — current GitHub Actions and development deployment
- [Roadmap](ROADMAP.md) — implemented baseline and future work

`SETUP_SUMMARY.md` is a compatibility stub for an obsolete Phase 0 snapshot and
is not maintained as an operational guide.

## Documentation maintenance

When code changes commands, routes, configuration, architecture, or user-visible
behavior, update the relevant guide in the same pull request. Treat executable
files—project files, launch settings, endpoint mappings, Compose definitions, and
workflows—as the final source of truth when resolving drift.
