# PredictFinance V1 — Codex Execution Plan for `PredictFinance_V1_Sprint_Backlog_Operational`

## Goal
Provide a Codex plan that is strict enough to implement the backend backlog in the real repository without drifting from:
- the actual solution structure under `FinanceBack/BackPredictFinance.sln`
- the repository contract in `AGENTS.md`
- the operational backlog in `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`

This plan is specifically framed so Codex can create the **controllers, services, view models, entities, DbContext registrations, migrations, and tests** required by the backlog, while preserving the current repository boundaries.

---

## 1. Repository truth Codex must start from

### PROVEN
The backend solution currently contains these projects:
- `BackPredictFinance.API`
- `BackPredictFinance.Services`
- `BackPredictFinance.Datas`
- `BackPredictFinance.ViewModels`
- `BackPredictFinance.Common`
- `BackPredictFinance.Patterns`
- `BackPredictFinance.Tests`

### PROVEN
The current API controller surface already includes:
- `AccountController`
- `ClientFinanceController`
- `FundamentalsController`
- `TickersController`
- `TradingController`
- `UserController`

### PROVEN
The current backend already contains analysis-history related entities:
- `AnalysisRun`
- `PatternAssessment`
- `DecisionSignal`
- `ModelSnapshot`
- `Recommendation`
- `AssetQuoteSnapshot`
- `AssetCandleSnapshot`
- `AssetPeaEligibility`

### PROVEN
The current backend does **not** currently prove the existence of dedicated persistence entities for at least these backlog capabilities:
- parameter dictionary governance
- wording versions governance
- notification center
- admin data quality persistence
- admin scoring policy persistence
- explicit onboarding state persistence

### PROVEN
The current `ClientFinanceController` already exposes:
- dashboard
- asset search
- watchlist
- quote
- transactions
- analysis run
- recent analyses
- simulation run

### PROVEN
The current architecture already violates the long-term target because `Services -> ViewModels` coupling exists today.
This is repository reality.
Codex must not make it worse.

### DECIDED
Codex may extend the current solution, but must **not** add a new project.

### DECIDED
Controllers remain HTTP-only.
Services contain orchestration.
Entities live only in `BackPredictFinance.Datas`.
View models live only in `BackPredictFinance.ViewModels`.
Tests live only in `BackPredictFinance.Tests`.

---

## 2. What Codex is allowed to create

### Allowed new controllers when proved missing
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
- `AdminUsersController` **only if** the current `UserController` cannot safely represent the admin surface without becoming incoherent

### Allowed new service areas under `BackPredictFinance.Services`
- `Analysis/Application`
- `Analysis/History`
- `Learn`
- `Parameters`
- `Notifications`
- `Admin/Governance`
- `Admin/Registry`
- `Admin/Scoring`
- `Admin/Snapshots`
- `Admin/DataQuality`

### Allowed new entities under `BackPredictFinance.Datas`
Only when a backlog capability requires durable governed truth and existing entities are insufficient.
Candidate entity families that Codex may introduce **only after proof**:
- parameter dictionary entities
- wording publication/version entities
- notification entities
- scoring policy entities
- onboarding state entities
- data-quality issue entities
- snapshot audit helper entities only if current snapshot domain cannot support the backlog through query/projection alone

### Allowed new view model families
- dashboard/home read models
- portfolio read models
- analysis detail read models
- history feed read models
- instrument detail read models
- comparison read models
- learn/runtime scope read models
- parameter detail read models
- admin overview/read models
- admin registry/read models
- notification center read models

### Forbidden
- new project creation
- giant all-purpose controller
- giant all-purpose service
- direct EF logic inside controllers
- returning EF entities from controllers
- frontend wording truth moved into backend free-form strings without enum/value lock
- pretending a route is missing before auditing current controller coverage

---

## 3. Exact implementation strategy Codex must follow

## Milestone 1 — Sprint 0 + Sprint 1

### Objective
Stabilize the execution baseline before broader backlog implementation.

### Codex must do
1. Audit current route coverage and classify each target route as:
   - `PROVEN_EXISTS`
   - `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`
   - `PROVEN_MISSING_ROUTE`
   - `REMAINING_TO_ARBITRATE`
2. Audit current auth/session surface.
3. Add a current-user/session read model if missing.
4. Add or patch role-aware route support needed by frontend shells and guards.
5. Introduce missing enums for closed sets that are already required by Sprint 1 and Sprint 2.
6. Keep this milestone narrow: no admin governance persistence yet.

