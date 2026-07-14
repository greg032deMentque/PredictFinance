# PredictFinance V1 — Codex Execution Plan v3 for `PredictFinance_V1_Sprint_Backlog_Operational`

## Purpose
This document is the execution contract for Codex to implement the backend backlog of PredictFinance V1 in the real repository.

It is intentionally stricter than the previous versions.
It merges:
- the repository-grounded context from the first plan
- the blocker gates and security hardening from the second plan
- the proven corrections raised by the Codex audit
- an explicit create / extend / forbid matrix so Codex does not drift into speculative architecture

This document is backend-oriented.
Its purpose is to guide the creation or modification of **controllers, services, view models, entities, EF configuration, migrations, and tests** required by `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`, while preserving repository truth.

---

## 1. Source of truth hierarchy
Codex must apply the following precedence order.

1. Real repository code under `FinanceBack/`
2. Canonical backlog in `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`
3. Canonical product/API documents under `Doc/product/`
4. This execution plan
5. Any earlier chat or earlier Codex assumption

If a lower-priority source contradicts a higher-priority source, higher-priority source wins and the mismatch must be reported explicitly.

---

## 2. Repository truth Codex must start from

### PROVEN repository structure
The backend solution currently contains these projects:
- `BackPredictFinance.API`
- `BackPredictFinance.Services`
- `BackPredictFinance.Datas`
- `BackPredictFinance.ViewModels`
- `BackPredictFinance.Common`
- `BackPredictFinance.Patterns`
- `BackPredictFinance.Tests`

### PROVEN current controller surface
The backend already exposes at least these controllers:
- `AccountController`
- `ClientFinanceController`
- `FundamentalsController`
- `TickersController`
- `TradingController`
- `UserController`

### PROVEN currently relevant persisted analysis/history domain
The backend already contains at least these persisted entities that matter for user-core, history, and comparison work:
- `AnalysisRun`
- `PatternAssessment`
- `DecisionSignal`
- `ModelSnapshot`
- `Recommendation`
- `AssetQuoteSnapshot`
- `AssetCandleSnapshot`
- `AssetPeaEligibility`

### PROVEN already existing reusable analysis infrastructure
The backend already contains reusable portfolio/snapshot infrastructure and Codex must audit and reuse it before creating replacements, including:
- existing portfolio context reconstruction logic
- existing analysis snapshot persistence logic
- existing analysis result projection logic

### PROVEN current security/session reality
- Sprint 1 already requires `GET /api/Account/me` in the canonical backlog.
- The frontend currently infers admin capabilities from JWT-side assumptions that are not fully guaranteed by the backend token payload.
- Current backend JWT generation explicitly emits roles, but not the broader claim set the frontend appears to expect.
- Existing authorization gaps are already present in the live controller surface and must be fixed before backlog expansion.

### PROVEN retired surface reality
`TradingController` is retired and already returns `410 Gone`.
It must remain quarantined unless a future approved product decision explicitly changes that contract.

### PROVEN technical derogation reality
Current repository reality already contains debt that Codex must acknowledge rather than normalize:
- `Services -> ViewModels` coupling already exists
- some persisted closed-state fields are stored as `string`

These are derogations from target architecture, not target architecture itself.

### DECIDED repository boundaries
- Controllers remain HTTP-only.
- Services contain orchestration and business coordination.
- Entities live only in `BackPredictFinance.Datas`.
- View models live only in `BackPredictFinance.ViewModels`.
- Tests live only in `BackPredictFinance.Tests`.
- Codex must not add a new project.

---

## 3. Non-negotiable execution rules

Codex must follow all of the rules below in every milestone.

### Rule A — Never invent repository state
Codex must not claim that a route, entity, service, or persistence gap exists before proving it from code.

### Rule B — Audit before creation
Before adding a new controller, route, entity, or migration, Codex must classify the current state using only these labels:
- `PROVEN_EXISTS`
- `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`
- `PROVEN_MISSING_ROUTE`
- `PROVEN_AUTHZ_GAP`
- `PROVEN_MISSING_PERSISTENCE`
- `REMAINING_TO_ARBITRATE`
- `DEROGATION`

### Rule C — Extend before duplicating
If an existing controller or service can be extended safely and coherently, Codex must extend it instead of creating a parallel surface.

### Rule D — Small bounded services only
Codex must not turn `ClientFinanceService`, `AccountService`, or any other service area into an opaque god service.

