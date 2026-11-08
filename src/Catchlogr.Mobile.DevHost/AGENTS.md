# Mobile.DevHost

Applies to this directory and descendants, together with the root instructions.

- Keep this console host on plain net10.0. Reference Mobile.Core and its shared
  service registration; do not reference MAUI, API, or Infrastructure projects.
- Keep the CLI restricted to loopback HTTP/HTTPS origins. Do not bypass HTTPS
  validation, follow redirects, or accept credential command-line arguments.
- Use memory-only secrets and per-run temporary SQLite. Close the database and
  remove local state on handled failure/cancellation; report cleanup failures.
- The scenario creates a real server trip. Preserve explicit read-back checks;
  Sync can catch an upload failure without making the overall call throw.
- Keep fixed GPS and UI/camera stubs explicit. This host does not prove device
  behavior or replace the app's persistent offline storage semantics.

[Mobile architecture](../../docs/MOBILE_ARCHITECTURE.md#console-devhost) owns
usage, side effects, gains, and risks. Run affected Mobile and API integration
suites using [Validation](../../docs/VALIDATION.md#devhost-validation).
