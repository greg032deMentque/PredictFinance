# AGENTS.md

## Repository purpose

This repository contains the refactor of a pedagogical investment-analysis application for beginner retail users.

The product goal is to help a user:
- maintain a watchlist
- maintain a portfolio
- store multiple purchase lines per instrument
- analyze daily market prices
- detect chart patterns
- understand possible scenarios
- receive pedagogical recommendations
- review analysis history over time

This product is:
- an educational investment-analysis and guidance tool
- not a broker
- not an order execution system
- not a bank-account integration
- not a real-time tick-by-tick trading platform

## Repository structure

Main observed areas in this repository:

- `BackPredictFinance.sln`
- `BackPredictFinance.API`
- `BackPredictFinance.Services`
- `BackPredictFinance.Datas`
- `BackPredictFinance.ViewModels`
- `BackPredictFinance.Common`
- `BackPredictFinance.Tests`
- `FinanceFront`
- Python analysis/test area provided separately with:
  - `main.py`
  - `run_tests.py`
  - `pyproject.toml`
  - `tests/`

When working in this repository, always identify first which layer is being changed.

## Global working mode

- For any non-trivial task, start with an audit and a written plan before editing code.
- Do not start broad refactors immediately.
- Work in small verified increments.
- Keep changes scoped to the current task.
- Preserve repository consistency at all times.
- After each meaningful change, validate using the relevant build/tests for the touched area.

## Mandatory workflow for complex tasks

For any major refactor, follow this order:

1. Audit current repository state
2. Identify mismatches against the requested target
3. Design the target grounded in the real codebase
4. Produce an ordered implementation plan
5. Implement incrementally
6. Validate after each meaningful increment
7. Report unresolved items explicitly

If the task is localized, still inspect surrounding files before changing anything.

## Product rules that are non-negotiable

- The API is the source of truth for business analysis.
- V1 must work without AI.
- AI, if present, must remain optional and peripheral in V1.
- AI must never be the sole source of:
  - pattern detection
  - confidence/scoring
  - recommendation
  - risk outputs
- All user-facing analysis outputs must be explainable, traceable, and auditable.
- If no credible pattern is detected, the system must explicitly say so.
- Do not force probabilities to sum to 100% if the business meaning would become false or misleading.

## V1 business scope

Target user:
- beginner retail investor

Initial market scope:
- French stocks

Initial time granularity:
- daily candles

Portfolio data must support:
- quantity held
- buy price
- buy date
- average cost / PRU
- fees
- currency
- multiple purchase lines per asset

Analysis output must be able to provide:
- main pattern
- alternative compatible patterns
- confidence level
- pattern progress/status
- recommendation
- invalidation level
- suggested stop loss
- suggested take profit
- risk/reward ratio
- pedagogical summary

If multiple patterns are compatible:
- keep all compatible patterns
- do not arbitrarily force a single winner
- explain why multiple compatible patterns are shown
- preserve separate calculations and traceability

Recommendation wording must depend on portfolio context:
- if the user does not hold the asset: monitor / buy / wait
- if the user holds the asset: hold / reinforce / lighten / sell / wait

## Architectural rules

The business architecture must separate:

- market data ingestion
- market data normalization
- pattern detection
- pattern validation / invalidation
- confidence/scoring
- risk evaluation
- recommendation generation
- pedagogical explanation
- portfolio contextualization
- persistence and analysis snapshot history

Do not collapse these responsibilities into one opaque service.

The architecture must stay open for later addition of:
- new financial instruments
- new asset classes
- new market data providers
- new chart patterns
- new scoring rules
- new recommendation policies
- optional AI explanation features

## Pattern engine rules

Pattern detection is core business logic.

It must be deterministic where business rules require determinism.

Each pattern should be modeled through an explicit contract or equivalent extension mechanism and should be able to declare:
- unique identifier
- display name
- pedagogical description
- minimum historical depth
- required inputs
- detection rules
- validation rules
- invalidation rules
- confidence/scoring rules
- explanation metadata
- risk-related outputs when applicable

The analysis window depends on the pattern.
Do not hardcode one universal historical window if the domain requires per-pattern depth.

## History and versioning rules

The system must support analysis snapshots over time.

The architecture should be able to preserve:
- analysis timestamp
- analyzed asset
- market data range used
- engine/rule version
- detected patterns
- confidence values
- recommendation
- risk outputs
- explanation outputs
- portfolio context used

Do not implement fake history or fake versioning.
If full V1 history is not present yet, create a clean path toward it.

## Ex post evaluation rules

The architecture should prepare for later evaluation of signal performance with a stable protocol, such as:
- performance at J+5
- performance at J+20
- maximum drawdown after signal
- invalidation reached or not
- target hit or not

Do not overbuild beyond task scope.
Prepare clean extension points instead.

## Layer-specific rules

### BackPredictFinance.API

This layer is responsible for:
- HTTP endpoints
- authentication/authorization wiring
- orchestration entry points
- request/response handling
- middleware registration
- dependency injection registration
- exposure of business use cases

