# Catchlogr 🎣

Catchlogr is an offline-first fishing log built with .NET 10. The .NET MAUI
client stores trips and catches in SQLite and synchronizes them with an ASP.NET
Core API backed by PostgreSQL.

## Status

The current development build includes:

- fishing-trip and catch CRUD;
- explicit two-way synchronization;
- private catch-photo capture and synchronization;
- Open-Meteo weather and calculated moon-phase enrichment;
- device location and LocationIQ place search;
- ASP.NET Core Identity registration, email confirmation, bearer/refresh
  tokens, password recovery, and per-user data isolation;
- Razor Pages for confirmation and password-reset links; and
- CI plus automatic deployment of the `dev` branch.

Teams, sharing, analytics, exports, and store publication are not implemented.
See the [roadmap](docs/ROADMAP.md) for the current backlog.

## Architecture

```text
Catchlogr.Mobile
  ├── SQLite (offline source of truth)
  ├── Catchlogr.Sync (trip, catch, weather-retry, and photo sync)
  └── HTTPS + bearer authentication
          ↓
Catchlogr.Api
  ├── Catchlogr.Application
  ├── Catchlogr.Domain
  ├── Catchlogr.Infrastructure
  └── PostgreSQL (cross-device system of record)

Catchlogr.Web ──HTTPS──> Catchlogr.Api
  confirmation and password-reset pages
```

The mobile app never connects directly to PostgreSQL or external providers.
LocationIQ, Open-Meteo, Resend, and photo storage are accessed by the API.

## Repository layout

```text
Catchlogr/
├── src/
│   ├── Catchlogr.Api/               Minimal API and composition root
│   ├── Catchlogr.Application/       Use cases, services, and validation
│   ├── Catchlogr.Contracts/         API request and response contracts
│   ├── Catchlogr.Domain/            Domain entities and interfaces
│   ├── Catchlogr.Infrastructure/    EF Core, providers, email, and storage
│   ├── Catchlogr.Mobile/            .NET MAUI application
│   ├── Catchlogr.Sync/              Platform-neutral synchronization logic
│   └── Catchlogr.Web/               Identity action Razor Pages
├── tests/
│   ├── Catchlogr.Tests/             Unit tests
│   ├── Catchlogr.Api.IntegrationTests/
│   └── Catchlogr.Mobile.Tests/      Windows-targeted MAUI tests
├── deploy/docker/
│   ├── compose.local.yml            Local PostgreSQL
│   └── compose.dev.yml              Development deployment stack
├── docs/
└── Catchlogr.slnx
```

## Prerequisites

- .NET SDK `10.0.302` or a compatible .NET 10 feature band
- Docker with Compose v2
- the .NET MAUI workload for mobile development
- Visual Studio 2022 on Windows, or a compatible .NET/MAUI environment
- Xcode on macOS for iOS and Mac Catalyst
- a LocationIQ access token
- Resend credentials and a verified sender for real email delivery

Install EF Core tools if needed:

```powershell
dotnet tool install --global dotnet-ef
```

## Local setup

### 1. Configure and start PostgreSQL

```powershell
Copy-Item deploy/docker/.env.example deploy/docker/.env
```

Change `POSTGRES_PASSWORD` in the copied file, then start PostgreSQL:

```powershell
docker compose --env-file deploy/docker/.env `
  -f deploy/docker/compose.local.yml up -d
```

The template exposes database `catchlogr_dev` on `localhost:5432` with user
`catchlogr_user`.

### 2. Configure API secrets

The API validates its database, LocationIQ, and email configuration at startup.
Store local values in user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Host=localhost;Port=5432;Database=catchlogr_dev;Username=catchlogr_user;Password=CHANGE_ME" `
  --project src/Catchlogr.Api
dotnet user-secrets set "LocationSearch:LocationIQ:ApiKey" "YOUR_LOCATIONIQ_KEY" `
  --project src/Catchlogr.Api
dotnet user-secrets set "Email:ApiKey" "YOUR_RESEND_KEY" `
  --project src/Catchlogr.Api
dotnet user-secrets set "Email:FromAddress" "account@YOUR_VERIFIED_DOMAIN" `
  --project src/Catchlogr.Api
dotnet user-secrets set "Email:FromName" "Catchlogr" `
  --project src/Catchlogr.Api
dotnet user-secrets set "Email:PublicWebBaseUrl" "https://localhost:7056" `
  --project src/Catchlogr.Api
```

The public web URL must be reachable by the email recipient. A loopback URL is
only suitable when links are opened on the development computer.

### 3. Apply migrations

```powershell
dotnet ef database update `
  --project src/Catchlogr.Infrastructure `
  --startup-project src/Catchlogr.Api
```

### 4. Run API and Web

Run these in separate terminals:

```powershell
dotnet run --project src/Catchlogr.Api --launch-profile https
dotnet run --project src/Catchlogr.Web --launch-profile https
```

| Component | Address |
| --- | --- |
| API HTTPS | `https://localhost:7160` |
| API HTTP | `http://localhost:5001` |
| Swagger UI (Development only) | `https://localhost:7160/swagger` |
| Health check | `https://localhost:7160/health` |
| Web HTTPS | `https://localhost:7056` |

The health endpoint is anonymous. Fishing trips, catches, locations, and photos
require a bearer token.

### 5. Run Mobile

Open `Catchlogr.slnx`, select the `Local` solution configuration, set
`Catchlogr.Mobile` as the startup project, choose a target, and run it.

```powershell
dotnet build src/Catchlogr.Mobile/Catchlogr.Mobile.csproj `
  -t:Run -f net10.0-android -c Local
```

Local Windows uses `https://localhost:7160`; the Android emulator is
automatically redirected to `http://10.0.2.2:5001`. See
[mobile configuration](docs/MOBILE_CONFIGURATION.md) for other targets.

## Tests

```powershell
dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj
dotnet test tests/Catchlogr.Api.IntegrationTests/Catchlogr.Api.IntegrationTests.csproj

# Windows with the MAUI workload
dotnet test tests/Catchlogr.Mobile.Tests/Catchlogr.Mobile.Tests.csproj
```

Linux CI runs the first two suites. Mobile tests are currently a separate
Windows-only check.

## Database commands

```powershell
dotnet ef migrations add MigrationName `
  --project src/Catchlogr.Infrastructure `
  --startup-project src/Catchlogr.Api

dotnet ef database update `
  --project src/Catchlogr.Infrastructure `
  --startup-project src/Catchlogr.Api
```

Dropping a database is destructive. Confirm the selected connection string
before running `dotnet ef database drop`.

## Documentation

Start with the [documentation index](docs/README.md). Development conventions
are defined in [AGENTS.md](AGENTS.md).

## Security

- Never commit passwords, provider keys, connection strings, or tokens.
- Embedded mobile settings are public configuration.
- Production CORS must list explicit origins.
- User-owned API resources are authorized and scoped to the current user.
- Do not log precise coordinates, tokens, or provider URLs containing keys.
