# V1 API surface gap audit versus the normative UI / UX specification

## Purpose

This file closes one specific documentation need:
- determine which endpoints are truly missing,
- distinguish them from existing endpoints with insufficient payloads,
- prevent over-declaring new routes when enrichment of an existing route is the smaller safe target.

This file is normative for API-gap review.
It does not claim implementation.
It classifies repository reality only.

## Inputs audited

Repository-truth inputs:
- `BackPredictFinance.API/Controllers/ClientFinanceController.cs`
- `BackPredictFinance.API/Controllers/FundamentalsController.cs`
- `BackPredictFinance.API/Controllers/TickersController.cs`
- `BackPredictFinance.API/Controllers/AccountController.cs`
- `BackPredictFinance.API/Controllers/UserController.cs`
- `BackPredictFinance.API/Controllers/TradingController.cs`

Normative product/UI inputs:
- `Doc/product/07_v1_functional_architecture_screen_by_screen.md`
- `Doc/product/08_v1_admin_and_parameter_explanation_contract.md`
- `Doc/product/11_v1_web_architecture_and_ui_wording_lock.md`
- `PredictFinance_V1_UI_UX_Screen_Spec_v2.md`

## Method

Each requirement is classified using only these route-gap buckets:
- `PROVEN_EXISTS`
- `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`
- `PROVEN_MISSING_ROUTE`
- `REMAINING_TO_ARBITRATE`

The classification is about exposed API surface only.
It is not a claim that the backend business logic is fully correct behind the route.

## Current exposed API surface

### User/client API
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

### Fundamentals API
- `POST /api/Fundamentals/score`

### Market/ticker API
- `GET /api/Tickers/exchanges`
- `GET /api/Tickers/symbols`
- `GET /api/Tickers/symbols/all`
- `GET /api/Tickers/timeseries/{symbol}`

### Account and user management API
- account/auth endpoints under `/api/Account/*`
- user/role administration endpoints under `/api/User/*`

### Explicitly retired surface
- retired prediction endpoints under `/api/Trading/*`

## Route-gap audit by user screen

## 1. Home dashboard

Normative mandatory needs:
- watchlist to review
- portfolio positions to review
- recent analyses
- non-evaluable or incomplete items
- quick search / analyze entry point

Classification:
- `GET /api/ClientFinance/dashboard` -> `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`
- `GET /api/ClientFinance/watchlist` -> `PROVEN_EXISTS`
- `GET /api/ClientFinance/analysis/recent` -> `PROVEN_EXISTS`
- `GET /api/ClientFinance/assets/search` -> `PROVEN_EXISTS`

Reason:
- the current dashboard route returns KPI totals only
- it does not provide the card-level semantic summaries required by the UI/UX baseline

Smallest safe target:
- enrich `GET /api/ClientFinance/dashboard` with dashboard sections,
- or introduce a dedicated summary-feed endpoint only if the current route would become structurally incoherent.

## 2. Watchlist

Normative mandatory fields:
- instrument identity
- latest market-reading summary
- latest support-reading summary
- PEA status
- data completeness or non-evaluable status
- last recommendation summary
- last analysis timestamp

Classification:
- `GET /api/ClientFinance/watchlist` -> `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`

Reason:
- the current route returns identity, live quote data, and held-position aggregates
- it does not return the required market/support/recommendation/PEA/completeness fields

Smallest safe target:
- enrich `GET /api/ClientFinance/watchlist`

## 3. Portfolio

Normative mandatory fields:
- instrument
- quantity held
- average cost / PRU
- latest market-reading summary
- latest support-reading summary
- latest recommendation summary
- risk hint

Classification:
- portfolio read route -> `REMAINING_TO_ARBITRATE`

Reason:
- the repository currently exposes:
  - `GET /api/ClientFinance/watchlist`
  - `GET /api/ClientFinance/transactions`
- neither route is a clean portfolio-reading contract
- the gap is real, but route strategy still needs an explicit decision:
  - enrich watchlist into a dual-purpose user-assets feed,
  - or create `GET /api/ClientFinance/portfolio`

Minimum decision needed:
- choose whether portfolio is a distinct read model or a projection of user assets already exposed elsewhere

## 4. Analysis entry

Normative mandatory needs:
- instrument search
- V1 perimeter reminder
- submit action

Classification:
- `GET /api/ClientFinance/assets/search` -> `PROVEN_EXISTS`
- `POST /api/ClientFinance/analysis/run` -> `PROVEN_EXISTS`

Reason:
- the route surface exists for the core action
- perimeter reminder is UI content, not a missing API route

## 5. Analysis result

Normative mandatory needs:
- analysis outcome
- market reading
- support reading
- personal-situation reading
- parameter-reading entry point
- snapshot / history entry point
- simulation entry point when relevant

Classification:
- `POST /api/ClientFinance/analysis/run` -> `PROVEN_EXISTS`
- persisted analysis detail route -> `PROVEN_MISSING_ROUTE`

Reason:
- fresh execution exists
- persisted detail retrieval for later re-open, deep read, or shareable route state does not

Minimum missing route:
- `GET /api/ClientFinance/analysis/{analysisId}`

## 6. Instrument detail

Normative mandatory needs:
- instrument summary
- market reading
- support reading
- parameter reading
- what this means for me
- links to simulation and history

Classification:
- instrument-detail route -> `PROVEN_MISSING_ROUTE`

