# V1 gaps and required arbitrations

## Purpose

This file now distinguishes between:
- product decisions that are closed for V1
- repository-truth mismatches that still exist in code
- remaining business explicitness points that still need deeper implementation-grade detail

## Product decisions now closed

### 1. V1 user promise
Decision:
- V1 active supported scope = French equities only
- ETF support = explicit V2 extension only

Consequence:
- no active V1 screen, wording, scoring rule, or recommendation rule may imply ETF runtime support
- architecture may remain extensible through instrument-aware policies, but runtime behavior stays French-equity only

### 2. Parameter explanation ownership model
Decision:
- parameter explanations are backend deterministic outputs generated from a governed parameter dictionary

Consequence:
- frontend wording fragments are not product truth
- admin governs dictionary content and versioning
- snapshots must preserve enough support-reading truth for later audit

### 3. Recommendation composition boundary
Decision:
- action-verb selection must be deterministic and based on structured inputs only
- allowed primary inputs are market-reading status, support-reading availability/status, portfolio-context status, and explicit risk-related outputs already owned by backend truth
- individual parameters and free-form text may nuance explanation wording, but must not override action-verb selection on their own

### 4. Snapshot depth for support reading
Decision:
- V1 snapshots must persist enough support-reading truth to remain auditable later without reconstructing the state from provider calls
- minimum persisted truth includes scoring version, universe id, PEA status, category-score summary, coverage summary, composite-score state, and support-reading outcome/status fields used by the UI

### 5. Minimal shippable V1 admin perimeter
Decision:
- instrument registry visibility
- PEA registry view/update path
- scoring policy visibility and version visibility
- parameter dictionary governance
- wording version visibility
- snapshot audit read surface
- data-quality visibility

## Proven architectural inconsistencies with the current repository

### 6. Contracts project mismatch
Repository reality:
- the solution contains `BackPredictFinance.Patterns`
- the solution does not currently contain `BackPredictFinance.Contracts`
- business-core V1 analysis contracts still live mostly under `BackPredictFinance.Common/AnalysisV1`

Required documentation stance:
- any document that treats `BackPredictFinance.Contracts` as already implemented must be read as target-state language, not repository truth
- transitional repository-truth wording must remain explicit until the refactor exists

### 7. Services to ViewModels dependency remains real
Repository reality:
- `BackPredictFinance.Services` currently references `BackPredictFinance.ViewModels`
- several service files use ViewModel types directly

Required documentation stance:
- architecture docs must keep distinguishing repository truth from target-state architecture
- agents must not claim this boundary is already fixed

### 8. Legacy DoubleTop compatibility still exists
Repository reality:
- `BackPredictFinance.Patterns` still contains `DoubleTopAnalysisPatternDefinition`
- the registry still exposes compatibility behavior around `DOUBLE_TOP`

Required documentation stance:
- documentation must distinguish active V1 continuation-pattern target from compatibility residue still present in the codebase
- no agent may document the compatibility residue as the intended V1 target

## Remaining business explicitness to deepen during implementation

These are no longer product arbitrations, but they still require implementation-grade detail in touched paths.

### 9. Parameter reading thresholds and semantics
Still needed:
- precise interpretation rules for each visible metric when the product wants more than raw percentile display
- explicit wording guardrails per metric family

### 10. Divergence handling between technical and support readings
Still needed:
- explicit user-visible states when technical reading is attractive but support reading is weak or incomplete
- explicit user-visible states when support reading is attractive but technical timing is absent

### 11. Actions when data is incomplete
Still needed:
- explicit deterministic wording matrix for missing support-reading data
- explicit deterministic wording matrix for unavailable PEA truth
- explicit deterministic wording matrix for non-comparable historical support readings

## Operating rule for coding agents

If any item in sections 6 to 11 is active in the touched implementation path:
- stop
- classify the point explicitly
- distinguish product-decision closure from repository-truth mismatch
- do not invent a silent resolution


## Explicit non-closed notion removed from active V1 screens

The earlier draft introduced a watchlist "convergence flag" without a canonical business contract.
That concept is not treated as a closed V1 truth in this documentation baseline.
Active V1 screens may show only:
- technical market-reading outputs,
- support-reading outputs,
- portfolio-context-aware recommendation outputs,
- explicit completeness or non-evaluable states already backed by canonical contracts.

