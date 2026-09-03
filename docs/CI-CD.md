# CI/CD

Catchlogr uses two GitHub Actions workflows on a self-hosted Linux runner labelled
`self-hosted`, `linux`, `x64`, and `catchlogr`.

## Branch flow

```text
feature/* ──pull request──> dev ──automatic deployment──> development
                               └──pull request──> main (future production line)
```

The CI workflow runs for pull requests targeting `dev` or `main`. A dedicated
job rejects a pull request to `main` unless its source branch is `dev`.

## Pull-request CI

`.github/workflows/ci.yml` currently runs:

1. `unit-tests`
   - restores `tests/Catchlogr.Tests`;
   - builds it in Release; and
   - runs it without rebuilding.
2. `integration-tests`
   - restores `tests/Catchlogr.Api.IntegrationTests`;
   - builds it in Release; and
   - runs it without rebuilding.
3. `validate-main-source`
   - runs only for a pull request targeting `main`;
   - requires the source branch to be `dev`.

The API integration suite replaces PostgreSQL with EF Core InMemory, Resend with
an in-memory sender, and photo storage with an isolated temporary directory. CI
therefore does not require a PostgreSQL service.

`tests/Catchlogr.Mobile.Tests` targets Windows/MAUI and is not executed by the
Linux workflow. Run it on a configured Windows machine until a Windows CI job is
added.

The self-hosted runner supplies the pinned .NET SDK; workflows verify it with
`dotnet --info` rather than installing it on every job.

## Development deployment

`.github/workflows/deploy-dev.yml` runs on pushes to `dev` and manual dispatch.
It has the following dependency chain:

```text
unit-tests ───────┐
                  ├──> deploy
integration-tests┘
```

The deployment job:

1. copies `deploy/docker/compose.dev.yml` to
   `/opt/catchlogr/compose.yml` on the development server;
2. connects to the server over SSH;
3. updates `/opt/catchlogr/app` to `origin/dev`;
4. builds API and Web container images;
5. runs the Compose `migrate` profile;
6. starts/updates the stack; and
7. verifies `https://dev-api.catchlogr.com/health` with retries.

The GitHub Environment is `development` with URL `https://dev.catchlogr.com`.
The workflow concurrency group `catchlogr-dev` allows only one development
deployment at a time and does not cancel an in-progress deployment.

## Development server layout

```text
/opt/catchlogr/
├── compose.yml          copied from the repository
├── .env                 server-owned secrets and configuration
├── app/                 repository checkout
└── data/photos/         persistent private photo data
```

PostgreSQL data lives in the Docker named volume `postgres_data`.

The server-owned `.env` supplies at least:

- `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD`;
- `LocationSearch__LocationIQ__ApiKey`;
- `Email__ApiKey`, `Email__FromAddress`, `Email__FromName`, and
  `Email__PublicWebBaseUrl`.

Secrets, PostgreSQL files, and private photos must not be copied back into Git.

## Failure behavior

- Test failures prevent the deployment job from starting.
- A failed file copy or SSH connection leaves the running stack unchanged.
- A failed source update, image build, or migration stops the remote script
  because it uses `set -e`.
- Compose builds do not replace running containers until `docker compose up -d`.
- A failed health check marks the workflow failed but does not automatically roll
  back containers or database migrations.

Database migrations must therefore remain forward-compatible with the currently
running application during deployment. Production deployment needs an explicit
backup and rollback strategy before it is enabled.

## Useful server commands

```bash
cd /opt/catchlogr
docker compose ps
docker compose logs -f --tail 100 api
docker compose logs -f --tail 100 web
docker compose logs -f --tail 100 db
docker compose --profile tools run --rm migrate
curl --fail https://dev-api.catchlogr.com/health
```

## Repository and environment ownership

Safe to version:

- Compose and Dockerfiles;
- workflow definitions;
- environment-variable names and safe templates;
- health-check and migration commands.

Owned outside Git:

- runner/server SSH credentials;
- provider and email keys;
- database passwords;
- private photo content;
- environment protection and branch rules.

The workflow files are the executable source of truth. Update this document in
the same pull request whenever job names, triggers, deployment paths, or service
dependencies change.