Minimum missing route:
- `GET /api/ClientFinance/instruments/{symbol}`

Reason:
- current API exposes fragments across quote, fundamentals scoring, recent analyses, and fresh analysis execution
- there is no single read endpoint aligned with the central V1 screen

## 7. Parameter detail

Normative mandatory needs:
- parameter name
- definition
- current-value reading guidance
- interpretation limits
- what it supports
- what it does not prove
- implication with and without a position

Classification:
- parameter-detail route -> `PROVEN_MISSING_ROUTE`

Minimum missing route:
- `GET /api/ClientFinance/parameters/{parameterId}`

Reason:
- no current route exposes governed parameter pedagogy

## 8. Simulation

Normative minimal needs:
- hypothetical entry price
- position size
- invalidation level
- objective level
- fees when available
- current position state

Classification:
- `POST /api/ClientFinance/simulation/run` -> `PROVEN_EXISTS`

Residual note:
- route presence is proven
- parameter-level completeness of the current request/response contract still needs touched-path audit during implementation

## 9. History

Normative needs:
- instrument
- date
- business outcome
- visible primary pattern
- recommendation summary
- support-reading availability
- PEA availability

Classification:
- `GET /api/ClientFinance/analysis/recent` -> `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT`
- generic history feed route -> `REMAINING_TO_ARBITRATE`

Reason:
- a recent feed exists
- whether it should become the canonical history feed or remain a dashboard-oriented slice still needs explicit API design choice

## 10. Analysis history for one instrument

Normative needs:
- snapshot timeline for one symbol/instrument
- versioned persisted truth over time

Classification:
- per-instrument history route -> `PROVEN_MISSING_ROUTE`

Minimum missing route:
- `GET /api/ClientFinance/instruments/{symbol}/analysis-history`

## 11. Snapshot comparison

Normative needs:
- market reading changes
- support reading changes
- recommendation changes
- explicit non-comparability states

Classification:
- snapshot comparison route -> `PROVEN_MISSING_ROUTE`

Minimum missing route:
- `POST /api/ClientFinance/snapshots/compare`

## 12. Learn

Normative needs:
- current V1 market perimeter
- active patterns
- daily granularity scope
- support-reading perimeter
- current limits and deferred areas

Classification:
- runtime-scope / learn metadata route -> `PROVEN_MISSING_ROUTE`

Minimum missing route:
- `GET /api/ClientFinance/runtime-scope`

Reason:
- this information is normative product truth and should not be duplicated ad hoc in the frontend

## Route-gap audit by admin screen

## 13. Admin overview

Classification:
- operations dashboard route -> `PROVEN_MISSING_ROUTE`

Minimum missing route:
- `GET /api/Admin/operations/dashboard`

## 14. Instrument registry

Classification:
- instrument registry route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/instruments`
- `GET /api/Admin/instruments/{instrumentId}`

## 15. PEA registry

Classification:
- PEA registry route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/pea-registry`
- `GET /api/Admin/pea-registry/{instrumentId}`
- `PUT /api/Admin/pea-registry/{instrumentId}`

## 16. Scoring policy

Classification:
- scoring policy route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/scoring-policies`
- `GET /api/Admin/scoring-policies/active`

## 17. Parameter dictionary

Classification:
- parameter dictionary route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/parameter-dictionary`
- `GET /api/Admin/parameter-dictionary/{parameterId}`

## 18. Wording versions

Classification:
- wording versions route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/recommendation-wordings`
- `GET /api/Admin/recommendation-wordings/{scenarioCode}`

## 19. Snapshot audit

Classification:
- snapshot audit route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/snapshots`
- `GET /api/Admin/snapshots/{snapshotId}`

## 20. Data quality

Classification:
- data-quality route family -> `PROVEN_MISSING_ROUTE`

Minimum route family:
- `GET /api/Admin/data-quality/overview`

## Corrected conclusions versus earlier endpoint listing

The stricter reading against the normative UI/UX spec leads to these corrections:

1. Do not call Home, Watchlist, or Simulation route families missing when a real route already exists.
2. Do distinguish:
   - missing route
   - existing route with insufficient payload
   - route-strategy still needing arbitration
3. Do treat the absence of admin domain routes as a first-rank proven gap.
4. Do treat Instrument detail, Parameter detail, Snapshot comparison, Learn metadata, and per-instrument history as true missing routes.

## Minimum endpoint additions still required

The smallest defensible missing-route set is:

### User
- `GET /api/ClientFinance/instruments/{symbol}`
- `GET /api/ClientFinance/analysis/{analysisId}`
- `GET /api/ClientFinance/instruments/{symbol}/analysis-history`
- `POST /api/ClientFinance/snapshots/compare`
- `GET /api/ClientFinance/parameters/{parameterId}`
- `GET /api/ClientFinance/runtime-scope`

### Admin
- `GET /api/Admin/operations/dashboard`
- `GET /api/Admin/instruments`
- `GET /api/Admin/pea-registry`
- `GET /api/Admin/scoring-policies`
- `GET /api/Admin/parameter-dictionary`
- `GET /api/Admin/recommendation-wordings`
- `GET /api/Admin/snapshots`
- `GET /api/Admin/data-quality/overview`

## Non-negotiable documentation rule

From this point onward, any endpoint-gap discussion must explicitly separate:
- route absence,
- payload insufficiency on an existing route,
- unresolved route-design choice.

No future answer may collapse these three cases into one generic "missing endpoint" label.
