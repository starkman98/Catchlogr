# API

Applies to this directory and descendants, together with the root instructions.

- Keep Minimal API handlers thin. Put business rules in Application services;
  wire server dependencies and endpoint groups in [Program.cs](Program.cs).
- Validate request DTOs with FluentValidation or focused helpers. Preserve
  meaningful errors: 201 creation, 400 validation, 404 missing or inaccessible
  resources, and 409 conflicts where applicable.
- Preserve ASP.NET Core Identity bearer/refresh-token authentication; tokens
  must not be assumed to be JWTs. Keep protected routes authorized.
- Preserve ownership checks for trips, catches, and private photos. Check both
  direct resource access and nested routes; never trust a submitted owner ID.
- Keep DTOs in Contracts separate from domain entities. Review Mobile and Sync
  clients when changing routes, responses, or modifiedSince behavior.

Start in Endpoints for routes, Authentication for the current-user adapter, and
Infrastructure for exception-to-response handling. For account/email flows,
read [Identity email](../../docs/IDENTITY_EMAIL.md); Web owns its Razor pages.

Run affected API integration tests and relevant service/validator unit tests
using [Validation](../../docs/VALIDATION.md). Cover unauthenticated and
cross-account requests when changing authorization or resource access.
