# Contract authority

This file is the binding working contract for any agent operating on this repository.

It is not guidance.
It is not optional.
It is not a style suggestion.

If repository reality contradicts an assumption, repository reality wins.
If this contract defines a stricter rule than a generic habit, this contract wins.
If a task request contradicts this contract, the contradiction must be stated explicitly before implementation.

An agent must never silently reconcile contradictions.
An agent must never present a proposal as if it were already implemented.
An agent must never use convenience to break project boundaries.

## Before you start (operational entry points)

This contract is authoritative. For the *how* — workflow, where things live, and mistakes already made here — read the in-repo wiki under [`Documentation/`](Documentation/README.md):
- [`Documentation/agent-guide.md`](Documentation/agent-guide.md) — how to work in this repo (golden rules, boundaries, conventions, build/test).
- [`Documentation/pitfalls.md`](Documentation/pitfalls.md) — **mistakes not to reproduce** (real traps + the correct approach). Read it before touching the analysis engine, persistence, config, or CI.
- `Documentation/` also covers architecture, data model, API surface, CI/CD, deployment, and secrets.

## Repository purpose

This repository contains the PredictFinance V1 implementation baseline:
- the backend solution under `FinanceBack`
- the frontend Angular application under `FinanceFront`
- the canonical and operating documentation under `Doc`

The repository goal is to deliver a pedagogical investment-analysis product for beginner retail users.

The product goal is to help a user:
- maintain a watchlist
- maintain a portfolio
- store multiple purchase lines per instrument
- analyze daily market prices
- detect chart patterns
- understand possible scenarios
- receive pedagogical recommendations
- review analysis history over time
- compare tracked instruments with an explicit fundamental scoring layer when that capability is implemented

This product is:
- an educational investment-analysis and guidance tool
- not a broker
- not an order execution system
- not a bank-account integration
- not a real-time tick-by-tick trading platform

## Repository reality observed in the current repository

Observed top-level repository areas:
- `FinanceBack` contains the `BackPredictFinance.sln` backend solution
- `FinanceFront` contains the Angular web application
- `Doc` contains canonical product contracts, gap audits, and agent-operating documents

Observed projects in `BackPredictFinance.sln`:
- `BackPredictFinance.API`
- `BackPredictFinance.Services`
- `BackPredictFinance.Datas`
- `BackPredictFinance.ViewModels`
- `BackPredictFinance.Common`
- `BackPredictFinance.Patterns`
- `BackPredictFinance.Tests`

Observed repository facts that matter for architecture control:
- there is no `BackPredictFinance.Contracts` project in the current solution
- `FinanceFront` exists in the current repository and must be treated as a real project boundary
- `BackPredictFinance.Services` currently references `BackPredictFinance.ViewModels`
- part of the current analysis-domain contract still lives under `BackPredictFinance.Common/AnalysisV1`
- `BackPredictFinance.Patterns` already exists and must be treated as a real project boundary

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
- Fundamental scoring and pattern analysis are different business capabilities and must not be merged into one opaque score.
- PEA eligibility status must remain explicit and traceable whenever the scoring capability is implemented.

## V1 scope lock

Target user:
- beginner retail investor

Initial market scope:
- French listed equities only

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

Technical-analysis output must be able to provide:
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

Fundamental-scoring output, when implemented, must be able to provide:
- explicit universe id
- explicit PEA eligibility status
- category scores
- coverage ratio
- composite score only when product rules allow it
- deterministic traceability metadata

If multiple patterns are compatible:
- keep all compatible patterns
- do not arbitrarily force a single winner
- explain why multiple compatible patterns are shown
- preserve separate calculations and traceability

V1 multi-pattern execution must follow these invariants:
- internal analysis contracts must support zero, one, or many requested pattern identifiers
- an omitted requested-pattern input means the API default enabled set, not an implicit mono-pattern fallback
- the runtime may execute one or many enabled patterns for the same instrument when the active path requires it
- response, persistence, and history must always preserve the executed pattern set explicitly
- no service, mapper, compatibility layer, or persistence writer may reject a supported enabled pattern only because a previous increment was mono-pattern
- a display-primary pattern may exist for UX, but it must never erase alternative compatible patterns from the response or the snapshot

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
- confidence / scoring for technical patterns
- risk evaluation
- situation classification
- recommendation decision
- pedagogical explanation rendering
- portfolio contextualization
- persistence and analysis snapshot history
- fundamental scoring
- PEA eligibility registry and traceability

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
- direct exposure of `Datas` entities to clients
- service location through `IServiceProvider`
- ad hoc mapping logic duplicated in controllers