### Expected code creation in this milestone
- possibly a `GET /me`-style route if the current backend does not expose stable current-user identity and role data
- one or more small auth/session view models
- service methods for current-user/session read model
- authorization policy tightening if gaps are proven
- enum baseline for closed state families already needed downstream
- tests for auth/session/role payload behavior

### Validation required
- role data returned by the backend is sufficient for anonymous/user/admin shell split
- current user cannot access admin routes
- no new `Services -> ViewModels` coupling beyond current repository reality

---

## Milestone 2 — Sprint 2

### Objective
Deliver the user-core backend read models for Home, Watchlist, Portfolio, Analysis Entry support, and Analysis Result details.

### Codex must do
1. Re-audit `ClientFinanceController` and keep existing route family where possible.
2. Extend existing payloads before creating new routes when that is the smallest safe target.
3. Create missing portfolio route only if Sprint 0 proves the current route family cannot represent portfolio cleanly.
4. Build dedicated orchestration/services instead of making `ClientFinanceService` a monolith.

### Expected code creation in this milestone
#### Controllers
- extend `ClientFinanceController`
- optionally add a narrowly scoped new portfolio endpoint under `ClientFinanceController`

#### Services
- dashboard projection service if current dashboard logic becomes too large
- portfolio read service
- analysis detail projection service
- analysis entry helper service only if needed for governed lookup/runtime-scope support

#### View models
- `HomeDashboardViewModel`
- `DashboardAttentionItemViewModel`
- `DashboardRecentAnalysisItemViewModel`
- `DashboardIncompleteItemViewModel`
- `WatchlistRowViewModel`
- `WatchlistFilterViewModel`
- `PortfolioViewModel`
- `PortfolioPositionViewModel`
- `AnalysisDetailViewModel`

#### Entities
Create **none by default** here unless a read model cannot be supported from current persisted truth.
Sprint 2 should prefer projection over schema expansion.

### Validation required
- Home, Watchlist, Portfolio, and Analysis Result all read stable backend truth
- recommendation wording still depends on held vs not-held context
- alternative compatible patterns remain visible in result payloads

---

## Milestone 3 — Sprint 3

### Objective
Deliver Instrument Detail, global History, instrument history, and Snapshot Comparison using persisted analysis truth.

### Codex must do
1. Audit the current snapshot/history persistence before exposing public history routes.
2. Reuse current analysis entities if they already contain stable queryable truth.
3. Only add persistence fields or entities when the history/comparison contract cannot be proven from current schema.

### Expected code creation in this milestone
#### Controllers
- `HistoryController` if route separation is proved necessary
- or extension of existing route family if Sprint 0/3 audit proves that is cleaner

#### Services
- history feed service
- instrument detail projection service
- snapshot comparison service
- history persistence audit/patch service or migration support if needed

#### View models
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
- `SnapshotComparisonRequestViewModel`
- `SnapshotComparisonViewModel`
- `SnapshotDeltaItemViewModel`

#### Entities / migrations
Only if current persisted truth is insufficient:
- patch existing history entities or add stable fields for comparison/history queries
- add migration only after proving missing persisted fields

### Validation required
- history endpoints read persisted truth, not reconstructed current guesses
- snapshot comparison distinguishes cause from consequence
- no route is declared final if the backlog marked strategy as open

---

## Milestone 4 — Sprint 4

### Objective
Deliver Learn, Parameter Detail, Account self-service gaps, forgot/reset password corrections, and onboarding state.

### Codex must do
1. Keep account work minimal if current `AccountController` already covers most flows.
2. Introduce parameter dictionary persistence because Sprint 4 explicitly depends on it.
3. Add onboarding persistence only if the frontend contract truly requires durable state rather than derived empty-state rendering.

### Expected code creation in this milestone
#### Controllers
- `LearnController`
- `ParametersController`
- possible targeted extension of `AccountController`

#### Services
- learn overview/runtime scope service
- parameter dictionary service
- parameter detail service
- account self-service read/update service
- onboarding state service if durability is required

#### View models
- `RuntimeScopeViewModel`
- `LearnOverviewViewModel`
- `ParameterDetailViewModel`
- account self-service view models only where current ones are insufficient

