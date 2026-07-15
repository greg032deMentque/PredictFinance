# Local development setup

## Prerequisites

- **.NET 10 SDK** (all backend projects target `net10.0`).
- **Node.js 20.x** (Angular 21 requires `^20.19.0`).
- **SQL Server** reachable by the dev connection string — the default is LocalDB (`(localdb)\mssqllocaldb`, Windows integrated auth).

## Backend configuration

Configuration is layered. The last source wins:

```
appsettings.json                 committed — structure and non-secret defaults
appsettings.{Environment}.json   Development = gitignored (local only)
user-secrets                     Development secrets, stored outside the repo
environment variables            staging / production (see secrets-management.md)
```

### 1. Create your local `appsettings.Development.json`

This file is **gitignored** — each developer/machine keeps its own. It holds only **non-secret** values. Minimal template:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PredictFinance;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Frontend": { "BaseUrl": "http://localhost:4200" },
  "JWTToken": {
    "Issuer": "PredictFinance.Dev",
    "Audience": "PredictFinance.Dev.Client",
    "Secret": "set-via-user-secrets",
    "ValidityMinutesAcessToken": "15",
    "ValidityMinutesRefreshToken": "1440"
  },
  "ServerSalt": "set-via-user-secrets",
  "AutomapperLicense": "set-via-user-secrets",
  "adminEmail": "set-via-user-secrets",
  "adminPwd": "set-via-user-secrets",
  "userEmail": "set-via-user-secrets",
  "userPwd": "set-via-user-secrets"
}
```

The LocalDB connection string uses Windows integrated auth (no password) and is therefore **not a secret** — it is fine to keep it in this local file.

### 2. Set the actual secrets in user-secrets

User-secrets are stored in your Windows user profile, outside the repository, and automatically override the file in the `Development` environment.

```powershell
$api = "FinanceBack/BackPredictFinance.API"
dotnet user-secrets set "JWTToken:Secret"   "<a-long-random-dev-key>"  --project $api
dotnet user-secrets set "ServerSalt"        "<a-random-base64-salt>"   --project $api
dotnet user-secrets set "AutomapperLicense" "<automapper-license-key>" --project $api
dotnet user-secrets set "adminEmail"        "admin@finance.dev"        --project $api
dotnet user-secrets set "adminPwd"          "<strong-dev-password>"    --project $api
dotnet user-secrets set "userEmail"         "user@finance.dev"         --project $api
dotnet user-secrets set "userPwd"           "<strong-dev-password>"    --project $api
```

List the keys currently set (values shown — run in a private terminal):

```powershell
dotnet user-secrets list --project FinanceBack/BackPredictFinance.API
```

> `dotnet ef` design-time commands load the connection string from `appsettings.Development.json` (kept non-secret above), so migrations work without any extra step.

### 3. Create / update the database

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API
```

### 4. Run

```powershell
dotnet run --project FinanceBack/BackPredictFinance.API      # backend
npm install --prefix FinanceFront ; npm start --prefix FinanceFront   # frontend on :4200
```

## Tests

```powershell
dotnet test FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj
```

Integration tests use an in-memory EF provider and a committed, secret-free `appsettings.Testing.json`, so they need no local config. Six integration tests are currently expected to fail on missing business seed data (tracked in [ci-cd.md](ci-cd.md) and [`../REMEDIATION-SUMMARY.md`](../REMEDIATION-SUMMARY.md)); the CI gate excludes exactly those.

## Frontend tests

Per project convention, **no Angular/Jasmine tests are written or requested.**
