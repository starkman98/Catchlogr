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
- Docker with Compose v2 (rootless Docker is supported on Linux)
- PowerShell 7 (`pwsh`) on Linux for repository validation scripts
- the .NET MAUI workload for mobile development
- an IDE/toolchain supporting the pinned .NET 10 SDK and chosen MAUI target
- Xcode on macOS for iOS and Mac Catalyst
- a LocationIQ access token
- Resend credentials and a verified sender for real email delivery

Install EF Core tools matching the repository's EF Core major version:

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
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

#### Linux server and worktree setup

Keep PostgreSQL credentials outside the checkout so new worktrees can reuse
one file. Run as the development account, without sudo:

```bash
mkdir -p ~/.config/catchlogr
chmod 700 ~/.config/catchlogr
# First-time setup only; do not overwrite an existing configured file.
cp deploy/docker/.env.example ~/.config/catchlogr/postgres.env
chmod 600 ~/.config/catchlogr/postgres.env
```

Edit that file and replace the example password. Mode `600` allows its owner to
read and edit it; other users have no access. From each new worktree's root,
create the ignored link if `deploy/docker/.env` does not already exist:

```bash
ln -s ~/.config/catchlogr/postgres.env deploy/docker/.env

docker compose --env-file ~/.config/catchlogr/postgres.env \
  -f deploy/docker/compose.local.yml up -d
```

The link also satisfies the Compose service's explicit `env_file: .env` entry;
`--env-file` alone supplies interpolation variables and does not replace that
entry. Alternatively, with the link in place:

```bash
cd deploy/docker
docker compose -f compose.local.yml up -d
```

Inspect and control the existing container from any directory:

```bash
docker ps -a
docker logs --tail 100 -f catchlogr-postgres
docker stop catchlogr-postgres
docker start catchlogr-postgres
```

Use Compose again after configuration changes. Changing `POSTGRES_PASSWORD`
does not change the password of a database already initialized in the volume.

For rootless Docker, use the development account's rootless context; do not use
`sudo docker`, which may select another daemon. Inspect it with:

```bash
docker context show
docker info --format '{{json .SecurityOptions}}'
systemctl --user is-active docker
loginctl show-user "$USER" -p Linger
```

Security options should include `name=rootless`. Enable the user service and
lingering when it must survive logout and start at boot. A sandbox may deny
socket or network access even when the daemon works; request supported approved
execution rather than changing socket permissions or disabling safeguards.

The local Compose file fixes the container name and host port. Use one shared
PostgreSQL service across worktrees. For concurrent schema work, provision a
separate disposable database per worktree and override its connection string
for that process; do not run competing migrations against a shared database.
Concurrent API/Web instances also need distinct listening ports.

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

#### User secrets across machines and worktrees

Visual Studio's Manage User Secrets uses the same .NET secret store as the CLI.
The API's committed `UserSecretsId` is
`b4bcec11-2678-4d51-8359-ad9ef0a0369e`. Its file locations are:

- Windows: `%APPDATA%\Microsoft\UserSecrets\b4bcec11-2678-4d51-8359-ad9ef0a0369e\secrets.json`
- Linux: `~/.microsoft/usersecrets/b4bcec11-2678-4d51-8359-ad9ef0a0369e/secrets.json`

Worktrees with the same project ID running under the same OS account reuse this
store automatically in Development. No secret copy or project initialization is
needed for a new worktree. A different machine or account needs its own store.
The Web project has a separate ID; API secrets do not automatically configure it.

To migrate development secrets from Windows, transfer the API file privately
using SSH/SFTP, keeping it outside the checkout. Adapt the server database
connection string to match its local PostgreSQL credentials and select an email
Web URL reachable from the recipient's browser. Never copy production credentials
as a shortcut or paste secret values into agent conversations or logs.

On Linux, protect the store as its owning development account:

```bash
chmod 700 ~/.microsoft/usersecrets/b4bcec11-2678-4d51-8359-ad9ef0a0369e
chmod 600 ~/.microsoft/usersecrets/b4bcec11-2678-4d51-8359-ad9ef0a0369e/secrets.json
```

User Secrets and Docker env files are plaintext development configuration, not
encrypted vaults. Filesystem permissions restrict other accounts, but agents
running as the owner can read them. Verify required key presence without printing
values. The API does not automatically load Docker's env file; its connection
string must be configured separately using the settings above. For isolated
worktree databases, a process-specific `ConnectionStrings__DefaultConnection`
environment variable overrides the shared user-secret value.

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

## Verification

After first-time setup, check API health, register and confirm an account, sign
in, save a trip/catch offline, then reconnect and sync. Check that switching
accounts does not expose another account's local rows or photos. This smoke
check needs a reachable Web origin and configured email delivery.

Use [Validation](docs/VALIDATION.md) for focused test commands and limitations.
Pull-request CI runs unit/API suites on Linux, Mobile tests on Windows, and
documentation validation. Routine test runs do not need real provider keys or
a local PostgreSQL instance; running the application does.

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
