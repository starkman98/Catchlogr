# Mobile app configuration

## Backend selection

Catchlogr Mobile selects its backend through the Visual Studio build
configuration. Select the configuration before starting the mobile project.

| Build configuration | Settings resource | API | SQLite file |
| --- | --- | --- | --- |
| Local | `appsettings.Local.json` | API on the developer PC | `catchlogr_local.db3` |
| Debug | `appsettings.Development.json` | `https://dev-api.catchlogr.com` | `catchlogr_dev.db3` |
| Release | `appsettings.json` | `https://api.catchlogr.com` | `catchlogr.db3` |

`AppSettings.Load()` loads exactly one embedded resource. It does not fall
back to another environment. Startup fails if the resource is missing, its
declared `BackendEnvironment` does not match the build, or a non-Local API
URL does not use HTTPS.

Changes to these JSON files require rebuilding the mobile app because the
files are embedded in the application assembly.

## Local API addresses

The API HTTPS launch profile listens at `https://localhost:7160` and also
exposes HTTP at port `5001`.

For a Local build, the mobile app resolves the endpoint as follows:

| Platform | API address |
| --- | --- |
| Windows | `https://localhost:7160` |
| Android emulator | `http://10.0.2.2:5001` |
| iOS simulator / Mac Catalyst | Configured Local URL, normally `https://localhost:7160` |
| Physical device | Configured Local URL |

`10.0.2.2` is the Android emulator alias for the host computer. Android
cleartext access is restricted to that host by
`Platforms/Android/Resources/xml/network_security_config.xml`.

To connect a physical device directly to the local API, temporarily set
`Api:BaseUrl` in `appsettings.Local.json` to the PC's LAN address, ensure
the API listens on the LAN interface, and rebuild. For routine physical-device
development, prefer the Debug configuration and the deployed development API.

## Local state isolation

Each backend has a different SQLite filename. Authentication values in
SecureStorage are also prefixed with the selected backend:

```text
catchlogr.local.auth.*
catchlogr.development.auth.*
catchlogr.production.auth.*
```

Switching configurations therefore does not reuse tokens, user metadata,
dirty rows, or sync cursors from another backend. Existing unscoped
`catchlogr.auth.*` tokens are intentionally not migrated; users sign in once
per backend after upgrading.

## Identity email links

Mobile backend selection does not configure Identity email links. The API uses
`Email:PublicWebBaseUrl`, which must be the externally reachable Catchlogr Web
origin rather than the API origin. The Web app receives confirmation/reset
parameters and calls the API over HTTPS.

Typical values are:

```text
Local Web:       https://localhost:7056
Development Web: https://dev.catchlogr.com
Production Web:  https://catchlogr.com
```

Use the real production origin when it is finalized. For deployed environments,
the environment-variable form is `Email__PublicWebBaseUrl`. See
[Identity email and password recovery](IDENTITY_EMAIL.md).

## Security

Embedded mobile settings are public application configuration. Never store
passwords, API keys, tokens, connection strings, or other secrets in these
files. Tokens belong in platform SecureStorage; server secrets belong in API
environment variables or an appropriate secret store.
