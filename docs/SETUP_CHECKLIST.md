# Catchlogr development setup checklist

Use this after following the [root README](../README.md).

## Tooling

- [ ] `dotnet --version` resolves SDK 10.0.302 or a compatible .NET 10 SDK.
- [ ] `docker compose version` succeeds.
- [ ] `dotnet ef --version` succeeds.
- [ ] The .NET MAUI workload is installed for mobile development.
- [ ] Android SDK, Xcode, or Windows App SDK tooling is installed for the chosen target.

## Repository and local database

- [ ] `Catchlogr.slnx` opens successfully.
- [ ] `deploy/docker/.env.example` was copied to `deploy/docker/.env`.
- [ ] The placeholder PostgreSQL password was replaced locally.
- [ ] PostgreSQL starts with:

  ```powershell
  docker compose --env-file deploy/docker/.env `
    -f deploy/docker/compose.local.yml up -d
  ```

- [ ] `docker compose ... ps` reports `catchlogr-postgres` as healthy.

## API configuration

- [ ] `ConnectionStrings:DefaultConnection` is stored in API user secrets.
- [ ] `LocationSearch:LocationIQ:ApiKey` is stored in API user secrets.
- [ ] All four `Email` settings are stored in API user secrets.
- [ ] No real secret was added to an `appsettings*.json` or `.env.example` file.
- [ ] Migrations apply with:

  ```powershell
  dotnet ef database update `
    --project src/Catchlogr.Infrastructure `
    --startup-project src/Catchlogr.Api
  ```

- [ ] `dotnet run --project src/Catchlogr.Api --launch-profile https` starts.
- [ ] `https://localhost:7160/health` returns healthy.
- [ ] `https://localhost:7160/swagger` opens in Development.

## Web and identity

- [ ] `dotnet run --project src/Catchlogr.Web --launch-profile https` starts.
- [ ] `Email:PublicWebBaseUrl` points to the Web origin, not the API origin.
- [ ] Registration sends a confirmation message using the configured sender.
- [ ] Confirmation and password-reset links open the Web application.
- [ ] Login succeeds only after confirmation under the default development policy.

## Mobile

- [ ] `Local`, `Debug`, or `Release` was selected intentionally.
- [ ] Local Windows reaches `https://localhost:7160`.
- [ ] The Android emulator reaches `http://10.0.2.2:5001`.
- [ ] A physical device uses a reachable LAN URL or the Development backend.
- [ ] Registration/login, offline trip creation, catch creation, and sync work.
- [ ] Switching accounts does not expose another account's local rows or photos.

## Tests

- [ ] Platform-neutral tests pass:

  ```powershell
  dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj
  dotnet test tests/Catchlogr.Api.IntegrationTests/Catchlogr.Api.IntegrationTests.csproj
  ```

- [ ] On Windows with MAUI installed, mobile tests pass:

  ```powershell
  dotnet test tests/Catchlogr.Mobile.Tests/Catchlogr.Mobile.Tests.csproj
  ```

## Before committing

- [ ] `git status --short` contains no secrets, local databases, or generated output.
- [ ] Documentation was updated for changed commands, routes, configuration, or behavior.
- [ ] The relevant focused tests and regression suites were run.

See [mobile configuration](MOBILE_CONFIGURATION.md), [sync strategy](SYNC_STRATEGY.md),
and [identity email](IDENTITY_EMAIL.md) for troubleshooting details.
