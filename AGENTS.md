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
- legacy or experimental Python analysis/test area provided separately with:
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
- Never claim completion without repository proof.
- Never present an architectural target as already implemented without proof.

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

## V1 runtime decoupling rules

For the current V1 target, the repository must satisfy all of the following:

- The API is the sole runtime source of truth for V1 analysis.
- No V1 on-demand analysis request may require Python availability.
- No V1 compatibility resolver may depend on Python options or Python catalogs.
- No enabled V1 pattern registry may depend on Python services.
- No mandatory recommendation, scoring, risk, or explanation output may come from Python.
- No configuration required only for Python may remain mandatory for V1 startup or V1 execution.
- No active API endpoint may expose Python as business truth for V1.
- No runtime-reachable service required by V1 may call Python directly or indirectly.

A repository is not considered V1-clean if any direct or indirect runtime dependency on Python remains on the active V1 path.

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
- market eligibility
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

## Market data truth rules

For V1, market data must be controlled by explicit business eligibility rules.

- Provider payload is evidence, not business truth by itself.
- V1 analyzable instruments must be accepted only if eligibility is explicitly proven in API-owned logic.
- V1 enabled instruments are restricted to active French listed equities only, unless a later written decision changes the scope.
- Eligibility must not be inferred through hidden fallbacks.
- Missing or ambiguous provider fields must lead to explicit rejection or explicit unresolved status, never silent acceptance.
- Market data ingestion, normalization, eligibility, and persistence must remain separated concerns.
- Analysis requests must not proceed when instrument eligibility is not proven.
- Eligibility rules must be centrally enforceable and testable.
- Persistence of an asset record does not by itself prove V1 analyzability.

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

## Pattern correctness proof rules

Any claim that a V1 pattern implementation is correct must be backed by explicit repository proof.

Minimum proof expected:
- executable pattern contract identified in the real code
- deterministic detection logic identified in the real code
- explicit validation rules identified in the real code
- explicit invalidation rules identified in the real code
- explicit scoring/confidence logic identified in the real code
- explicit recommendation boundary identified in the real code
- targeted tests covering nominal path, edge cases, and no-credible-pattern path
- persisted snapshot proof showing traceability of the produced assessment

Forbidden:
- claiming a pattern is correct because a legacy or Python implementation exists somewhere else
- claiming multi-pattern readiness if the active runtime only supports one pattern
- hiding a temporary mono-pattern limitation behind generic architecture wording
- claiming explainability if the final output cannot be traced to API-owned rules

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
- market eligibility workflows
- pattern detection workflows
- recommendation workflows
- risk calculation workflows
- optional legacy adapters kept outside the active V1 runtime path where strictly necessary during transition

This layer should become the main place for business orchestration.
Keep responsibilities explicit.
Do not turn service classes into god-objects.

If existing Python-related services exist:
- they must not be required by the active V1 path
- they must be isolated behind explicit boundaries
- they must be classified as legacy, experimental, test-only, or removal-target
- they must never be presented as core business truth for V1

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
- prove runtime-path claims with focused tests when possible
- prove negative paths as well as positive paths when they matter for V1 truth

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

Python is not part of the V1 runtime truth.

For this repository:
- V1 runtime must not depend on Python.
- Python must not be required to execute any V1 API analysis use case.
- Python must not be required to resolve enabled V1 patterns.
- Python must not be required to compute V1 analysis windows.
- Python must not be required to produce mandatory V1 outputs.
- Python may remain only as:
  - archived legacy code
  - explicit non-V1 experimental tooling
  - temporary migration material scheduled for removal

Forbidden for V1:
- mandatory DI registrations toward Python services
- runtime analysis paths that call Python directly or indirectly
- V1 contracts whose resolution depends on Python configuration
- API endpoints that expose Python as business truth
- hidden coupling between V1 orchestration and Python-specific models/options

If Python code still exists in the repository:
- classify it explicitly as legacy, experimental, test-only, or removal-target
- prove whether it is runtime-reachable or not
- never present a repository as V1-clean while Python runtime dependencies still exist

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
- No claim of compliance without repository proof
- No mixing of audit facts and proposals without explicit labeling

When information is missing:
- stop
- identify the exact missing element
- ask only the minimum blocking question

## Evidence classification rules

For any significant audit or refactor response, always classify statements using these buckets:

- PROVEN: directly supported by repository code, configuration, tests, or contract documents
- DECIDED: explicitly mandated by AGENTS.md or contract documents
- PROPOSED: recommendation not yet implemented nor contractually frozen
- DEROGATION: conscious deviation from contract or target
- REMAINING TO ARBITRATE: unresolved point requiring a written decision

Do not merge these categories.
Do not present a proposal as if it were already proven or decided.
Do not present a derogation as contract compliant.
Do not present an unresolved point as closed.

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
- weak validation on business-critical inputs
- ambiguous error handling on business-critical paths
- configuration sprawl that obscures the true runtime path

## Minimum proof requirements for V1 audit and corrective work

Any serious V1 audit or corrective delivery must include:

- current runtime-path audit
- dependency-injection audit
- active configuration audit
- market-data eligibility audit
- pattern-engine audit
- recommendation-boundary audit
- snapshot/history audit
- targeted test audit
- anti-drift matrix

If implementation is performed, also include:
- exact files changed
- exact reason for each change
- validation commands executed
- explicit residual risks
- ZIP delivery of changed files when requested by the task contract

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

Python validation is never sufficient to prove V1 runtime correctness.

## Output expectations

For significant tasks, structure responses in this order:

1. Current state audit
2. Gap analysis versus target
3. Target design
4. Implementation plan
5. Implementation
6. Validation report
7. Remaining issues / next step

## Refusal criteria

Refuse to present the repository as V1-ready if any of the following remains true:

- an active V1 runtime path still depends on Python
- enabled V1 patterns are still resolved through Python-specific services or options
- market eligibility remains distributed, implicit, or unproven
- mandatory analysis outputs are not traceable to API-owned logic
- snapshot persistence omits required trace/version/context fields
- tests do not prove the claimed corrective behavior
- a response mixes proven facts with assumptions without explicit classification

## Scope control

Prioritize V1 delivery.
Do not drift into V2/V3 unless the task explicitly asks for it.

V1 priorities are:
- API as source of truth
- deterministic and explainable pattern analysis
- full runtime decoupling from Python
- explicit market eligibility enforcement
- separation of business layers
- portfolio-aware recommendation wording
- analysis history foundation
- extensibility for assets, providers, and patterns
- AI optional, not central