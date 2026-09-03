# Catchlogr CI/CD

This document describes the current and recommended CI/CD flow for Catchlogr, with `dev` as the automatically deployed development branch and `main` as the future production branch.

## Goals

The pipeline should provide these guarantees:

- Feature work is developed outside `dev`.
- Pull requests into `dev` must pass automated tests before they can be merged.
- A successful merge into `dev` automatically deploys the exact `origin/dev` commit to the development server.
- A failed test must never trigger a deployment.
- Server secrets and persistent data remain outside Git.
- Database migrations are applied explicitly during deployment, before new application containers are started.
- Production can later use the same model with stronger protection and manual approval.

---

# Repository layout

Recommended relevant structure:

```text
Catchlogr/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── deploy-dev.yml
│
├── deploy/
│   └── docker/
│       └── compose.dev.yml
│
├── src/
├── tests/
└── ...
```

The deployment server uses:

```text
/opt/catchlogr/
├── compose.yml          # copied from repo by deployment pipeline
├── .env                 # server-owned secrets; never committed
├── data/
│   └── photos/          # persistent photo data
└── app/                 # Git clone of Catchlogr repo
```

Docker's named PostgreSQL volume is also persistent and is not stored in Git.

---

# Branch model

Recommended development flow:

```text
feature/*
    │
    ▼
Pull Request
    │
    ▼
dev
    │
    ▼
Automatic dev deployment
```

Later, production can use:

```text
dev
 │
 ▼
Pull Request
 │
 ▼
main
 │
 ▼
Production approval
 │
 ▼
Production deployment
```

## Suggested meanings

### Feature branches

Used for active development.

Examples:

```text
feature/authentication
feature/web-dashboard
fix/photo-upload
```

Feature branches are not automatically deployed.

### `dev`

Represents code that has passed CI and is suitable for the persistent Catchlogr development environment.

A successful change to `dev` automatically deploys to:

```text
https://dev.catchlogr.com
https://dev-api.catchlogr.com
```

### `main`

Future production branch.

Eventually, changes to `main` should deploy to production only after stronger checks and preferably a manual environment approval.

---

# CI workflow

The PR CI workflow runs before code is allowed into `dev`.

Recommended file:

```text
.github/workflows/ci.yml
```

Example:

```yaml
name: CI

on:
  pull_request:
    branches:
      - dev

jobs:
  test:
    runs-on: [self-hosted, linux, x64, catchlogr]

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.302

      - name: Restore tests
        run: |
          dotnet restore tests/Catchlogr.Tests/Catchlogr.Tests.csproj

      - name: Build tests
        run: |
          dotnet build tests/Catchlogr.Tests/Catchlogr.Tests.csproj \
            --configuration Release \
            --no-restore

      - name: Test
        run: |
          dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj \
            --configuration Release \
            --no-build
```

The current Catchlogr test suite does not require PostgreSQL. Repository/database dependencies are mocked and tests that need files use temporary storage.

The CI runner therefore only needs to build and execute the test project.

---

# Development deployment workflow

Recommended file:

```text
.github/workflows/deploy-dev.yml
```

The development deployment workflow should trigger when `dev` changes:

```yaml
on:
  push:
    branches:
      - dev
  workflow_dispatch:
```

The workflow should contain two jobs:

```text
test
 ↓
deploy
```

The deploy job uses:

```yaml
needs: test
```

Therefore a failed test prevents deployment.

The deployment job should reference the GitHub Environment:

```yaml
environment:
  name: development
  url: https://dev.catchlogr.com
```

---

# Successful development deployment

When a Pull Request is merged into `dev`:

## 1. GitHub updates `dev`

The merge creates a new commit on the `dev` branch.

That generates a `push` event.

## 2. `deploy-dev.yml` starts

The self-hosted Catchlogr runner receives the workflow.

## 3. Tests run again

The deployment workflow restores, builds and tests Catchlogr.

This is intentionally redundant with PR CI.

PR CI protects the merge.

Deployment CI protects the deployment.

If these tests fail, the server is not touched.

## 4. Deployment job starts

Only after the `test` job succeeds.

## 5. Compose definition is copied

The version-controlled file:

```text
deploy/docker/compose.dev.yml
```

