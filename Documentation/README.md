# PredictFinance — Operational documentation (wiki)

This folder is the **operational** knowledge base: how to run the project locally, how it is built and shipped, and how secrets are handled across environments. It complements — and never overrides — the two authoritative sources:

- [`AGENTS.md`](../AGENTS.md) — the binding working contract (architecture rules, what each project may/may not contain). **Read it before writing code.**
- [`Doc/`](../Doc) — canonical **product** specifications (scope, screens, data contracts).

If those contradict this wiki, they win. This wiki documents *how we operate*, not *what the product is*.

## Pages

**Start here if you are an agent about to make a change:** [agent-guide.md](agent-guide.md) then [pitfalls.md](pitfalls.md).

### Understand the project
| Page | What it covers |
|---|---|
| [architecture.md](architecture.md) | Solution layout, layers, dependency direction, the core analysis flow |
| [data-model.md](data-model.md) | Entities by domain, key invariants, soft-delete, migrations workflow |
| [api-endpoints.md](api-endpoints.md) | The 31 controllers, auth model, route conventions |
| [glossary.md](glossary.md) | Domain terms (pattern, direction, confidence, PEA, FIFO, ex-post…) |
| [business-rules-analysis-engine.md](business-rules-analysis-engine.md) | Every analysis the backend runs (patterns, scoring, risk, fundamentals, portfolio, tax, alerts) with the exact thresholds/formulas currently coded |

### Contribute safely
| Page | What it covers |
|---|---|
| [agent-guide.md](agent-guide.md) | How to work here: golden rules, boundaries, conventions, build/test, delegation |
| [pitfalls.md](pitfalls.md) | **Mistakes not to reproduce** — real traps with the correct approach |

### Operate
| Page | What it covers |
|---|---|
| [local-setup.md](local-setup.md) | Running the stack on a dev machine: prerequisites, dev config, user-secrets, database |
| [ci-cd.md](ci-cd.md) | The 4 Azure DevOps pipelines: stages, gates, triggers, current exclusions |
| [deployment.md](deployment.md) | Build artifacts, applying EF migrations per environment, release checklist |
| [secrets-management.md](secrets-management.md) | The secrets strategy per environment (user-secrets / env vars / Key Vault) |

## Fast orientation for AI agents

- **Stack**: .NET 10 backend (`FinanceBack/`, 7 projects) + Angular 21 frontend (`FinanceFront/`), mono-repo, SQL Server, Azure DevOps CI.
- **Where business logic lives**: `BackPredictFinance.Services/ClientFinanceServices/Analysis/`. The frontend never recomputes business truth.
- **Build/test**: `dotnet build FinanceBack/BackPredictFinance.sln`, `dotnet test FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj`. `TreatWarningsAsErrors` is on — a warning fails the build.
- **Config**: `appsettings.json` (committed, structure/defaults) → `appsettings.{Env}.json` (committed for staging/prod as placeholders; **gitignored** for Development) → **user-secrets** (dev) / **environment variables** (staging/prod). Secrets never live in the repo — see [secrets-management.md](secrets-management.md).
- **Migrations**: EF Core, in `BackPredictFinance.Datas/Migrations/`. Never auto-generate without explicit instruction; never apply to a database as part of a code change.
- **Latest audit + follow-ups**: [`../KNOWN_ISSUES.md`](../KNOWN_ISSUES.md).
