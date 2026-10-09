---
name: review-account-isolation
description: Review or change Catchlogr authentication lifecycle, backend/account switching, logout, resource ownership, or private photo access. Use when a change could mix users' tokens, local data, sync state, or protected resources, or when investigating an isolation failure.
---

# Review account and backend isolation

Identify the affected boundary: API ownership, Mobile account lifecycle, backend
selection, or private photos. Read the affected [scoped instructions](../../../AGENTS.md#scoped-instructions)
and [Identity email guide](../../../docs/IDENTITY_EMAIL.md) only when email/account
flows are involved. Authentication uses Identity bearer/refresh tokens; do not
assume JWT payloads or replace real authentication with test shortcuts.

- For API changes, trace authenticated identity into repository queries and
  nested resource access. A catch's parent trip and private photo must belong to
  the same authorized user. Cover missing/invalid credentials and another user's
  IDs; preserve existing missing/inaccessible resource response behavior.
- For Mobile changes, follow activation, token save/refresh, logout preparation,
  database closure, account changes, and backend changes where affected. Include
  failure halfway through a transition and background work completing afterward.
- Verify the selected backend/account owns the token keys, active SQLite data,
  cursors, pending records, and photo paths it uses. Check cleanup and retry
  behavior rather than assuming clearing the access token clears every resource.
- Use distinct users and, for backend changes, distinct backend scopes with
  overlapping resource IDs. Verify each scope's observable data and credentials
  remain independent without logging tokens or private storage details.

Reuse the [API auth/ownership tests](../../../tests/Catchlogr.Api.IntegrationTests/AuthenticationAndAuthorizationTests.cs)
and relevant Mobile tests for SecureTokenStore, AuthenticationMessageHandler,
LogoutService, LocalDatabaseAccountIsolation, and PhotoCaptureServiceAccountIsolation.
Read test scope rules before changing fixtures. Use
[Validation](../../../docs/VALIDATION.md) for the affected suites and device limits.

Report specific isolation paths checked, reproducible failures, and coverage gaps.
For review-only requests, report findings before proposing product changes;
implement only fixes included in the user's task.
