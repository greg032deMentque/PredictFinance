## Contract authority

This file is the binding working contract for any agent operating on this repository.

It is not guidance.
It is not a style suggestion.
It is not optional.

If repository reality contradicts an assumption, repository reality wins.
If this contract contradicts a generic habit, this contract wins.
If a task request contradicts this contract, the contradiction must be stated explicitly and resolved before any broad implementation.

An agent must never silently reconcile contradictions.
An agent must never treat a proposal as if it were already implemented.
An agent must never use convenience as a reason to break project boundaries.

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

## Current repository perimeter

Main observed projects:
- `BackPredictFinance.sln`
- `BackPredictFinance.API`
- `BackPredictFinance.Services`
- `BackPredictFinance.Datas`
- `BackPredictFinance.ViewModels`
- `BackPredictFinance.Contracts`
- `BackPredictFinance.Common`
- `BackPredictFinance.Tests`
- `FinanceFront`

A new project is forbidden by default.
It may be added only under the explicit rule defined later in this file.

## Non-negotiable product rules

- The API is the source of truth for business analysis.
- V1 must work without AI.
- AI, if present later, must remain optional and peripheral.
- All user-facing analysis outputs must be explainable, traceable, and auditable.
- If no credible pattern is detected, the system must explicitly say so.
- Do not force probabilities to sum to 100% if doing so would create false meaning.
- Recommendation wording must depend on portfolio context.
- Backend business truth must not be duplicated in the frontend.

## V1 scope lock

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

## Mandatory working method

For any non-trivial task, the agent must do all of the following before broad code changes:
1. identify the exact touched runtime path
2. inspect surrounding files in the real repository
3. classify current state as PROVEN / DECIDED / PROPOSED / DEROGATION / REMAINING TO ARBITRATE
4. state the smallest safe target
5. implement in narrow increments
6. validate after each meaningful increment
7. report residual risks explicitly

Forbidden:
- broad rewrite before audit
- repository-wide cosmetic movement
- convenience refactor outside scope
- invented missing semantics
- fake completion claims

## Evidence classification is mandatory

Every significant audit or refactor response must classify statements using only these buckets:
- PROVEN
- DECIDED
- PROPOSED
- DEROGATION
- REMAINING TO ARBITRATE

Rules:
- do not merge categories
- do not present PROPOSED as PROVEN
- do not present DEROGATION as compliant
- do not present unresolved points as closed

## Architecture target

The business architecture must separate all of the following concerns:
- market data ingestion
- market data normalization
- market eligibility
- pattern detection
- pattern validation / invalidation
- confidence / scoring
- risk evaluation
- situation classification
- recommendation decision
- pedagogical explanation rendering
- portfolio contextualization
- persistence and analysis snapshot history

These responsibilities must not be collapsed into one opaque service.

## Project contract

### BackPredictFinance.API

This project is the HTTP delivery layer only.

Allowed responsibilities:
- route declaration
- authorization attributes
- request binding
- response return
- protocol translation
- middleware registration
- dependency injection registration
- application bootstrap

Forbidden responsibilities:
- business rules
- market eligibility logic
- pattern detection logic
- scoring logic
- risk logic
- recommendation logic
- persistence query logic in controllers
- direct `FinanceDbContext` use in controllers
- direct exposure of `Datas` entities to the frontend
- service location through `IServiceProvider`
- ad hoc mapping logic duplicated in controllers

Mandatory rules:
- controllers depend on service interfaces from backend service projects only
- public payloads are `BackPredictFinance.ViewModels` types or simple framework primitives when repository-proven
- `Program.cs` and bootstrap files contain bootstrap only
- middleware remains cross-cutting and must not own domain decisions
- controllers must not return backend contracts directly

Refusal rule:
- if a controller contains business branching that changes business truth, the change is architecturally invalid

### BackPredictFinance.Services

This project contains business services and application orchestration.

Allowed responsibilities:
- use-case orchestration
- market-data workflows
- eligibility workflows
- deterministic pattern detection workflows
- validation / invalidation workflows
- scoring workflows
- risk workflows
- situation classification workflows
- recommendation workflows
- pedagogical explanation rendering workflows
- persistence orchestration
- history orchestration