Mandatory rules:
- controllers depend on service interfaces from backend service projects only
- public payloads are `BackPredictFinance.ViewModels` types or simple framework primitives when repository-proven
- `Program.cs` and bootstrap files contain bootstrap only
- middleware remains cross-cutting and must not own domain decisions
- controllers must not return backend internal contracts directly

### BackPredictFinance.Services

This project contains business services and application orchestration.

Allowed responsibilities:
- use-case orchestration
- market-data workflows
- eligibility workflows
- deterministic analysis workflows
- validation / invalidation workflows
- scoring workflows
- risk workflows
- situation classification workflows
- recommendation workflows
- pedagogical explanation rendering workflows
- persistence orchestration
- history orchestration
- fundamentals scoring orchestration when that capability is implemented

Mandatory rules:
- services and their interfaces belong here unless a separately justified backend project is explicitly created
- the interface stays in the same file as its implementing service by default
- create a separate interface file only when the same interface is implemented by multiple services or when a strong repository-proven reason requires it
- keep services organized by capability, not by convenience
- if a service file starts absorbing unrelated orchestration, split by capability before adding more logic
- keep controllers thin and keep business orchestration in services
- use `BaseService` only when it reduces duplication without artificial inheritance
- no service may return EF entities as business truth
- no service may use frontend concerns as inputs or outputs to internal domain logic
- any current `Services -> ViewModels` dependency is a repository reality, not an architectural target
- no new service-to-ViewModel coupling may be introduced

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
- `Fundamentals`

Dependency discipline inside Services:
- `Application` may orchestrate the other capabilities
- `Advice` depends on facts/contracts, not on controller concerns
- `Risk` depends on facts/contracts, not on UI concerns
- `Persistence` maps contracts to persistence, but must not recalculate business truth
- `Patterns` produces analytical facts and traces, but must not generate final frontend wording
- `History` persists or retrieves prior outputs, but must not reinterpret analytical truth
- `Fundamentals` owns ranking/scoring logic but must not absorb chart-pattern logic
- cyclic dependencies between capabilities are forbidden

Refusal rules:
- if one service becomes the default place for unrelated decisions, split by capability before extending it further
- if recommendation logic is copied into pattern definitions, the design is invalid
- if scoring truth is duplicated between chart-pattern flow and fundamentals flow, the design is invalid

### BackPredictFinance.Patterns

This project contains deterministic pattern-definition logic and pattern registry concerns.

Allowed content:
- pattern identifiers
- pattern descriptors
- pattern registry support
- deterministic pattern definitions
- shared helpers that are specific to pattern execution
- pattern execution artifacts that are specific to the pattern engine

Forbidden content:
- HTTP/controller concerns
- EF entities
- frontend transport DTOs
- recommendation wording as primary business truth
- persistence recalculation logic
- unrelated fundamentals scoring logic

Mandatory rules:
- pattern definitions stay deterministic
- pattern support truth must not be duplicated across controllers, services, and frontend metadata
- pattern definitions may produce analytical facts, but not final API wording ownership
- pattern-specific artifacts must stay inside `BackPredictFinance.Patterns` unless a stronger dedicated contract project is explicitly created later

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
- if PEA eligibility is implemented, persist the registry fact and its trace metadata explicitly
- new persistence entity classes must follow the repository comment convention for entity documentation

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
- new or materially rewritten transport types should follow one-class-per-file unless a repository-proven exception exists
- expose a stable frontend projection, not backend internal truth objects
- do not leak EF entities or backend pattern artifacts directly to the frontend
- if the frontend needs a new shape, create or update a ViewModel rather than exposing an internal contract

### BackPredictFinance.Common

This project must remain very small.

Allowed content:
- genuinely shared cross-project classes with broad reuse
- ultra-generic helpers
- enums that are truly shared across multiple projects and not specific to one business capability