This layer must not own core pattern logic, provider-specific parsing logic, or UI concerns.

Avoid:
- embedding domain rules directly in controllers
- placing pattern calculations inside controllers
- mixing persistence details into endpoint logic

### BackPredictFinance.Services

This layer is responsible for:
- application/business services
- orchestration of use cases
- pattern detection workflows
- recommendation workflows
- risk calculation workflows
- optional adapters toward Python/AI if they exist

This layer should become the main place for business orchestration.
Keep responsibilities explicit.
Do not turn service classes into god-objects.

If existing Python-related services exist:
- keep them optional in V1
- isolate them behind explicit interfaces if needed
- do not make them mandatory for core business truth

### BackPredictFinance.Datas

This layer is responsible for:
- EF Core persistence
- entities
- DbContext
- migrations
- persistence mapping/configuration
- repository-style persistence concerns if present

This layer must not contain:
- UI rules
- controller concerns
- provider-specific HTTP logic
- opaque business recommendation logic

Database changes must be intentional and minimal.
Do not introduce schema changes without explaining their business purpose.

### BackPredictFinance.ViewModels

This layer should stay limited to transport/view contracts.
Do not leak persistence entities directly into API outputs if dedicated view models exist or are required.

Avoid turning view models into domain truth.

### BackPredictFinance.Common

Use only for genuinely shared cross-cutting primitives.
Do not move business logic into `Common` just to avoid choosing the correct layer.

### BackPredictFinance.Tests

When changing .NET behavior:
- update or add tests when justified
- prefer targeted tests over broad noisy test changes
- keep tests aligned with actual business rules
- never fake expected behavior that the product rules do not support

### FinanceFront

This is the Angular frontend.

Frontend responsibilities include:
- watchlist UI
- portfolio UI
- analysis display
- explanation display
- history comparison display
- alerts display
- user workflow and presentation

Do not place core financial pattern logic in Angular.
Do not duplicate API truth in frontend business code unless there is a justified UI-only derivation.

If UI needs computed display states:
- keep them presentation-focused
- do not create a second source of truth for pattern analysis

Respect the existing Angular setup and repository conventions.
Do not introduce architectural divergence between legacy Angular style and current project structure without explicit task need.

### Python analysis area

The Python area is optional for V1.
Treat it as:
- experimental
- peripheral
- replaceable
- never the sole source of truth for core business outputs

If modifying Python code:
- keep interfaces explicit
- avoid hidden coupling with .NET internals
- avoid making API behavior depend on opaque model behavior for mandatory outputs
- ensure testability and reproducibility where possible

## Refactor discipline

Before changing architecture:
- identify what exists
- identify what is reusable
- identify what is misplaced
- identify what must be moved
- identify what must be rewritten
- identify what should be deferred beyond V1

Never assume the current repository already matches the target architecture.

Prefer:
- explicit contracts
- real extension points
- maintainable naming
- small safe increments
- narrow, testable changes

Avoid:
- speculative frameworks
- generic abstractions with no real use
- dead extension points
- hidden coupling
- silent behavior changes
- large rewrites without validation checkpoints

## Engineering rigor rules

- No invented requirements
- No hidden assumptions
- No fake business logic
- No fake data semantics
- No silent behavior changes
- No dead code introduced
- No broad rewrite outside the scope actually needed
- No comments inside code unless repository conventions or the specific task explicitly require them

When information is missing:
- stop
- identify the exact missing element
- ask only the minimum blocking question

## Security and quality rules

All code changes must aim for high rigor, maintainability, and security.

Prefer code that is:
- explicit
- testable
- auditable
- maintainable
- static-analysis-friendly
- OWASP-minded where applicable

Avoid:
- hidden coupling
- magic behavior
- architecture leakage across layers
- provider-specific logic embedded in core domain contracts
- UI concerns embedded in core business analysis

## Validation commands and expectations

Choose validation based on the touched area.

### For .NET changes

Prefer relevant commands such as:
- `dotnet restore BackPredictFinance.sln`
- `dotnet build BackPredictFinance.sln`
- `dotnet test BackPredictFinance.Tests/BackPredictFinance.Tests.csproj`

### For Angular changes

Prefer relevant commands such as:
- `npm install`
- `npm run build`
- `npm test` if configured

### For Python changes

Prefer relevant commands such as:
- environment setup consistent with `pyproject.toml` or `requirements.txt`
- `pytest`

## Output expectations

For significant tasks, structure responses in this order:

1. Current state audit
2. Gap analysis versus target
3. Target design
4. Implementation plan
5. Implementation
6. Validation report
7. Remaining issues / next step

## Scope control

Prioritize V1 delivery.
Do not drift into V2/V3 unless the task explicitly asks for it.

V1 priorities are:
- API as source of truth
- deterministic and explainable pattern analysis
- separation of business layers
- portfolio-aware recommendation wording
- analysis history foundation
- extensibility for assets, providers, and patterns
- AI optional, not central