Mandatory rules:
- services and their interfaces belong here unless a separately justified backend analysis project is explicitly created
- the interface stays in the same file as its implementing service by default
- create a separate interface file only when the same interface is implemented by multiple services or when a strong repository-proven reason requires it
- keep services organized by capability, not by convenience
- keep controllers thin and keep business orchestration in services
- use `BaseService` only when it reduces duplication without artificial inheritance
- no service may depend on `BackPredictFinance.ViewModels`
- no service may return EF entities as business truth
- no service may use frontend concerns as inputs or outputs to internal domain logic

Required internal capability boundaries:
- `Analysis/Application`
- `Analysis/MarketData`
- `Analysis/Eligibility`
- `Analysis/Patterns`
- `Analysis/Scoring`
- `Analysis/Risk`
- `Analysis/Advice`
- `Analysis/Persistence`
- `Analysis/History`

Dependency discipline inside Services:
- `Application` may orchestrate the other capabilities
- `Advice` depends on facts/contracts, not on controller concerns
- `Risk` depends on facts/contracts, not on UI concerns
- `Persistence` maps contracts to persistence, but must not recalculate business truth
- `Patterns` produces analytical facts and traces, but must not generate final frontend wording
- `History` persists or retrieves prior outputs, but must not reinterpret analytical truth
- cyclic dependencies between capabilities are forbidden

Refusal rules:
- if one service becomes the default place for unrelated decisions, split by capability before extending it further
- if a capability depends on `ViewModels`, the design is invalid
- if recommendation logic is copied into pattern definitions, the design is invalid

### BackPredictFinance.Contracts

This project contains backend business-core contracts that are not frontend transport models and not database entities.

Allowed content:
- backend domain enums used by the analysis engine
- analysis result contracts
- advice contracts
- trace contracts
- history snapshot contracts
- family-specific artifact contracts
- value objects used by backend services
- structured payloads for recommendation and explanation

Mandatory rules:
- no service implementation
- no service interface
- no EF entity
- no HTTP/controller concern
- no frontend transport concern
- no business logic methods beyond trivial value-object safety when repository-proven
- contracts must remain serializable and stable enough for backend boundaries

Recommended internal structure:
- `Analysis/Common`
- `Analysis/Families/HorizontalBreakout`
- `Analysis/Families/TriangleContinuation`
- `Analysis/Families/PoleContinuation`
- `Analysis/Advice`
- `Analysis/History`

Refusal rule:
- if a type is placed in `Contracts` only because it is shared but it is actually a service concern, persistence concern, or frontend DTO, the placement is invalid

### BackPredictFinance.Datas

This project contains persistence-only code.

Allowed content:
- EF Core entities
- `DbContext`
- migrations
- persistence configuration
- database access concerns tied to persistence

Forbidden content:
- frontend transport models
- controller code
- pattern detection logic
- recommendation logic
- scoring logic
- explanation logic
- business reinterpretation of persisted facts

Mandatory persistence rules for V1:
- normalize all common queryable fields that are cross-pattern business facts
- store family-specific detailed artifacts in versioned JSON only when they are not stable universal fields
- keep situation codes and recommendation scenario codes queryable when they are part of business history and auditability
- persist rule version and analysis timestamp when a snapshot is part of business truth

Refusal rules:
- if persistence code recalculates recommendation, the design is invalid
- if the schema stores only opaque JSON where common history fields should be queryable, the design is invalid

### BackPredictFinance.ViewModels

This project contains frontend transport/view models only.

Allowed content:
- request models
- response models
- API-facing paging/list shapes
- AutoMapper profiles related to transport mapping

Forbidden content:
- business-core contracts
- service logic
- persistence entities
- backend engine objects exposed by convenience
- reusable backend shared objects added only to save time

Mandatory rules:
- one class per file
- expose a stable frontend projection, not backend internal truth objects
- do not leak EF entities or backend contracts directly to the frontend
- if the frontend needs a new shape, create or update a ViewModel rather than exposing an internal contract

Refusal rule:
- if a ViewModel exists only as a mirror of an internal backend contract with no frontend projection purpose, the design is invalid

### BackPredictFinance.Common