#### Entities / migrations
Likely candidates:
- parameter dictionary entity family
- onboarding state entity if required

### Validation required
- learn/runtime scope is driven by backend truth
- parameter detail is not hardcoded in frontend text blobs
- password reset and account routes remain secure and coherent

---

## Milestone 5 — Sprint 5

### Objective
Deliver admin governance core.

### Codex must do
1. Create dedicated admin controllers where the surface is truly distinct.
2. Prefer query/projection first for overview/audit pages.
3. Introduce governance persistence only where the backlog requires explicit durable truth.

### Expected code creation in this milestone
#### Controllers
- `AdminOverviewController`
- `AdminInstrumentRegistryController`
- `AdminPeaRegistryController`
- `AdminScoringPolicyController`
- `AdminParameterDictionaryController`
- `AdminSnapshotAuditController`
- `AdminDataQualityController`
- `AdminUsersController` only if reuse of `UserController` is not structurally sound

#### Services
- admin overview service
- instrument registry service
- PEA registry service
- scoring policy service
- parameter dictionary admin service
- snapshot audit service
- data quality service
- admin user management service if needed

#### View models
- `AdminOverviewViewModel`
- `AdminInstrumentRegistryItemViewModel`
- `AdminInstrumentDetailViewModel`
- `SnapshotAuditItemViewModel`
- `SnapshotAuditDetailViewModel`
- `SnapshotAuditComparisonViewModel`
- other admin-specific read models strictly as required by each route

#### Entities / migrations
Likely candidates if missing:
- normalized PEA registry truth
- scoring policy entities
- data quality issue / audit entities only if current persisted sources cannot express the contract

### Validation required
- admin pages are not backed by mock data when presented as governed truth
- PEA traceability is explicit when surfaced
- admin users management surface is structurally coherent and authorization-protected

---

## Milestone 6 — Sprint 6 + Sprint 7

### Objective
Deliver wording governance, notification center, hardening, consistency, and full test coverage.

### Codex must do
1. Keep wording governance two-layered: structured recommendation truth separate from publishable text templates.
2. Add notification center persistence and routes.
3. Run final normalization and security hardening pass.
4. Complete tests route-by-route and service-by-service.

### Expected code creation in this milestone
#### Controllers
- `AdminWordingVersionsController`
- `NotificationsController`

#### Services
- wording version governance service
- wording publication service
- notification center service
- security hardening/test support updates

#### View models
- wording version admin view models
- notification center read models
- publication status/read model families as required

#### Entities / migrations
Likely candidates if missing:
- wording version entities
- wording publication entities
- notification entities

### Validation required
- same backend state always maps to the same French wording family
- controllers return only view models or framework primitives
- tests prove all claimed behavior
- no opaque mega-service remains

---

## 4. Rules Codex must apply in every milestone

### Mandatory implementation order inside a milestone
1. current-state audit in touched runtime path
2. classification of gaps with `PROVEN`, `DECIDED`, `PROPOSED`, `DEROGATION`, `REMAINING TO ARBITRATE`
3. smallest safe target
4. exact files to change
5. implementation
6. validation
7. residual risks and deferred items

### Mandatory architecture rules
- keep controllers thin
- keep EF access out of controllers
- keep entities out of public API responses
- use enums for closed state sets
- do not introduce new free-form strings for governed closed states
- do not widen a sprint because a nearby page looks convenient to include
- do not silently freeze an API route strategy marked open in the backlog

### Mandatory migration rules
- add a migration only when schema change is proven necessary
- migration names must reflect the actual business change
- no speculative persistence tables
- no persistence added "just in case"

### Mandatory testing rules
For every materially changed endpoint or service path, add or update tests proving:
- authorization behavior
- response shape
- core business decision invariants
- persistence behavior when schema changed
- no regression on held vs not-held recommendation wording
- preservation of alternative compatible patterns when relevant

---

## 5. Master Codex prompt to use

