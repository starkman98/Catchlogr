# Mobile.Core

Applies to this directory and descendants, together with the root instructions.

- Keep this project on plain net10.0 with no MAUI dependencies or platform types.
  Put device, Shell, secure-storage, and UI-thread operations behind interfaces;
  implement them in the MAUI shell and register them in MauiProgram.cs.
- ViewModels use injected abstractions, never SQLite or HttpClient directly.
  Use partial-property ObservableProperty syntax and retain preview language mode.
- Preserve backend/account isolation, token refresh, logout cleanup, photo paths,
  UTC storage, and local integer/server GUID boundaries.
- Hosts supply the storage root and configuration assembly/environment explicitly.
  Keep the MAUI Local HTTP exception and existing database filenames unchanged.
- Run the portable Mobile tests using [Validation](../../docs/VALIDATION.md).
  Changes to host contracts also need composition/platform review; passing Core
  tests does not prove Shell query forwarding, XAML bindings, or device behavior.

See [Mobile architecture](../../docs/MOBILE_ARCHITECTURE.md) for boundaries,
[configuration](../../docs/MOBILE_CONFIGURATION.md) for backend selection, and
[sync](../../docs/SYNC_STRATEGY.md) for reconciliation behavior.