is copied to:

```text
/opt/catchlogr/compose.yml
```

The deployed copy should not be manually edited.

The repo is the source of truth.

## 6. Pipeline connects to the dev server with SSH

The pipeline enters:

```text
/opt/catchlogr/app
```

and performs:

```bash
git fetch origin
git checkout dev
git reset --hard origin/dev
```

This guarantees that the server source exactly matches GitHub's current `dev` branch.

## 7. Docker images are built

From:

```text
/opt/catchlogr
```

the pipeline executes:

```bash
docker compose build
```

This builds the new API/Web/migration images without replacing the currently running application containers.

## 8. Database migrations are applied

The pipeline executes:

```bash
docker compose --profile tools run --rm migrate
```

The migration container:

- uses the SDK-based migration image;
- contains `dotnet-ef`;
- connects to the development PostgreSQL database;
- applies only pending EF Core migrations;
- exits when finished.

Migrations do not run automatically on every API startup.

## 9. Application containers are updated

After migrations succeed:

```bash
docker compose up -d
```

Docker recreates services whose image/configuration changed.

Persistent resources remain:

- PostgreSQL named volume;
- `/opt/catchlogr/data/photos`;
- `/opt/catchlogr/.env`.

## 10. Deployment is verified

The workflow calls:

```text
https://dev-api.catchlogr.com/health
```

If the endpoint responds successfully, GitHub marks the deployment successful.

---

# What happens when something fails?

## CI restore/build/test failure before deployment

Example:

```text
dotnet test ❌
```

Result:

```text
deploy job does not start
development server remains on previous version
```

This is the safest failure mode.

The bad commit may exist on `dev` if this happened during the post-merge deployment workflow, but it is not deployed.

PR CI should normally catch this before merge.

---

## SCP failure

If copying `compose.dev.yml` fails:

```text
tests ✅
scp ❌
```

The deployment stops.

Existing containers continue running.

---

## SSH failure

If the runner cannot connect to `catchlogr-dev`, deployment stops.

Existing containers continue running.

Typical causes:

- SSH key problem;
- host unavailable;
- networking/firewall problem;
- wrong deployment address.

---

## Git fetch/reset failure

The remote script uses:

```bash
set -e
```

Therefore failure of commands such as:

```bash
git fetch origin
```

stops the deployment immediately.

The existing Docker containers continue running.

---

## Docker build failure

If:

```bash
docker compose build
```

fails, the existing running containers are normally unaffected.

The new application version is not started.

Example:

```text
old app ✅ running
new image ❌ failed to build
```

---

## Migration failure

If:

```bash
docker compose --profile tools run --rm migrate
```

fails:

```text
docker compose up -d
```

is not executed because the script uses `set -e`.

The old application containers therefore remain running.

However, database deployment requires more care:

```text
Migration A ✅
Migration B ✅
Migration C ❌
```

Previously completed migrations remain applied even though the overall deployment failed.

A migration that fails transactionally is normally rolled back itself where the database/provider supports it.

This means:

```text
failed deployment != guaranteed unchanged database
```

For development this is acceptable.

For production, migrations should be designed to remain compatible with the previous application version whenever possible.

---

## `docker compose up -d` failure

At this point:

```text
source updated ✅
images built ✅
database migrated ✅
container startup ❌
```

The development environment may be partially updated or unavailable.

Inspect:

```bash
docker compose ps
docker compose logs api
docker compose logs web
```

There is currently no automatic rollback.

---

## Health-check failure

The health check happens after deployment.

Therefore:

```text
health check ❌
```

does NOT mean the deployment was rolled back.

It means:

```text
deployment happened
verification failed
```

The new containers may be:

- running but unhealthy;
- partially functioning;
- unreachable through nginx;
- unable to reach the database;
- failing during startup.

Inspect logs and container state.

Automatic rollback is not necessary for the current dev environment.

---

# Pull Request vs direct push to `dev`

The deployment workflow reacts to:

```yaml
push:
  branches:
    - dev
```

Therefore both of these ultimately trigger the same deployment:

## Direct push

```text
local dev
 ↓
git push origin dev
 ↓
Deploy Dev workflow
```

## Pull Request

