# Catchlogr quick start

This is the short path for an already provisioned development machine. For
prerequisites and explanations, use the [root README](../README.md).

## First-time configuration

From the repository root:

```powershell
Copy-Item deploy/docker/.env.example deploy/docker/.env
```

Change `POSTGRES_PASSWORD` in `deploy/docker/.env`, then set the matching API
connection string and the required provider/email secrets:

```powershell
dotnet user-secrets set ConnectionStrings:DefaultConnection `
  Host=localhost;Port=5432;Database=catchlogr_dev;Username=catchlogr_user;Password=CHANGE_ME `
  --project src/Catchlogr.Api
dotnet user-secrets set LocationSearch:LocationIQ:ApiKey YOUR_LOCATIONIQ_KEY `
  --project src/Catchlogr.Api
dotnet user-secrets set Email:ApiKey YOUR_RESEND_KEY `
  --project src/Catchlogr.Api
dotnet user-secrets set Email:FromAddress account@YOUR_VERIFIED_DOMAIN `
  --project src/Catchlogr.Api
dotnet user-secrets set Email:FromName Catchlogr `
  --project src/Catchlogr.Api
dotnet user-secrets set Email:PublicWebBaseUrl https://localhost:7056 `
  --project src/Catchlogr.Api
```

## Start the local stack

```powershell
docker compose --env-file deploy/docker/.env `
  -f deploy/docker/compose.local.yml up -d

dotnet ef database update `
  --project src/Catchlogr.Infrastructure `
  --startup-project src/Catchlogr.Api

dotnet run --project src/Catchlogr.Api --launch-profile https
```

In a second terminal:

```powershell
dotnet run --project src/Catchlogr.Web --launch-profile https
```

Verify the API:

```powershell
Invoke-RestMethod https://localhost:7160/health
```

Swagger is available at `https://localhost:7160/swagger` in Development.

## Run Mobile

Open `Catchlogr.slnx`, select the `Local` configuration, choose
`Catchlogr.Mobile` as the startup project, and run the required target.

The Android emulator is automatically redirected to `http://10.0.2.2:5001`.
Windows uses `https://localhost:7160`. Physical devices need a reachable LAN URL
or the deployed Development backend.

## Common commands

```powershell
# Stop PostgreSQL without deleting data
docker compose --env-file deploy/docker/.env `
  -f deploy/docker/compose.local.yml down

# View PostgreSQL logs
docker compose --env-file deploy/docker/.env `
  -f deploy/docker/compose.local.yml logs -f postgres

# Run platform-neutral tests
dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj
dotnet test tests/Catchlogr.Api.IntegrationTests/Catchlogr.Api.IntegrationTests.csproj
```

See [mobile configuration](MOBILE_CONFIGURATION.md),
[identity email](IDENTITY_EMAIL.md), and
[location search](LOCATION_SEARCH_INTEGRATION.md) for integration details.
