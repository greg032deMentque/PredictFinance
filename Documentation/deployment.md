# Deployment

The repo pipelines (see [ci-cd.md](ci-cd.md)) **build and publish artifacts**; the actual deploy to the host is configured in Azure DevOps (release/environment settings) and is **not** in the repo. There is no Dockerfile, Terraform, or Kubernetes manifest — deployment targets an App Service / server that consumes the published artifact.

## Environments

| Environment | Source branch | Config source | Notes |
|---|---|---|---|
| Development | local | `appsettings.Development.json` (gitignored) + user-secrets | your machine |
| Staging | `dev` | host environment variables | mirrors prod |
| Production | `master` | host environment variables | live |

## Artifacts

- **Backend**: `dotnet publish` self-contained `linux-x64`. Local `appsettings.*.json` are stripped from the artifact — runtime config comes from environment variables on the host (see [secrets-management.md](secrets-management.md)).
- **Frontend**: `dist/` from `ng build --configuration {production|staging}`; served as static files. `apiUrl` is the relative `/api/` in staging/prod (reverse-proxied to the backend).

## Applying database migrations

Migrations are **not** applied automatically by the app at startup. Apply them explicitly as a deploy step, pointed at the target environment's database.

Option A — from the SDK, against a target connection string:

```powershell
$env:ConnectionStrings__DefaultConnection = "<target-db-connection-string>"
dotnet ef database update --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API
```

Option B — generate an idempotent SQL script in CI and run it during release (preferred for prod, reviewable):

```powershell
dotnet ef migrations script --idempotent --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API -o migrate.sql
```

> Migrations that drop columns/tables in `Up()` are destructive. Review any such migration and back up the target database before applying. Additive migrations (new columns/tables) are safe.

## Reverse proxy

Staging/prod run behind a reverse proxy. The API enables `UseForwardedHeaders` so the real client IP is used for rate-limiting and analytics. Set `ForwardedHeaders:KnownProxies` (or `KnownNetworks`) to the proxy address in the host environment for the strictest configuration; by default only the immediate proxy's forwarded headers are trusted.

## Release checklist

1. Merge to the target branch (`dev` → staging, `master` → prod). The matching pipeline builds, tests, scans, and publishes.
2. Ensure host **environment variables** are set for all required secrets (see [secrets-management.md](secrets-management.md)).
3. Apply pending EF migrations against the target database (Option A or B above).
4. Deploy the published artifact.
5. Smoke-check `/health` (backend health check) and a basic authenticated flow.