Forbidden content:
- pattern-engine contracts that belong specifically to `BackPredictFinance.Patterns`
- service logic
- persistence entities
- frontend transport models
- dumping-ground shared types created only to avoid choosing the right project

Mandatory rule:
- if a type is specific to analysis, fundamentals scoring, or one business capability, prefer a more specific project boundary over `Common`
- current `Common/AnalysisV1` content is a repository reality to reduce progressively, not a target to extend further

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
- targeted tests for fundamentals scoring and PEA rules when that capability is implemented

Mandatory rules:
- tests must prove business behavior, not implementation trivia alone
- tests must cover negative paths when the business risk of silent failure is meaningful
- tests must verify that no-credible-pattern output is explicit when appropriate
- tests must verify portfolio-context-sensitive recommendation when recommendation behavior changes
- tests must verify that PEA status and coverage rules gate composite score output when the scoring capability is added

### FinanceFront

This folder contains the Angular web application.

Allowed responsibilities:
- route declaration and navigation structure
- guards and session-aware redirection
- HTTP calls to backend endpoints
- page composition and state rendering
- deterministic rendering of backend-owned truth
- user input capture and form submission
- accessibility, responsiveness, and shell/layout behavior

Forbidden responsibilities:
- business-core truth recomputation
- pattern detection logic
- support-reading or PEA eligibility recomputation
- recommendation-action selection logic
- hidden frontend-owned wording truth
- presenting mock data as governed product truth
- targeting retired endpoints as the current implementation target

Mandatory rules:
- the frontend renders backend-owned business truth and must not become a second source of truth
- if a backend payload is insufficient, fix the backend projection or add an explicit transport shape instead of deriving hidden truth in the frontend
- the same backend state must map to the same French wording family across pages
- temporary placeholders or mock values must remain explicitly labeled and must not be documented as implemented governed behavior
- frontend docs, examples, and task backlogs must not present retired routes such as `/api/Trading/predict/{symbol}` as the active target when repository code proves otherwise
- Bootstrap must be used as the default frontend styling foundation whenever it can satisfy the requirement without breaking repository constraints
- page-specific or component-specific CSS must not be introduced when Bootstrap utility/classes or repository-shared theme rules can solve the need cleanly
- shared frontend design truth lives under `FinanceFront/src/themes`
- `FinanceFront/src/styles.scss` is the only runtime entry point for shared theme imports and global design orchestration
- component-level SCSS must remain limited to local geometry or strictly local exceptions and must not redefine shared primitives such as colors, spacing scales, shadows, radii, buttons, inputs, tables, badges, dropdowns, navigation shells, or shared state panels
- page-specific styling that is not a strictly local geometry exception is forbidden in page/component stylesheet files and must instead be implemented with Bootstrap or moved into `FinanceFront/src/themes`
- if a visual pattern appears on more than one screen, it must be promoted into `FinanceFront/src/themes` instead of being copied into multiple component stylesheets
- reusable design concerns must not be implemented with inline style attributes in templates when a semantic class plus centralized theme rule can own them

### Doc

This folder contains canonical product contracts, gap audits, summaries, and agent-operating files.

Allowed content:
- canonical product contracts
- repository-truth gap audits
- derived summaries and reading aids
- implementation backlogs that remain explicit about status classification
- agent-operating documents that explain how to work safely from canonical contracts

Forbidden content:
- silently overriding canonical product decisions
- documenting target-state architecture as already implemented repository truth
- restating repository mismatches as if they were resolved
- treating the root `README.md` as a competing normative source

Mandatory rules:
- `Doc/v1/00_INDEX.md` remains the canonical map for documentation authority
- agent-operating files may interpret how to work safely, but they must not silently override canonical product contracts
- documentation must distinguish `PROVEN`, `DECIDED`, `PROPOSED`, `DEROGATION`, and `REMAINING TO ARBITRATE`
- documentation must distinguish an existing endpoint with an insufficient payload from a truly missing route
- the root `README.md` is an entry point for humans, not a normative contract that overrides canonical files

## Optional new project rule

Do not add a new project by default.