### Rule E — No speculative persistence
A new entity or migration is allowed only if the backlog requires durable governed truth and current persisted truth cannot satisfy the contract through projection, query, or extension of an existing entity.

### Rule F — Authz is a blocker, not cleanup
No feature expansion milestone may proceed until the baseline authorization defects are fixed and proven through integration tests.

### Rule G — Integration proof for security-sensitive changes
Controller unit tests with mocks are not enough for authorization/session gates.
Security-sensitive changes must be proven through integration tests traversing the ASP.NET authorization pipeline.

### Rule H — Closed sets must be locked
When the product contract defines a closed set, Codex must use enums or equivalent locked value sets rather than free-form strings, unless current persistence reality forces a documented derogation.

### Rule I — Retired surface quarantine
`TradingController` must stay retired and must not be silently repurposed to host V1 analysis behavior.

### Rule J — Backend truth governs frontend shell decisions
Frontend shell behavior must not depend on guessing unissued JWT claims.
Sprint 1 must be backed by a backend-governed current-user/session contract.

---

## 4. Explicit derogation register that must exist before wider implementation

Before feature expansion, Codex must create or update an explicit derogation register under `Doc/` if no canonical equivalent already exists.

It must include at least:
- `Services -> ViewModels` coupling exists today
- persisted closed-state fields stored as `string`
- any other proven structural debt that future milestones must not silently amplify

The purpose of this register is not to solve all debt immediately.
Its purpose is to stop Codex from treating derogations as a green light to spread them further.

---

## 5. Global create / extend / forbid matrix

## 5.1 Controllers

### Extend first
Codex must first evaluate extension of these existing controllers:
- `AccountController`
- `ClientFinanceController`
- `UserController`

### Allowed new controllers only when audit proves they are the smallest coherent target
- `HistoryController`
- `LearnController`
- `ParametersController`
- `NotificationsController`
- `AdminOverviewController`
- `AdminInstrumentRegistryController`
- `AdminPeaRegistryController`
- `AdminScoringPolicyController`
- `AdminParameterDictionaryController`
- `AdminWordingVersionsController`
- `AdminSnapshotAuditController`
- `AdminDataQualityController`
- `AdminUsersController` only if extending `UserController` would become structurally incoherent

### Forbidden controller patterns
- a single catch-all admin controller
- a single catch-all user-read controller beyond coherent `ClientFinanceController` extension
- direct EF access from controllers
- returning EF entities directly from controllers
- reactivating `TradingController`

## 5.2 Services

### Allowed service areas
Codex may introduce small bounded services under the existing project structure, including as needed:
- `Auth/...`
- `ClientFinanceServices/Analysis/...`
- `ClientFinanceServices/Portfolio/...`
- `ClientFinanceServices/History/...`
- `ClientFinanceServices/Learn/...`
- `ClientFinanceServices/Parameters/...`
- `Notifications/...`
- `Admin/...`

### Forbidden service patterns
- monolithic service expansion without bounded purpose
- duplicating existing reusable portfolio/snapshot logic without first proving current services are insufficient

## 5.3 View models

### Allowed view model families
- self-session/current-user read models
- dashboard read models
- watchlist read models
- portfolio read models
- analysis detail read models
- history read models
- instrument detail read models
- comparison read models
- learn/runtime-scope read models
- parameter detail read models
- notification read models
- bounded admin read models

### Forbidden view model patterns
- free-form "admin blob" models
- exposing persistence-only internal shapes as public API contract

## 5.4 Entities and migrations

### Allowed only after proof
Codex may create new durable entities only when the contract truly requires governed persisted truth and existing persistence cannot meet it.
Likely candidate families, only after proof:
- parameter dictionary entities
- wording version/publication entities
- notification entities
- scoring policy entities
- onboarding state entities
- data-quality issue entities
- audit helper entities only if snapshot/history tables cannot support the contract otherwise

### Forbidden persistence patterns
- speculative tables “for later”
- persisting UI-only concepts without product need
- adding migrations before the route and contract audit closes the need

---

## 6. Execution sequence Codex must follow

Codex must execute milestone by milestone.
It must not batch the whole backlog into one implementation wave.

