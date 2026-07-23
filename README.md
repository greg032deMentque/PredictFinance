# PredictFinance

PredictFinance is a pedagogical investment-analysis product for beginner retail users.

The V1 target is intentionally narrow:
- French listed equities only
- daily analysis only
- deterministic backend-owned business truth
- frontend as a rendering and orchestration layer, not a second business engine

The product helps a user maintain a watchlist and portfolio, run technical analysis, review history over time, and understand possible scenarios through explainable guidance. It is not a broker, not an execution platform, and not a real-time trading terminal.

## Repository structure

- `FinanceBack`
  - .NET 10 backend solution (7 projects — see the map below)
- `FinanceFront`
  - Angular 21 web application (standalone components + Signals) that consumes backend truth
- `Doc`
  - canonical product contracts, gap audits, and delivery documentation
- `Documentation`
  - **operational documentation** — local setup, CI/CD, deployment, and secrets management
- `AGENTS.md`
  - repository working contract for coding agents (read first before writing code)
- `KNOWN_ISSUES.md`
  - tracked deviations from the architecture contract, and audit fixes with manual follow-ups

### Backend project map (quick orientation for agents)

| Project | Role | Must NOT contain |
|---|---|---|
| `BackPredictFinance.API` | HTTP delivery only (thin controllers, DI, middleware) | business rules, EF queries, pattern logic |
| `BackPredictFinance.Services` | business orchestration, organised by capability (`Analysis/`, `Fundamentals/`, `AuthServices/`, …) | duplicated mapping, EF entities as outputs |
| `BackPredictFinance.Patterns` | deterministic pattern definitions & detection | HTTP concerns, persistence, recommendation wording |
| `BackPredictFinance.Datas` | EF Core entities, `FinanceDbContext`, migrations | pattern/scoring/recommendation logic |
| `BackPredictFinance.ViewModels` | request/response DTOs + AutoMapper profiles | business logic, exposed EF entities |
| `BackPredictFinance.Common` | small cross-project shared types (`AnalysisV1` is legacy, to shrink) | capability-specific contracts |
| `BackPredictFinance.Tests` | behaviour proof (patterns, scoring, recommendation, persistence, authz) | trivial plumbing coverage |

Dependency direction: `API → Services → {Patterns, Datas}`, with `Common` shared. The `Services → ViewModels` coupling is a documented derogation (see `AGENTS.md`), not a target.

The core business flow lives in `BackPredictFinance.Services/ClientFinanceServices/Analysis/` (orchestration, scoring, risk, recommendation, persistence of analysis snapshots).

## Recommended reading order

1. `AGENTS.md`
2. `Doc/v1/00_INDEX.md`
3. `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`

The root README is an entry point only. Canonical product authority remains in `Doc/v1/*` and the contract map above. For **operational** topics (running locally, CI/CD, deployment, secrets), see the [`Documentation/`](Documentation/README.md) wiki.

## Local development setup

The backend needs local configuration that is **not** committed (`appsettings.Development.json` is gitignored, and secrets live in .NET user-secrets). See [`Documentation/local-setup.md`](Documentation/local-setup.md) for the full step-by-step, including the exact user-secrets keys to set. In short:

```powershell
# one-time: create your local dev config from the template, then set secrets
dotnet user-secrets set "JWTToken:Secret" "<dev-signing-key>" --project FinanceBack/BackPredictFinance.API
# ...repeat for Security:RefreshTokenHmacKey, AutomapperLicense, adminEmail/adminPwd, userEmail/userPwd
dotnet ef database update --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API
```

## Useful commands

Run these from the repository root unless noted otherwise.

### Backend

```powershell
dotnet restore FinanceBack/BackPredictFinance.sln
dotnet build FinanceBack/BackPredictFinance.sln
dotnet test FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj
dotnet run --project FinanceBack/BackPredictFinance.API
```

### Frontend

```powershell
npm install --prefix FinanceFront
npm start --prefix FinanceFront
npm run build --prefix FinanceFront
npm test --prefix FinanceFront
```

Notes:
- `dotnet test FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj` is currently proven to pass in this repository state.
- `npm run build --prefix FinanceFront` requires installed node packages first; run `npm install --prefix FinanceFront` before claiming frontend build status.

## Known current mismatches / debt

- `FinanceFront/README.md` is the default Angular stub and is obsolete.
- `TradingController` is a `410 Gone` tombstone (the product is not an order-execution system) and can be removed.
- `BackPredictFinance.Services` still depends on `BackPredictFinance.ViewModels` (documented derogation in `AGENTS.md`).
- part of the current analysis-domain contract still lives under `BackPredictFinance.Common/AnalysisV1` (to shrink, not extend).
- `ViewModels` still expose some internal analysis contracts directly instead of dedicated projections.

`DOUBLE_TOP` is **not** legacy residue: it is a first-class reversal pattern (`DoubleTopReversalPatternDefinition`). See `KNOWN_ISSUES.md` for the latest audit fixes and outstanding follow-ups.