This project must remain very small.

Allowed content:
- genuinely shared cross-project classes with broad reuse
- ultra-generic helpers
- enums that are truly shared across multiple projects and not specific to the analysis domain

Forbidden content:
- analysis-engine contracts that belong in `BackPredictFinance.Contracts`
- service logic
- persistence entities
- frontend transport models
- dumping-ground shared types created only to avoid choosing the right project

Mandatory rule:
- if a type is specific to the analysis domain, prefer `BackPredictFinance.Contracts` over `BackPredictFinance.Common`

Refusal rule:
- if `Common` grows because classification is avoided, the architecture is degrading and must be corrected

### BackPredictFinance.Tests

This project provides behavior proof.

Required test categories for backend analysis work:
- unit tests for pattern detection / validation / invalidation / scoring
- unit tests for situation classification
- unit tests for recommendation policy behavior
- unit tests for persistence mapping
- targeted integration tests for end-to-end analysis execution
- targeted integration tests for history snapshot persistence
- targeted API tests for request/response projection when the API surface changes

Mandatory rules:
- tests must prove business behavior, not implementation trivia alone
- tests must cover negative paths when the business risk of silent failure is meaningful
- tests must verify that no-credible-pattern output is explicit when appropriate
- tests must verify portfolio-context-sensitive recommendation when recommendation behavior changes

Refusal rule:
- passing tests do not justify duplicated business truth or misplaced architecture

### FinanceFront

This is the frontend presentation layer.

Allowed responsibilities:
- watchlist UI
- portfolio UI
- analysis display
- explanation display
- history comparison display
- filters and presentation-only derived state

Forbidden responsibilities:
- core pattern detection logic
- recommendation truth
- scoring truth
- hidden reimplementation of backend business rules

Mandatory rule:
- frontend computed display state must stay presentation-focused and must not become a second source of truth

Refusal rule:
- if a frontend transformation changes business meaning rather than presentation only, the design is invalid

## Optional new project rule

Do not add a new project by default.

A new project is allowed only if all of the following are true:
- the current project boundaries would otherwise become materially less maintainable
- the new project has a single clear responsibility
- the dependency direction remains simpler after the split
- the split removes real coupling rather than cosmetic size
- the need is explained explicitly with repository evidence

If a new backend project becomes necessary, the only preferred addition is:
- `BackPredictFinance.Analysis`

Purpose of this optional project:
- isolate deterministic analysis-domain services when `BackPredictFinance.Services` would otherwise become a high-coupling monolith

If this project is added:
- `BackPredictFinance.Analysis` may contain analysis-domain services only
- service interfaces still remain with their implementing services by default
- `BackPredictFinance.API` depends on backend services through interfaces only
- `BackPredictFinance.Analysis` depends on `BackPredictFinance.Contracts`, `BackPredictFinance.Common`, and `BackPredictFinance.Datas` only as justified by real code
- do not create this project for cosmetic reorganization only

## Layer placement decision rule

When a type is created or moved, choose its project using this order:
1. Is it a frontend input/output transport model? -> `BackPredictFinance.ViewModels`
2. Is it a database entity or persistence concern? -> `BackPredictFinance.Datas`
3. Is it a backend business-core contract not meant for the frontend? -> `BackPredictFinance.Contracts`
4. Is it a genuinely common shared class or enum reused broadly across projects and not domain-specific? -> `BackPredictFinance.Common`
5. Is it service logic or a service interface? -> `BackPredictFinance.Services` or `BackPredictFinance.Analysis` if that optional project has been explicitly justified and created

If classification is ambiguous, stop and state the ambiguity explicitly instead of guessing.

## Architecture validity lock

The repository architecture must remain valid after every change.

Mandatory rules:
- do not move a type to another project unless the destination project is explicitly justified by the layer-placement decision rule
- do not leave a project with types that violate its contractual role
- do not solve a local compile issue by placing a type in the wrong project
- do not introduce a new shared type in `ViewModels`
- do not introduce a new frontend transport type in `Contracts` or `Common`
- do not introduce a new business-core backend contract in `ViewModels`
- do not introduce a new persistence entity outside `BackPredictFinance.Datas`
- do not split or move files for cosmetic reasons only
- after each architectural change, explicitly verify that `API`, `ViewModels`, `Services`, `Datas`, `Contracts`, and `Common` still respect their contractual boundaries