For every milestone, Codex must do this exact sequence:
1. rebuild repository truth in the touched area
2. classify every relevant route/persistence point using the approved labels
3. identify blockers first
4. decide whether to extend or create
5. list exact files to change before coding
6. implement only the scoped milestone
7. run validation
8. report residual risks, derogations, and deferred items

---

## 7. Milestone 0 — Sprint 0 baseline audit

## Objective
Produce the canonical backend starting inventory before any implementation work.

This milestone exists because the operational backlog explicitly requires a route and surface audit before implementation starts.

## Hard constraints
During this milestone:
- no migration
- no new controller
- no refactor
- no code movement
- no implementation beyond possibly test-only inspection helpers if absolutely required

## Required outputs
Codex must produce a written audit covering at least:
- current backend route inventory relevant to Sprint 1 through Sprint 7
- classification of each required backlog route as `PROVEN_EXISTS`, `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`, `PROVEN_MISSING_ROUTE`, or `REMAINING_TO_ARBITRATE`
- existing frontend/backend mismatch hotspots that directly affect backend planning
- current auth/session contract reality
- current admin surface reality versus product target
- current persistence reality for history, comparison, notifications, wording governance, parameter dictionary, scoring policy, onboarding, and admin data quality
- explicit list of mock or placeholder surfaces that must not be treated as governed truth

## Required route decisions to leave open if still unproven
Codex must not freeze these by convenience during Milestone 0 if the repository does not yet prove the smallest safe option:
- dedicated portfolio route versus coherent extension of current user-asset read surface
- dedicated history controller versus coherent extension of existing route family
- dedicated learn runtime-scope route versus reuse of existing user route family
- admin user management hosted in `UserController` versus dedicated `AdminUsersController`

## Acceptance criteria
- Codex has a file-by-file backend starting inventory
- every Sprint 1 required backend surface is classified
- any route strategy still open remains explicitly marked `REMAINING_TO_ARBITRATE`
- no implementation has started yet except audit artifacts if needed

---

## 8. Milestone 1A — Security and session baseline

## Objective
Repair current authorization risk, establish backend-governed current-user/session truth, and create the security proof baseline required before any wider backlog delivery.

## This milestone is a hard blocker for all later milestones
Nothing beyond Milestone 1A may proceed until this milestone is implemented and validated.

## Mandatory deliverables
1. Audit current touched routes for authorization defects.
2. Fix proven authorization gaps on existing controllers.
3. Implement `GET /api/Account/me`.
4. Implement `GET /api/Account/me/permissions` only if roles alone are proven insufficient.
5. Define the minimal backend-governed current-user contract needed for Sprint 1 shell behavior.
6. Create or update the technical derogation register.
7. Confirm `TradingController` remains quarantined.
8. Add integration tests proving authz and `/me` behavior through the real ASP.NET pipeline.

## Required current-user payload
At minimum, the backend-governed contract must cover:
- user id
- display name
- email
- roles
- allowed areas: user and/or admin
- optional landing preference only if repository reality already supports it or if later milestone explicitly adds it

## Required enums
- `UserArea`
- `UserRole`
- `PermissionCode` only if permissions beyond roles are truly required

## Expected code changes

### Controllers
- extend `AccountController`
- tighten authorization in `UserController` and any other controller with a proven authz gap

### Services
- current-user/session read service
- policy/helper wiring only if really needed

### View models
- `CurrentUserViewModel`
- minimal capability/read models only if required

### Entities and migrations
Default expectation: none.
A schema change in this milestone requires explicit proof and should be treated as exceptional.

## Validation required
- unauthenticated user cannot reach authenticated endpoints
- authenticated non-admin user cannot reach admin-only endpoints
- `/api/Account/me` returns stable backend-governed truth
- no invented tenant/site/capability claims are introduced without proof
- integration tests cover authorization, not just controller method invocation

---

## 9. Milestone 1B — Sprint 1 shell-enabling contract

## Objective
Deliver the minimal backend contract needed to make the application structurally coherent after login, once the security baseline is closed.

## Mandatory deliverables
1. Re-audit Sprint 1 backend tasks against the now-secure route surface.
2. Keep shell decisions backend-governed rather than JWT-inferred.
3. Introduce closed-set enums immediately needed by Sprint 1 and early Sprint 2 only when product vocabulary is already fixed.
4. Keep the milestone narrow: no admin governance persistence yet.

## Expected code changes

### Controllers
- further targeted `AccountController` extension only if `/me` payload still proves insufficient
- no speculative new controller in this milestone