```text
Treat this prompt as the binding working contract for this task.

You are working on the real repository of `PredictFinance`.

Do not rely on prior chat memory.
Rebuild context from repository reality first.
If repository reality contradicts this prompt, repository reality wins and you must report the mismatch explicitly.

You must never invent repository state.
You must never silently reconcile contradictions.
You must never widen scope by convenience.
You must never present a proposal as implemented.
You must never claim completion without proof.

==================================================
1. TASK TYPE
==================================================

This is a bounded backend implementation task for `FinanceBack/BackPredictFinance.sln`.

Your mission is to implement the next approved milestone from `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`.

The implementation must create or update the backend pieces that are actually necessary for that milestone, including when proved necessary:
- controllers
- services
- view models
- entities
- DbContext registration
- EF Core configuration
- migrations
- tests

You must not create anything speculative.
You must not create persistence "for later".
You must not create a new project.

==================================================
2. REPOSITORY TRUTH TO START FROM
==================================================

The current solution contains:
- BackPredictFinance.API
- BackPredictFinance.Services
- BackPredictFinance.Datas
- BackPredictFinance.ViewModels
- BackPredictFinance.Common
- BackPredictFinance.Patterns
- BackPredictFinance.Tests

Current controllers already present:
- AccountController
- ClientFinanceController
- FundamentalsController
- TickersController
- TradingController
- UserController

Current analysis/history entities already present include at least:
- AnalysisRun
- PatternAssessment
- DecisionSignal
- ModelSnapshot
- Recommendation
- AssetQuoteSnapshot
- AssetCandleSnapshot
- AssetPeaEligibility

Current route family already present in ClientFinanceController includes at least:
- dashboard
- assets/search
- watchlist
- quote
- transactions
- analysis/run
- analysis/recent
- simulation/run

Current repository reality also proves that `Services -> ViewModels` coupling already exists.
This is not the target architecture.
Do not make it worse.
Do not introduce any new unnecessary service-to-ViewModel coupling.

==================================================
3. ARCHITECTURE RULES
==================================================

Respect `AGENTS.md`.

Mandatory rules:
- controllers are HTTP-only
- business logic stays in services
- entities stay in Datas only
- public payloads stay in ViewModels only
- tests stay in Tests only
- use enums for closed state sets
- do not return EF entities from controllers
- do not use DbContext directly in controllers
- do not add a new project
- do not collapse unrelated capabilities into one giant service

==================================================
4. TARGET MILESTONE
==================================================

Implement: <REPLACE_WITH_APPROVED_MILESTONE>

Examples:
- Milestone 1 = Sprint 0 + Sprint 1
- Milestone 2 = Sprint 2
- Milestone 3 = Sprint 3
- Milestone 4 = Sprint 4
- Milestone 5 = Sprint 5
- Milestone 6 = Sprint 6 + Sprint 7

Use `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md` as the sprint scope source of truth.

==================================================
5. EXECUTION METHOD
==================================================

You must work in this exact order:

1. Audit the touched runtime path in the real repository.
2. Classify findings using only:
   - PROVEN
   - DECIDED
   - PROPOSED
   - DEROGATION
   - REMAINING TO ARBITRATE
3. Identify the smallest safe target.
4. List the exact files you will change.
5. Implement the changes.
6. Run the narrowest meaningful validation.
7. Report residual risks and deferred items.

If a target route already exists, prefer extending it over creating a parallel route.
If the backlog marks a route strategy as open, do not freeze it without explicit proof.
If the existing schema is sufficient, do not add entities or migrations.
If the existing schema is insufficient for durable governed truth, add the smallest necessary entities and migration.

==================================================
6. REQUIRED OUTPUT FORMAT
==================================================

Return your answer in the following sections only:

1. Current state audit
2. Gap classification
3. Smallest safe target
4. Exact files to change
5. Implementation summary
6. Validation results
7. Residual risks / deferred items

==================================================
7. NON-NEGOTIABLE NO-DRIFT CHECKS
==================================================

Do not finish if any of the following is true:
- a controller returns persistence entities
- a controller owns business logic
- a closed business state stays a free-form string without reason
- recommendation wording loses held vs not-held distinction
- alternative compatible patterns disappear from result/history/detail when they should remain visible
- history/comparison is built from reconstructed guesses instead of persisted truth
- an admin capability is presented as governed truth while backed only by mock logic
- a new project was added
- tests do not prove the claimed change
```

---

## 6. Recommended usage

Use the master prompt milestone by milestone.
Do **not** send the whole backlog as one giant implementation order.

Recommended safe sequence:
1. Milestone 1
2. Milestone 2
3. Milestone 3
4. Milestone 4
5. Milestone 5
6. Milestone 6

That sequence matches the backlog dependency map and is the safest way to let Codex create the required controllers, services, view models, entities, and migrations without architectural drift.