Any later reintroduction of a convergence summary requires:
- a canonical input contract,
- deterministic computation rules,
- explicit ownership,
- an explicit decision on whether it is persisted or presentation-only.


### 12. API surface gaps versus the normative UI/UX specification

Repository reality checked against:
- `BackPredictFinance.API/Controllers/*`
- `Doc/product/07_v1_functional_architecture_screen_by_screen.md`
- `Doc/product/08_v1_admin_and_parameter_explanation_contract.md`
- `PredictFinance_V1_UI_UX_Screen_Spec_v2.md`

#### 12.1 Proven existing user API surface
The current API already exposes:
- `GET /api/ClientFinance/dashboard`
- `GET /api/ClientFinance/assets/search`
- `GET /api/ClientFinance/watchlist`
- `POST /api/ClientFinance/watchlist`
- `DELETE /api/ClientFinance/watchlist/{symbol}`
- `GET /api/ClientFinance/quote/{symbol}`
- `GET /api/ClientFinance/transactions`
- `POST /api/ClientFinance/transactions`
- `DELETE /api/ClientFinance/transactions/{id}`
- `POST /api/ClientFinance/analysis/run`
- `GET /api/ClientFinance/analysis/recent`
- `POST /api/ClientFinance/simulation/run`
- `POST /api/Fundamentals/score`
- ticker lookup endpoints under `/api/Tickers/*`
- account and user-management endpoints

Required stance:
- documentation must not describe these as absent
- any new endpoint proposal must distinguish true absence from response-shape insufficiency

#### 12.2 Proven payload insufficiencies on existing endpoints
The following gaps are real, but they do not automatically require a new route.

`GET /api/ClientFinance/dashboard`
- current payload is KPI-only
- it does not expose the mandatory home-card content required by the UI/UX spec:
  - latest market-reading summary
  - recommendation summary
  - support-reading summary
  - PEA status
  - incomplete / non-evaluable item feed

`GET /api/ClientFinance/watchlist`
- current payload exposes instrument identity, price, variation, and held-position aggregates
- it does not expose the mandatory watchlist fields required by the UI/UX spec:
  - latest market-reading summary
  - latest support-reading summary
  - PEA status
  - data completeness or non-evaluable status
  - last recommendation summary
  - last analysis timestamp

`GET /api/ClientFinance/transactions`
- this is a transaction feed, not a portfolio reading endpoint
- it does not expose the portfolio reading fields required by the UI/UX spec:
  - latest market-reading summary
  - latest support-reading summary
  - latest recommendation summary
  - risk hint

`GET /api/ClientFinance/analysis/recent`
- this helps populate recent analyses and history summaries
- it is not sufficient on its own for persisted snapshot detail, instrument timeline reading, or snapshot comparison

#### 12.3 Proven missing user endpoints
The following user-facing routes are absent from the current API surface and correspond to first-rank UI/UX screens or screen capabilities:

- no instrument-detail read endpoint
- no persisted analysis-detail read endpoint by snapshot or analysis id
- no per-instrument analysis-history endpoint
- no snapshot-comparison endpoint
- no parameter-detail endpoint
- no runtime-scope / learn-metadata endpoint

Minimum target route families still missing:
- `GET /api/ClientFinance/instruments/{symbol}`
- `GET /api/ClientFinance/analysis/{analysisId}`
- `GET /api/ClientFinance/instruments/{symbol}/analysis-history`
- `POST /api/ClientFinance/snapshots/compare`
- `GET /api/ClientFinance/parameters/{parameterId}`
- `GET /api/ClientFinance/runtime-scope`

#### 12.4 Proven missing admin endpoints
The normative admin perimeter requires explicit API support for:
- operations dashboard
- instrument registry
- PEA registry
- scoring policy
- parameter dictionary
- wording versions
- snapshot audit
- data quality

Repository reality:
- the current exposed admin surface is limited to account/user administration
- none of the required V1 admin domain route families above is currently exposed

#### 12.5 Required documentation discipline
When discussing endpoint gaps:
- do not classify an existing endpoint with an insufficient payload as a missing endpoint without saying so explicitly
- do not assume that every UI gap requires a brand new route
- do not claim admin V1 is API-ready while the domain routes above remain absent