### Services
- session/auth shell support services only where necessary

### View models
- current-user and shell-enabling response shapes only

### Entities and migrations
Default expectation: none.

## Validation required
- user/admin split no longer depends on guessing non-issued JWT claims
- admin switch visibility is backend-governed
- closed-state values introduced here are enum-backed or equivalently locked
- no widened scope into admin governance or user-core delivery yet

---

## 10. Milestone 2 — Sprint 2 user-core backend

## Objective
Deliver Home, Watchlist, Portfolio, Analysis Entry support, and Analysis Result detail using current persisted truth and existing reusable services where possible.

## Mandatory deliverables
1. Re-audit `ClientFinanceController` and extend it where possible.
2. Reuse existing portfolio context reconstruction instead of rebuilding portfolio truth from zero.
3. Reuse existing snapshot persistence truth where possible.
4. Correct current projection debt explicitly where it conflicts with the V1 product contract.
5. Preserve compatible alternative patterns in the public read model when the contract requires them.

## Required route strategy constraints
- `GET /api/ClientFinance/dashboard` must be enriched, not duplicated.
- `GET /api/ClientFinance/watchlist` must be enriched, not duplicated.
- `GET /api/ClientFinance/portfolio` is allowed only if Milestone 0 proved it is the smallest coherent target.
- `POST /api/ClientFinance/analysis/run` must remain the analysis execution entry if still coherent.
- `GET /api/ClientFinance/analysis/{analysisId}` is the expected read endpoint for detailed result playback.
- runtime-scope support must stay route-strategy-aware and must not prematurely force a dedicated `LearnController` if a smaller safe route exists first.

## Projection debt that must be treated explicitly
This milestone must inspect and resolve the already identified tensions in analysis projection logic, including:
- recommendation semantics collapsed too aggressively into broader actions
- compatible alternatives not clearly preserved in the primary public read model where the product contract requires them

This is not optional cleanup.
This milestone must either fix the behavior or prove that current behavior already matches the contract.

## Expected code changes

### Controllers
- extend `ClientFinanceController`
- optionally add a narrowly scoped portfolio endpoint under the existing controller only if Milestone 0 proved it is needed

### Services
- bounded dashboard projection service if needed
- bounded portfolio read service if needed
- bounded analysis detail projection service
- bounded analysis entry helper service only if route contract requires it

### View models
- `HomeDashboardViewModel`
- `DashboardAttentionItemViewModel`
- `DashboardRecentAnalysisItemViewModel`
- `DashboardIncompleteItemViewModel`
- `WatchlistRowViewModel`
- `WatchlistFilterViewModel`
- `PortfolioViewModel`
- `PortfolioPositionViewModel`
- `AnalysisDetailViewModel`
- compatible alternative pattern item models if the contract requires them

### Entities and migrations
Create none by default.
Sprint 2 should prefer projection over schema expansion unless the contract cannot be satisfied from existing persisted truth.

## Validation required
- Home reads stable backend truth
- Watchlist reads stable backend truth
- Portfolio remains portfolio-aware without changing market-reading truth
- analysis result preserves distinct recommendation semantics where the product contract distinguishes them
- compatible alternatives remain visible when required

---

## 11. Milestone 3 — Sprint 3 history, instrument detail, comparison

## Objective
Deliver Instrument Detail, global History, instrument history, and Snapshot Comparison from persisted truth.

## Mandatory deliverables
1. Audit current history and snapshot persistence before exposing new read routes.
2. Reuse existing analysis entities and persisted snapshots where sufficient.
3. Add new persistence only if the comparison/history contract cannot be proven otherwise.
4. Keep route strategy open until audit proves whether extending an existing family or creating `HistoryController` is cleaner.

## Expected code changes

### Controllers
- extend existing route family if coherent
- create `HistoryController` only if audit proves that is the smallest coherent target

### Services
- history feed service
- instrument detail projection service
- snapshot comparison service

### View models
- `InstrumentDetailViewModel`
- `InstrumentSummaryViewModel`
- `MarketReadingViewModel`
- `SupportReadingViewModel`
- `PersonalSituationReadingViewModel`
- `InstrumentNavigationLinksViewModel`
- `HistoryFeedViewModel`
- `HistoryItemViewModel`
- `InstrumentHistoryViewModel`
- `InstrumentHistoryItemViewModel`
- `SnapshotComparisonViewModel`
- `SnapshotDeltaItemViewModel`

