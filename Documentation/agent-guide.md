# Agent guide — how to work in this repository

This is the fast, operational guide for an AI agent contributing here. The **binding** rules are in [`../AGENTS.md`](../AGENTS.md); this page distills the workflow and links the rest of the wiki. Before writing code, also skim [pitfalls.md](pitfalls.md).

## Golden rules (non-negotiable)

1. **Plan before code.** For any change, first produce a plan: files touched (with their project), change per file, impacted callers of modified symbols, potential breakages. Stop and get approval before generating code.
2. **The API is the source of business truth.** The frontend renders and orchestrates; it never recomputes scoring, recommendation, PnL, or eligibility.
3. **Everything must be explainable and auditable.** No opaque merged scores, no fake probabilities. Pattern reliability and fundamental scoring stay separate.
4. **Respect project boundaries** (see the table below and `AGENTS.md`).
5. **No secret in the repo.** See [secrets-management.md](secrets-management.md).
6. **Tests**: backend tests only when they prove business behaviour at risk of silent regression. **No frontend (Angular/Jasmine) tests** — do not write or suggest them.
7. **No comments in code.** Names carry meaning. (Documentation like this wiki is fine.)
8. **No new NuGet/npm package** without it being called out in the plan and approved.
9. **Keep the business rules registry current.** If you change a threshold, formula, or add/remove an analysis anywhere under `ClientFinanceServices/` or `BackPredictFinance.Patterns/`, update [business-rules-analysis-engine.md](business-rules-analysis-engine.md) in the same change.

## Where things live

| You need to… | Go to |
|---|---|
| Change HTTP surface (routes, auth, binding) | `BackPredictFinance.API/Controllers/` |
| Change business logic | `BackPredictFinance.Services/` (analysis: `ClientFinanceServices/Analysis/`) |
| Change pattern detection/geometry | `BackPredictFinance.Patterns/` |
| Change persistence / entities / migrations | `BackPredictFinance.Datas/` |
| Change request/response shapes | `BackPredictFinance.ViewModels/` + AutoMapper profiles |
| Change the UI | `FinanceFront/src/app/` |
| Understand endpoints | [api-endpoints.md](api-endpoints.md) |
| Understand the data model | [data-model.md](data-model.md) |
| Understand domain terms | [glossary.md](glossary.md) |
| Understand the analysis engine's business rules (thresholds, formulas) | [business-rules-analysis-engine.md](business-rules-analysis-engine.md) |

## Project boundaries (quick contract)

| Project | Allowed | Forbidden |
|---|---|---|
| `API` | thin controllers, DI, middleware, auth | business rules, EF queries, pattern logic |
| `Services` | business orchestration by capability | duplicated mapping, exposing EF entities |
| `Patterns` | deterministic detection & geometry | HTTP, persistence, recommendation wording |
| `Datas` | entities, DbContext, migrations | scoring/recommendation/pattern logic |
| `ViewModels` | DTOs + AutoMapper profiles | business logic, exposed entities |
| `Common` | small shared types | capability-specific contracts; extending `AnalysisV1` |
| `Tests` | behaviour proof | trivial plumbing coverage |

## Conventions cheat-sheet

- English in code; French for user-facing text.
- One concrete type per file (tolerated: interface+impl, VM+Profile, grouped enums).
- Soft-delete via `IsDeleted`; never physical `DELETE`.
- Pagination required on list endpoints.
- Enums serialised as strings; renames/reorders are breaking changes shared with the frontend.
- Frontend binding objects are PascalCase.
- Frontend user-facing errors are business French only (no traceId / HTTP code / technical text).
- `TreatWarningsAsErrors` is on — a warning fails the build.

## Build / test / run

```powershell
dotnet build FinanceBack/BackPredictFinance.sln
dotnet test  FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj
dotnet run   --project FinanceBack/BackPredictFinance.API
```
Local config & secrets setup: [local-setup.md](local-setup.md). Six integration tests are expected to fail on missing seed data (see [ci-cd.md](ci-cd.md)) — the CI gate excludes exactly those at method level.

## Delegation (Claude Code specific)

When routing work: full-stack or unclear scope → `project-orchestrator`; backend only → `dotnet-api-architect`; frontend only → `angular-architect`; read-only audit/review → handle directly. Each architect plans before coding.

## When you finish

- Update [pitfalls.md](pitfalls.md) if you hit or fixed a new trap.
- Update [`../REMEDIATION-SUMMARY.md`](../REMEDIATION-SUMMARY.md) if you applied audit fixes with manual follow-ups.
- Keep this wiki accurate — a stale doc costs the next agent more than no doc.
