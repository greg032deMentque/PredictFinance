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
  - .NET backend solution with API, services, persistence, patterns, view models, and tests
- `FinanceFront`
  - Angular web application that consumes backend truth
- `Doc`
  - canonical product contracts, gap audits, and delivery documentation
- `AGENTS.md`
  - repository working contract for coding agents

## Recommended reading order

1. `AGENTS.md`
2. `Doc/product/README.md`
3. `Doc/product/12_v1_documentation_baseline_and_canonical_map.md`
4. `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`

The root README is an entry point only. Canonical product authority remains in `Doc/product/*` and the contract map above.

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

## Known current mismatches

- `FinanceFront/README.md` is obsolete and still references `GET /api/Trading/predict/{symbol}` as the active target.
- `BackPredictFinance.Services` still depends on `BackPredictFinance.ViewModels`.
- part of the current analysis-domain contract still lives under `BackPredictFinance.Common/AnalysisV1`.
- legacy `DOUBLE_TOP` compatibility residue still exists in the backend codebase.