### Entities and migrations
Only if current persisted truth is insufficient:
- patch existing history entities or add missing stable fields
- add migration only after proof of insufficiency

## Validation required
- history endpoints read persisted truth rather than reconstructed guesses
- comparison distinguishes source facts from derived consequences where required
- route strategy remains aligned with audit and backlog, not convenience

---

## 12. Milestone 4 — Sprint 4 learn, parameters, account gaps, onboarding

## Objective
Deliver Learn, Parameter Detail, account self-service gaps, forgot/reset password corrections, and onboarding state only where durable state is actually required.

## Mandatory deliverables
1. Keep account work minimal where current `AccountController` already satisfies the contract.
2. Introduce parameter dictionary persistence if the backlog requires governed backend truth.
3. Add onboarding persistence only if derived empty-state rendering is insufficient for the approved product contract.

## Expected code changes

### Controllers
- `LearnController` only if audit now proves a dedicated controller is the smallest coherent target
- `ParametersController` if parameter detail/governance split justifies it
- targeted extension of `AccountController` only where required

### Services
- learn overview/runtime-scope service
- parameter dictionary service
- parameter detail service
- account self-service read/update service
- onboarding state service only if durability is required

### View models
- `RuntimeScopeViewModel`
- `LearnOverviewViewModel`
- `ParameterDetailViewModel`
- account self-service view models only where current models are insufficient

### Entities and migrations
Likely candidates only if proven necessary:
- parameter dictionary entities
- onboarding state entity family

## Validation required
- learn/runtime scope is backend-driven
- parameter detail is not uncontrolled frontend-only text
- password/account flows remain secure and coherent

---

## 13. Milestone 5 — Sprint 5 admin governance core

## Objective
Deliver the admin governance backend with explicit authorization boundaries and governed truth.

## Mandatory deliverables
1. Create dedicated admin controllers only where the surface is truly distinct.
2. Prefer projection/query first for overview and audit screens.
3. Introduce governance persistence only when the backlog requires durable admin truth.
4. Ensure admin-only surface is fully protected by the Milestone 1A authz baseline.

## Expected code changes

### Controllers
- `AdminOverviewController`
- `AdminInstrumentRegistryController`
- `AdminPeaRegistryController`
- `AdminScoringPolicyController`
- `AdminParameterDictionaryController`
- `AdminSnapshotAuditController`
- `AdminDataQualityController`
- `AdminUsersController` only if extending `UserController` is not structurally sound

### Services
- admin overview service
- instrument registry service
- PEA registry service
- scoring policy service
- parameter dictionary admin service
- snapshot audit service
- data quality service
- admin user management service if required

### View models
- `AdminOverviewViewModel`
- `AdminInstrumentRegistryItemViewModel`
- `AdminInstrumentDetailViewModel`
- `SnapshotAuditItemViewModel`
- `SnapshotAuditDetailViewModel`
- `SnapshotAuditComparisonViewModel`
- other bounded admin read models only where real routes require them

### Entities and migrations
Likely candidates only if missing and required:
- normalized PEA registry truth
- scoring policy entities
- data-quality issue or audit entities only if existing persisted sources cannot express the contract

## Validation required
- admin pages are not backed by mock truth when presented as governed backend truth
- admin routes are authorization-protected and coherent
- PEA traceability is explicit where surfaced

---

## 14. Milestone 6 — Sprint 6 and Sprint 7 wording governance, notifications, hardening

## Objective
Deliver wording governance, notification center, and final hardening without violating the two-layer recommendation model.

## Mandatory deliverables
1. Preserve two-layer recommendation design: structured recommendation truth separated from publishable wording templates.
2. Add notification persistence and routes only where required by product contract.
3. complete final hardening and consistency pass.
4. complete route-by-route and service-by-service proof.

## Expected code changes

### Controllers
- `AdminWordingVersionsController`
- `NotificationsController`

### Services
- wording version governance service
- wording publication service
- notification center service
- hardening/test support changes

### View models
- wording version admin view models
- notification center read models
- publication state/read models where required

### Entities and migrations
Likely candidates only if missing and required:
- wording version entities
- wording publication entities
- notification entities

## Validation required
- same backend state maps to stable wording-family selection
- controllers return only view models or framework primitives
- no opaque mega-service remains in touched area
- tests prove all claimed behavior

