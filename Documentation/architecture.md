# Architecture

> Authoritative source for architecture *rules* is [`../AGENTS.md`](../AGENTS.md). This page is a factual map to orient quickly; it does not grant exceptions to the contract.

## Solution layout

Mono-repo with two independently built and deployed applications:

```
FinanceBack/                     .NET 10 solution (BackPredictFinance.sln)
  BackPredictFinance.API         HTTP delivery: controllers, DI wiring, middleware, Program.cs
  BackPredictFinance.Services    Business orchestration, organised by capability
  BackPredictFinance.Patterns    Deterministic pattern definitions & detection
  BackPredictFinance.Datas       EF Core entities, FinanceDbContext, migrations
  BackPredictFinance.ViewModels  Request/response DTOs + AutoMapper profiles
  BackPredictFinance.Common      Small shared cross-project types (AnalysisV1 = legacy, shrinking)
  BackPredictFinance.Tests       Behaviour tests (xUnit)
FinanceFront/                    Angular 21 app (standalone components + Signals)
```

## Layering & dependency direction

```
API  ->  Services  ->  { Patterns, Datas }
                \-> ViewModels        (documented derogation, not a target)
Common is shared by all.
```

- **API** is thin: it authenticates/authorises, binds requests, calls a service, returns a ViewModel. No business rules, no `DbContext` queries.
- **Services** own the business truth. Capability folders under `ClientFinanceServices/` (`Analysis/`, `Fundamentals/`, `Portfolio/`, `Screener/`, `Tax/`, …), plus `AuthServices/`, `AdminGovernance/`, `BackgroundJobs/`, `Notifications/`.
- **Patterns** is a pure deterministic engine (no HTTP, no persistence). Pattern definitions derive geometry (target/invalidation prices, direction).
- **Datas** is persistence only. Soft-delete via `IsDeleted` is the convention (no physical `DELETE`).
- **ViewModels** are the transport boundary to the frontend.

## Core flow: running an analysis

Entry: `POST api/ClientFinance/analysis/run` → `ClientFinanceAnalysisController` → `ClientFinanceService.RunAnalysisAsync` →

1. `AnalysisRequestCompatibilityResolver` resolves the request (asset, as-of date, portfolio context via `PortfolioContextLoader`).
2. `ClientAnalysisOrchestrator` runs the deterministic pipeline: `DeterministicAnalysisExecutionService` (pattern engine), `RiskEvaluationService`, `RecommendationPolicyService`, `ActionPlanGenerationService`, pedagogical wording.
3. `AnalysisSnapshotPersistenceService` persists an `AnalysisRun` snapshot (auditable, versioned).
4. Result is projected to `AnalysisResponseViewModel` for the frontend.

Background jobs (`BackgroundJobs/`) refresh market data and evaluate signal outcomes ex-post.

## Key conventions

- English in code; French for user-facing text.
- One concrete type per file (tolerated: interface+impl, VM+Profile, grouped enums).
- Pagination required on list endpoints.
- Frontend = rendering/orchestration only; it must not recompute scoring/recommendation.
- Enums are serialised as **strings** (`JsonStringEnumConverter`) and shared with the frontend — renames/reorders are breaking changes.

See [`../AGENTS.md`](../AGENTS.md) for the full contract and the current documented derogations.