A new project is allowed only if all of the following are true:
- the current project boundaries would otherwise become materially less maintainable
- the new project has a single clear responsibility
- the dependency direction remains simpler after the split
- the split removes real coupling rather than cosmetic size
- the need is explained explicitly with repository evidence

If a new backend project becomes necessary, the preferred additions are, in order of justification:
- `BackPredictFinance.Contracts` for backend business-core contracts that are currently missing as a dedicated boundary
- `BackPredictFinance.Analysis` only if `BackPredictFinance.Services` would otherwise become a high-coupling monolith

Do not create either project for cosmetic reorganization only.

## Layer placement decision rule

When a type is created or moved, choose its project using this order:
1. Is it a frontend input/output transport model? -> `BackPredictFinance.ViewModels`
2. Is it a database entity or persistence concern? -> `BackPredictFinance.Datas`
3. Is it a pattern-definition artifact specific to pattern execution? -> `BackPredictFinance.Patterns`
4. Is it a genuinely common shared class or enum reused broadly across projects and not capability-specific? -> `BackPredictFinance.Common`
5. Is it service logic or a service interface? -> `BackPredictFinance.Services`
6. Is it a backend business-core contract that needs a dedicated boundary and the repository has explicitly introduced that project? -> `BackPredictFinance.Contracts`

If classification is ambiguous, stop and state the ambiguity explicitly instead of guessing.

## Internal folder organization rule

Files inside each project must be organized by capability and responsibility, not by convenience or random accumulation.

Mandatory rules:
- place new files under the nearest existing capability folder inside the owning project when that folder already matches the responsibility
- if no suitable folder exists in the owning project, create a clearly named folder for that capability inside that same project
- prefer colocating related files in their owning project boundary rather than creating informal cross-cutting dumping areas
- do not create generic folders such as `Misc`, `Helpers`, `Utilities`, or `Temp` unless the content is genuinely broad, stable, and repository-proven
- do not use generic catch-all helper files as a convenience dumping ground for unrelated logic
- do not perform broad cosmetic folder moves only to satisfy this rule; apply it incrementally in the touched scope

## Architecture validity lock

The repository architecture must remain valid after every change.

Mandatory rules:
- do not move a type to another project unless the destination project is explicitly justified by the layer-placement decision rule
- do not leave a project with types that violate its contractual role
- do not solve a local compile issue by placing a type in the wrong project
- do not introduce a new shared type in `ViewModels`
- do not introduce a new frontend transport type in `Common`
- do not introduce a new persistence entity outside `BackPredictFinance.Datas`
- do not use partial classes, nested dumping patterns, or similar indirection to hide oversized-file or mixed-responsibility problems unless repository reality already requires it and the reason is stated explicitly
- do not split or move files for cosmetic reasons only
- after each architectural change, explicitly verify that `API`, `Services`, `Datas`, `ViewModels`, `Common`, `Patterns`, and `Tests` still respect their contractual boundaries

## Pattern-engine contract

Pattern detection is core business logic and must remain deterministic where business rules require determinism.

The active V1 architecture must support a small real multi-pattern scope without speculative over-engineering.

Current continuation-pattern target:
- `RectangleContinuation`
- `SymmetricalTriangleContinuation`
- `BullFlagContinuation`
- `BearFlagContinuation`

Transitional compatibility rule:
- temporary compatibility inputs may still accept a singular requested-pattern field when repository reality requires it
- this compatibility input must be resolved immediately into an explicit internal pattern-id list
- compatibility mappers may adapt legacy enums or legacy DTOs, but they must never become the source of truth for which V1 patterns are supported
- no runtime path may remain permanently blocked on `DOUBLE_TOP` or any other single pattern once V1 is declared multi-pattern in the touched scope

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

## Fundamentals scoring and PEA contract

Fundamental scoring is a separate backend-owned capability.

Mandatory rules:
- the frontend must not recompute the scoring logic
- the universe id used for a score must be explicit
- PEA eligibility status must be explicit
- `UNKNOWN` PEA status must never be silently treated as eligible
- a composite score must not be emitted when product rules forbid it
- the product registry is the truth for PEA status, not a silent inference from one provider field
- raw provider fundamentals and product eligibility registry facts must remain distinct in design and persistence

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
- controllers return `ViewModels`, not backend internal artifacts
- controllers never expose `Datas` entities directly
- backend analysis contracts may evolve independently from frontend projection contracts
- if a new UI payload shape is needed, create or update a `ViewModel`