---

## 15. Required proof matrix per milestone

Every milestone report from Codex must contain these sections:
- `Current State Audit`
- `Target Design`
- `Exact Files To Change`
- `Implementation`
- `Validation`
- `Residual Risks`
- `Deferred But Explicitly Not Done`

For every changed route, Codex must state whether it was:
- extended in place
- newly created because extension was incoherent
- deferred because route strategy remains open

For every new entity or migration, Codex must state:
- why current persistence was insufficient
- which backlog contract required durable truth
- why projection/query alone was not enough

---

## 16. Build and test expectations

At minimum, Codex must preserve green build and tests after each implementation milestone.
For security-sensitive milestones, it must also add the missing proof layer rather than relying only on pre-existing controller tests.

Expected validation baseline includes:
- solution build
- targeted test execution for touched area
- integration tests for authz/session behavior when security surface is touched

Codex must not claim completion without command-level proof.

---

## 17. What Codex must not do even if it seems convenient

- do not create all future controllers up front
- do not add empty persistence tables to “prepare later work”
- do not move business logic into controllers
- do not treat current frontend assumptions as backend truth
- do not silently widen permissions to make tests pass
- do not normalize existing derogations into new code as if they were approved architecture
- do not freeze still-open route strategy decisions by convenience
- do not repurpose retired `TradingController`

---

## 18. Final challenge of this plan

This v3 plan intentionally challenges both earlier versions.

### What was weak in v1
- it was too permissive on early milestone sequencing
- it treated `/api/Account/me` as optional when the backlog already requires it
- it did not elevate authz defects to blocker status
- it was not explicit enough on derogation handling

### What was weak in v2
- it was stricter, but less autonomous as a single execution document
- it condensed some operational detail that Codex needs to act safely milestone by milestone
- it did not surface Sprint 0 strongly enough as a separate pre-implementation audit contract

### Remaining risks even in v3
- some route-family choices remain legitimately open until Milestone 0 closes them from repository proof
- current persistence may still hide additional closed-state string debt beyond what is already known
- frontend/backend wording mismatches may still create pressure to overfit backend contracts unless Codex stays disciplined

These remaining risks are acceptable only because this plan keeps them explicit instead of hiding them.

---

## 19. Codex master prompt starter for execution

Use the following prompt in English when launching Codex for a milestone.

```text
Treat this prompt and the file `PredictFinance_Codex_Execution_Plan_v3.md` as the binding execution contract for this task.

You are working on the real PredictFinance repository.
Do not rely on prior chat memory.
Rebuild context from repository reality first.
If repository reality contradicts the plan, repository reality wins and you must report the mismatch explicitly.

You must never invent repository state.
You must never silently reconcile contradictions.
You must never widen scope by convenience.
You must never present a proposal as implemented.
You must never claim completion without proof.

Your task is to execute exactly one milestone from `PredictFinance_Codex_Execution_Plan_v3.md`.
Do not implement later milestones.
Do not front-load future controllers, entities, or migrations.

Mandatory workflow:
1. rebuild current repository truth in the touched area
2. classify findings using only: PROVEN_EXISTS, PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT, PROVEN_MISSING_ROUTE, PROVEN_AUTHZ_GAP, PROVEN_MISSING_PERSISTENCE, REMAINING_TO_ARBITRATE, DEROGATION
3. identify blockers first
4. decide extend-versus-create with explicit justification
5. list exact files to change before coding
6. implement only the scoped milestone
7. run build/tests
8. report residual risks and explicitly deferred items

Hard rules:
- no new project
- no speculative persistence
- no controller business logic
- no direct EF in controllers
- no reactivation of TradingController
- no invented claims or permissions beyond what repository truth can prove
- for security-sensitive work, integration-test proof through the ASP.NET pipeline is mandatory

Output format:
1. Current State Audit
2. Target Design
3. Exact Files To Change
4. Implementation
5. Validation
6. Residual Risks
7. Deferred But Explicitly Not Done
```

---

## 20. Recommended execution order

The safe execution order is:
1. Milestone 0 - done
2. Milestone 1A - done
3. Milestone 1B - done
4. Milestone 2 - done
5. Milestone 3 - done
6. Milestone 4 - done
7. Milestone 5 - done
8. Milestone 6

Codex must not skip ahead.