Before creating a new type, classify it explicitly as one of:
- frontend transport model
- service or service interface
- persistence entity or persistence concern
- backend business-core contract
- genuinely shared common type or enum

If the classification is ambiguous, stop and ask the minimum blocking question.

## Pattern-engine contract

Pattern detection is core business logic and must remain deterministic where business rules require determinism.

The active V1 architecture must support a small real multi-pattern scope without speculative over-engineering.

Current continuation-pattern target:
- `RectangleContinuation`
- `SymmetricalTriangleContinuation`
- `BullFlagContinuation`
- `BearFlagContinuation`

Pattern family rule:
- use families only when they carry a real shared artifact shape or real shared business invariants
- do not introduce family hierarchies for naming symmetry only

Each pattern definition must be able to declare:
- unique identifier
- display name
- family identifier
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

Rectangle business rule:
- `RectangleContinuation` is strictly a continuation pattern in this repository
- it requires a proven prior trend
- it validates only on breakout in the direction of that prior trend
- an opposite breakout does not validate the pattern

## Advice and recommendation contract

Do not treat recommendation as raw text attached directly to the pattern implementation.

The architecture must separate:
- analytical facts produced by the pattern engine
- situation classification
- recommendation decision
- pedagogical explanation rendering

Mandatory advice truth rule:
- the source of truth is structured data, not rendered wording

Minimum structured advice truth expected:
- `SituationCode`
- `AdviceScenarioCode`
- `RecommendationAction`
- `RecommendationStrength`
- structured recommendation parameters

Rendered wording may be persisted for audit/history, but it is not the primary truth.

Recommendation must depend on:
- pattern situation
- portfolio context
- risk metrics
- confidence / validation state

Do not make recommendation depend only on pattern name.

Refusal rule:
- if recommendation truth is primarily embedded in text templates or duplicated inside pattern implementations, the design is invalid

## Persistence and history contract

The system must support analysis snapshots over time.

The persisted history should preserve:
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
- situation code
- advice scenario code

Do not implement fake history or fake versioning.

## API contract rules

Public API contracts must remain stable frontend-facing projections.

Mandatory rules:
- controllers return `ViewModels`, not backend contracts
- controllers never expose `Datas` entities directly
- backend analysis contracts may evolve independently from frontend projection contracts
- if a new UI payload shape is needed, create or update a `ViewModel`

Do not use backend contracts as API shortcuts.

## Mapping rules

Mapping boundaries must be explicit.

Mandatory mapping boundaries:
- `Contracts` <-> `Datas` through dedicated persistence mapping owned by backend application services
- `Contracts` <-> `ViewModels` through dedicated API mapping or AutoMapper profiles
- no ad hoc duplication of mapping logic across controllers, services, and persistence code

Refusal rules:
- entity-to-view-model mapping inside controllers is invalid
- contract-to-entity mapping scattered across unrelated services is invalid

## Naming and vocabulary rules

Naming must be explicit, precise, and in English in code.

Mandatory rules:
- use English for variable names, parameter names, property names, method names, class names, and file names
- prefer explicit names over short or vague names
- avoid names like `data`, `info`, `value`, `item`, `obj`, `result2`, `tmp`, `misc`, or other low-information names unless the scope is extremely local and the meaning is still explicit
- use precise business vocabulary consistently for the same concept across projects
- do not use two different names for the same business concept in the same active path unless compatibility requires it and the reason is explicit
- avoid ambiguous abbreviations unless they are standard, well known in the repository, and still explicit
- keep user-facing texts in the language required by the product, but keep code identifiers in English

When renaming for clarity:
- do not perform broad cosmetic renaming out of scope
- rename only when the current naming creates real ambiguity, contradiction, or maintenance cost
- preserve architectural consistency across the touched path

## Anti-duplication rules

The agent must actively avoid code duplication and duplicated business truth.