```text
feature/*
 ↓
PR into dev
 ↓
merge
 ↓
dev changes
 ↓
Deploy Dev workflow
```

Opening a PR does not deploy.

Merging the PR changes `dev`, which triggers deployment.

The advantage of a PR is that CI can run before the merge and GitHub can block the merge when tests fail.

---

# Recommended GitHub rules for `dev`

For a solo-maintained Catchlogr repository, keep protection useful but lightweight.

Recommended:

- Require a Pull Request before merging into `dev`.
- Require CI `test` status check to pass.
- Do not require another person's approval.
- Block force pushes.
- Do not require manual approval for the `development` Environment.
- Allow successful merges into `dev` to deploy automatically.

This creates:

```text
feature branch
 ↓
PR
 ↓
CI tests
 ├─ fail → merge blocked
 └─ pass
      ↓
    merge
      ↓
    dev
      ↓
 automatic development deployment
```

---

# Recommended GitHub Environment settings — `development`

The `development` GitHub Environment represents the persistent dev deployment.

Recommended now:

```text
Required reviewers: none
Wait timer: none
Deployment branches: dev only
```

There is little value in making the sole developer manually approve every dev deployment.

The environment still provides:

- deployment history;
- environment-specific configuration;
- a clear distinction from future production;
- a place for environment-specific secrets/variables if needed later.

---

# Future production rules

When production is introduced, use stronger controls.

Suggested:

```text
feature/*
 ↓
PR
 ↓
dev
 ↓
automatic dev deployment
 ↓
validation
 ↓
PR dev → main
 ↓
CI
 ↓
merge
 ↓
production environment approval
 ↓
production deployment
```

Recommended `main` protections:

- Require Pull Request.
- Require CI status checks.
- Block force pushes.
- Prefer no direct pushes.
- Optional required review if another contributor joins the project.

Recommended `production` Environment:

- manual approval before deployment;
- production-only secrets;
- branch restriction to `main`;
- optionally prevent self-review once multiple maintainers exist.

---

# Secrets and deployment ownership

## Stored in Git

Safe to version:

```text
deploy/docker/compose.dev.yml
Dockerfiles
workflow files
environment variable names
health checks
migration commands
```

## Stored on the dev server

Never commit:

```text
/opt/catchlogr/.env
database passwords
Resend API key
other provider API keys
persistent photo data
PostgreSQL database files
```

The pipeline copies deployment definitions but does not replace `.env` or persistent data.

---

# Useful server commands

Check deployment state:

```bash
cd /opt/catchlogr
docker compose ps
```

API logs:

```bash
docker compose logs -f --tail 100 api
```

Web logs:

```bash
docker compose logs -f --tail 100 web
```

PostgreSQL logs:

```bash
docker compose logs -f --tail 100 db
```

Run migrations manually:

```bash
docker compose --profile tools run --rm migrate
```

Build without starting:

```bash
docker compose build
```

Start/update services:

```bash
docker compose up -d
```

Verify API:

```bash
curl --fail https://dev-api.catchlogr.com/health
```

---

# Current recommended Catchlogr workflow

```text
Developer
   │
   ▼
feature branch
   │
   ▼
Pull Request → dev
   │
   ▼
CI
restore → build → tests
   │
   ├── FAIL
   │    └── fix feature branch
   │
   └── PASS
        │
        ▼
      Merge
        │
        ▼
       dev
        │
        ▼
Deploy Dev
tests → copy compose → sync source → build → migrate → start → health
        │
        ├── FAIL
        │    └── inspect failed stage / logs
        │
        └── PASS
             │
             ▼
      dev.catchlogr.com
      dev-api.catchlogr.com
```

---

# Principles

1. `dev` should represent deployable development code.
2. Feature branches should not deploy automatically.
3. Tests gate merges and deployments.
4. Deployment configuration belongs in Git.
5. Secrets and persistent state do not belong in Git.
6. Database migrations are explicit deployment operations.
7. Development deployment is automatic after a valid merge.
8. Production deployment should eventually require stronger protection than development.
9. Failed verification does not imply automatic rollback.
10. Keep the process simple while Catchlogr is maintained by one developer; add governance when the project/team actually needs it.