Do not use backend internal contracts as API shortcuts.

## Mapping rules

Mapping boundaries must be explicit.

Mandatory mapping boundaries:
- backend internal artifacts <-> `Datas` through dedicated persistence mapping owned by backend application services
- backend internal artifacts <-> `ViewModels` through dedicated API mapping or AutoMapper profiles
- no ad hoc duplication of mapping logic across controllers, services, and persistence code
- compatibility mapping from legacy mono-pattern DTOs or enums must stay explicitly labeled as compatibility and must not constrain supported internal pattern ids

## Naming and vocabulary rules

Naming must be explicit, precise, and in English in code.

Mandatory rules:
- use English for variable names, parameter names, property names, method names, class names, and file names
- prefer explicit names over short or vague names
- avoid names like `data`, `info`, `value`, `item`, `obj`, `result2`, `tmp`, or `misc` unless the scope is extremely local and the meaning is still explicit
- use precise business vocabulary consistently for the same concept across projects
- do not use two different names for the same business concept in the same active path unless compatibility requires it and the reason is explicit
- avoid ambiguous abbreviations unless they are standard, well known in the repository, and still explicit
- keep user-facing texts in the language required by the product, but keep code identifiers in English

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
- new or materially rewritten files must stay focused on one cohesive responsibility
- do not extend a large file when doing so would turn it into or keep it as a catch-all file
- when a file accumulates unrelated responsibilities, split by capability before adding more behavior
- file splitting must be driven by cohesion and responsibility boundaries, not by cosmetic reshuffling alone
- a file is considered too large when cohesion is lost, unrelated responsibilities accumulate, or routine work requires navigating across unrelated logic blocks
- if a large or mixed-responsibility file is kept temporarily because a safe split is out of scope or blocked by repository reality, that choice must be stated explicitly as `DEROGATION`
- make quality-gate failures blocking until explicitly arbitrated
- do not keep code that only exists as speculative extension with no active use

## Engineering rigor rules

- No invented requirements
- No hidden assumptions
- No fake business logic
- No fake data semantics
- No silent behavior changes
- No dead code introduced
- No broad rewrite outside the scope actually needed
- No comments inside code unless repository conventions or the specific task explicitly require them
- Repository convention requires concise comments for each new persistence entity class and for each new interface method
- Required comments must describe business intent, contract, or important semantics, not restate obvious implementation steps
- Prefer concise XML documentation comments in C# for required code comments
- No claim of compliance without repository proof
- No mixing of audit facts and proposals without explicit labeling

When information is missing:
- stop
- identify the exact missing element
- ask only the minimum blocking question

## Minimum proof requirements for serious analysis work

Any serious analysis-engine or scoring-engine audit or corrective delivery must include:
- current runtime-path audit
- dependency direction audit in the touched scope
- active configuration audit in the touched scope
- pattern-engine audit when relevant
- recommendation-boundary audit when relevant
- snapshot/history audit when relevant
- fundamentals scoring / PEA audit when relevant
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

### Useful validation commands from repository root

Prefer relevant commands such as:
- `dotnet restore FinanceBack/BackPredictFinance.sln`
- `dotnet build FinanceBack/BackPredictFinance.sln`
- `dotnet test FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj`
- `dotnet run --project FinanceBack/BackPredictFinance.API`
- `npm install --prefix FinanceFront`
- `npm start --prefix FinanceFront`
- `npm run build --prefix FinanceFront`
- `npm test --prefix FinanceFront`

### For .NET changes in `FinanceBack`

Prefer relevant commands such as:
- `dotnet restore FinanceBack/BackPredictFinance.sln`
- `dotnet build FinanceBack/BackPredictFinance.sln`
- `dotnet test FinanceBack/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj`

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
- a scoring or PEA implementation silently infers product eligibility instead of exposing explicit eligibility truth

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
- deterministic fundamental scoring foundation
- explicit PEA eligibility traceability
- extensibility for assets, providers, and patterns
- strong maintainability and secure-by-default implementation