Mandatory rules:
- one business rule should have one active source of truth in the touched scope
- do not duplicate the same decision logic in controller, service, mapper, persistence projection, and tests
- do not duplicate mapping logic when one reusable mapper or helper can own it cleanly
- do not duplicate constants, policy versions, error messages, pattern identifiers, or fallback rules across unrelated files when a single repository-grounded location can own them
- do not duplicate runtime truth between response shaping and persistence shaping when one shared contract or helper can keep them aligned
- do not duplicate validation rules across layers unless the duplication is explicitly required and justified
- do not introduce copy-paste variants of existing logic to make one scenario pass quickly

If duplication is found, the agent must do one of the following:
- remove it now when it is in scope and safe
- isolate it and classify it explicitly as a compatibility constraint
- stop and state why removing it would exceed the current scope

The agent must never present duplicated logic as acceptable just because tests pass.

## Security and code quality contract

All code changes must aim for high rigor, maintainability, and security.

Mandatory secure-development rules:
- validate all external inputs at trust boundaries
- encode or sanitize outputs according to the sink when applicable
- enforce authentication and authorization explicitly
- keep session and token handling explicit and minimal
- protect secrets and sensitive data from source code, logs, and error messages
- use approved cryptographic primitives through platform libraries only
- prefer parameterized data access over dynamic query construction
- keep error handling explicit without leaking sensitive internals
- log for diagnosis and audit without logging secrets or unnecessary personal data
- prefer secure defaults in configuration
- use least privilege for infrastructure and data access
- keep dependencies minimal and justified
- preserve trust boundaries explicitly in design and implementation
- avoid insecure fallback behavior that silently broadens access or weakens validation

Mandatory maintainability rules:
- keep methods focused and cohesive
- keep side effects explicit
- avoid hidden temporal coupling
- avoid dead code
- avoid unreachable branches
- keep nullability and invariants explicit
- use types, names, and contracts that reduce ambiguity
- keep complexity bounded and justified
- make quality-gate failures blocking until explicitly arbitrated
- do not keep code that only exists as speculative extension with no active use

Mandatory quality-gate rule:
- a change is not complete if it knowingly leaves unresolved critical security, reliability, or maintainability regressions in the touched scope without explicit arbitration

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

## Minimum proof requirements for serious analysis work

Any serious analysis-engine audit or corrective delivery must include:
- current runtime-path audit
- dependency direction audit in the touched scope
- active configuration audit in the touched scope
- pattern-engine audit
- recommendation-boundary audit
- snapshot/history audit
- targeted test audit
- anti-duplication audit in the touched scope
- security-impact review in the touched scope

If implementation is performed, also include:
- exact files changed
- exact reason for each change
- validation commands executed
- explicit residual risks
- explicit statement of any deferred issue

## Validation commands and expectations

Choose validation based on the touched area.

### For .NET changes

Prefer relevant commands such as:
- `dotnet restore BackPredictFinance.sln`
- `dotnet build BackPredictFinance.sln`
- `dotnet test BackPredictFinance.Tests/BackPredictFinance.Tests.csproj`

### For frontend changes

Prefer relevant commands such as:
- install command appropriate for the frontend package manager in the repository
- build command appropriate for the frontend package manager in the repository
- test command when configured

Do not claim runtime-path correctness without repository proof.
Do not claim security improvement without pointing to the concrete touched trust boundary or risk reduction.

## Output contract for significant tasks

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
- mandatory analysis outputs are not traceable to backend-owned logic
- recommendation truth is still primarily text-based or duplicated across layers
- market eligibility remains distributed, implicit, or unproven
- snapshot persistence omits required trace/version/context fields
- tests do not prove the claimed corrective behavior
- a response mixes proven facts with assumptions without explicit classification
- the touched code introduces avoidable security weaknesses or unresolved quality-gate regressions without explicit arbitration
- a project boundary is broken for convenience
- a new project was added without meeting the explicit rule of this contract

## Scope control

Prioritize V1 delivery.
Do not drift into later phases unless the task explicitly asks for it.

V1 priorities are:
- API as source of truth
- deterministic and explainable pattern analysis
- explicit market eligibility enforcement
- separation of business layers
- portfolio-aware recommendation wording
- analysis history foundation
- extensibility for assets, providers, and patterns
- strong maintainability and secure-by-default implementation
