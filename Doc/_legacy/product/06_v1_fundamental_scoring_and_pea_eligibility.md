# V1 fundamental scoring and PEA eligibility integration contract

## Purpose

This file defines the V1 contract for adding a deterministic fundamental scoring capability to the existing product without breaking the current architecture or the existing technical-pattern scope.

This capability is additive.
It does not replace the current pattern-analysis engine.
It must remain separated from:
- pattern detection
- validation / invalidation
- risk computation tied to chart patterns
- recommendation wording driven by pattern analysis

The goal is to allow the product to:
- rank eligible instruments in a deterministic way
- enrich the watchlist with explainable fundamental scores
- support a future rebalancing or portfolio-review workflow
- preserve explicit traceability for every score

## Scope

V1 scope for this capability is restricted to:
- French equities already inside the product market perimeter
- deterministic backend-owned scoring
- explainable category scores
- explicit data coverage reporting
- explicit PEA eligibility status in the product
- no AI dependency
- no silent heuristic eligibility inference

Out of scope for this file:
- tax advice
- legal certification of PEA status
- automatic legal interpretation from incomplete market metadata
- replacement of the chart-pattern engine
- probabilistic Gaussian scoring based on mean/std assumptions
- broker execution
- automatic order generation

## Business intent

This capability exists to answer a different product need than chart-pattern detection.

Pattern analysis answers questions such as:
- is a technical continuation pattern forming or confirmed
- what is the invalidation level
- what is the current technical recommendation

Fundamental scoring answers questions such as:
- among the instruments I follow, which ones look stronger on selected business ratios
- which instruments have weak data coverage and should not be over-trusted
- which instruments are product-confirmed PEA-eligible, explicitly ineligible, or still unknown

The two capabilities may coexist in the same user journey, but they must not be merged into one opaque score.

## Non-negotiable V1 rules

- The API is the source of truth for fundamental scoring.
- The frontend must not recompute the scoring logic.
- PEA eligibility status must be explicit.
- `UNKNOWN` PEA status must never be silently treated as eligible.
- Scores must remain explainable category by category.
- Missing data must be surfaced explicitly.
- The final score must be penalized by data coverage when coverage penalty is enabled.
- The scoring method must not depend on Gaussian assumptions.
- The scoring method must be robust to outliers.
- Every score response must include enough metadata to be auditable later.

## Scoring method

### General principle

V1 scoring uses a non-parametric cross-sectional percentile ranking over the active universe.

This replaces the previous legacy-style mean/std plus normal-distribution transformation.

The V1 method must:
- avoid dependence on normality assumptions
- avoid outlier domination
- remain deterministic
- remain explainable
- remain implementable with backend-owned rules

### Universe

Every score is computed relative to an explicit universe id.

V1 default universe:
- `PEA_FR_EQUITIES`

V2 extensibility rule:
- the scoring architecture must remain capable of adding ETF-specific universes and policies later
- no V1 scoring rule may silently reuse equity semantics for ETFs
- future ETF support must introduce explicit ETF-specific scoring and support-reading contracts

If the backend cannot prove that a requested universe exists and is supported, the request must fail explicitly.

### Metric direction

Each metric must declare whether:
- higher is better
- lower is better
- the metric is informational only and excluded from the V1 composite score

Examples:
- `roe` -> higher is better
- `operatingMargin` -> higher is better
- `netMargin` -> higher is better
- `debtToEquity` -> lower is better
- `trailingPe` -> lower is better
- `priceToBook` -> lower is better
- `payoutRatio` -> informational only in V1 unless explicitly reintroduced by a later contract

### Percentile transformation

For a metric included in scoring:
- gather all valid values in the active universe snapshot
- compute the instrument rank inside that valid set
- convert the rank to a percentile between `0` and `1`
- invert the percentile when the metric direction is `lower is better`

The exact tie-handling rule is closed for V1.
V1 rule:
- use average-rank style handling for ties when the chosen ranking implementation supports it directly and traceably
- otherwise use a stable deterministic fallback based on the declared metric order and a stable instrument identifier ordering
- equal inputs must always produce equal outputs across repeated executions on the same snapshot

### Category scores

Metrics are grouped into explicit business categories.

V1 categories:
- `Profitability`
- `Valuation`
- `FinancialStrength`
- `Growth`
- `Income`

Each category score is the average of the valid metric percentiles inside that category.

If no valid metric exists in a category:
- the category score must be absent
- the category must count as missing for coverage purposes

### Coverage

The response must expose:
- `categoriesPresent`
- `categoriesMissing`
- `coverageRatio`

Where:
- `coverageRatio = categoriesPresent / totalConfiguredCategories`

### Minimum coverage

V1 minimum required categories:
- `3`

If an instrument has fewer than the minimum required categories:
- the composite score must be absent
- the response must explicitly state that the score is not usable due to insufficient coverage

### Composite score

If the instrument passes minimum coverage and PEA eligibility is confirmed:
- compute the simple average of present category scores
- if coverage penalty is enabled, multiply that average by `coverageRatio`

If PEA eligibility is not confirmed:
- see the PEA rules below

### Missing metrics

The response must include:
- missing categories
- missing metrics
- enough detail for the user and later audits to understand why a score is partial or unavailable

## PEA eligibility contract

### Why Yahoo Finance is not the source of truth

Yahoo Finance may remain the V1 source for raw fundamental ratios, but it must not be treated as the legal or product source of truth for PEA eligibility.

Reason:
- the product needs an explicit, traceable eligibility status
- PEA eligibility is not safely inferable from a single generic market-data field
- a silent heuristic would create unreliable product behavior

### V1 product truth for PEA

The product must store and expose one of these statuses:
- `CONFIRMED_ELIGIBLE`
- `CONFIRMED_INELIGIBLE`
- `UNKNOWN`

These statuses are product states, not legal certifications.

### V1 operational rule

- `CONFIRMED_ELIGIBLE` -> instrument may receive a composite score
- `CONFIRMED_INELIGIBLE` -> instrument must not receive a composite score in the PEA-scoped universe
- `UNKNOWN` -> instrument must not receive a composite score in the PEA-scoped universe

`UNKNOWN` may still expose raw metrics and category details if the product chooses to display them, but it must not be ranked as eligible.

### Recommended source model

V1 recommended source split:
- Yahoo Finance -> raw ratios and general market metadata
- internal product registry -> PEA eligibility status and traceability

The internal registry must carry at least:
- `symbol`
- `isin` when available
- `peaEligibilityStatus`
- `sourceType`
- `sourceReference`
- `checkedUtc`
- `policyVersion`
- optional `notes`

### Source quality rule

The product registry may use:
- broker confirmation
- issuer or market reference
- documented internal review
- another stronger future provider

If no sufficiently strong proof exists:
- keep `UNKNOWN`

The product must never silently upgrade `UNKNOWN` to `CONFIRMED_ELIGIBLE`.

## Required traceability

Every score response must include:
- `scoringVersion`
- `universeId`
- `asOfUtc`
- `providerId`
- `snapshotId` when a persisted snapshot exists
- `peaEligibilityStatus`
- `peaPolicyVersion` when available

If the backend cannot provide the required scoring metadata, the score is incomplete and must not be presented as fully auditable.

## User-facing uses in V1

This capability may add the following user actions.

### Watchlist enrichment

The user can:
- view the fundamental score of each eligible instrument in the watchlist
- inspect category scores
- understand why a score is missing or partial
- see explicit PEA eligibility status

### Ranking and prioritization

The user can:
- sort the watchlist by fundamental score
- filter by `CONFIRMED_ELIGIBLE`
- exclude `UNKNOWN` or `CONFIRMED_INELIGIBLE` instruments from ranked views

### Portfolio review support

The user can:
- compare held instruments on the same scoring basis
- identify weak coverage
- prepare a later rebalancing review

V1 must not claim:
- that the score alone is a buy recommendation
- that the score replaces technical analysis
- that PEA status is a legal guarantee

## API contract

### Placement rules

The implementation must respect AGENTS.md while distinguishing repository truth from target state:
- transport request/response models -> `BackPredictFinance.ViewModels`
- backend business contracts -> target-state `BackPredictFinance.Contracts`; current repository-truth transitional location remains largely `BackPredictFinance.Common/AnalysisV1` until refactored
- services and orchestration -> `BackPredictFinance.Services`
- persistence entities and registry tables -> `BackPredictFinance.Datas`

The scoring capability must not be embedded as an ad hoc extension of an unrelated service if that would create a mixed-responsibility default service.

### Suggested endpoints

#### `POST /api/Fundamentals/score`

Purpose:
- score a requested set of symbols inside an explicit universe

Request transport model:
- `universeId`
- `symbols`
- `asOfUtc` optional
- `options`
  - `minCategoriesRequired`
  - `coveragePenaltyEnabled`
  - `includeInformationalMetrics`

Rules:
- reject empty symbol lists
- cap the symbol count explicitly in V1
- reject unknown universes
- require authentication unless the repository explicitly decides otherwise
- return explicit validation errors

Response transport model:
- `scoringVersion`
- `universeId`
- `asOfUtc`
- `providerId`
- `snapshotId` optional
- `results`
  - `symbol`
  - `isin` optional
  - `peaEligibilityStatus`
  - `scoreAvailable`
  - `compositeScore` optional
  - `coverageRatio`
  - `categoriesPresent`
  - `categoriesMissing`
  - `categoryScores`
  - `missingMetrics`
  - `informationalMetrics`
  - `explanationFlags`

#### `GET /api/ClientFinance/watchlist/scored`

Purpose:
- return the existing watchlist enriched with fundamental scoring metadata

Rules:
- the watchlist endpoint remains the source of the user-owned instrument set
- the scoring enrichment is additive
- the backend must not require the frontend to recompute any score

Suggested response addition per watchlist row:
- `fundamentalScore`
- `scoreAvailable`
- `coverageRatio`
- `peaEligibilityStatus`
- `fundamentalScoringVersion`
- `fundamentalAsOfUtc`

## Internal backend contracts

The backend business contracts should include explicit scoring-domain types such as:
- `FundamentalUniverseId`
- `PeaEligibilityStatus`
- `FundamentalMetricDirection`
- `FundamentalCategoryScore`
- `FundamentalScoreResult`
- `FundamentalScoreSnapshotMetadata`

These are backend business-core concepts.
They should not be hidden inside transport DTOs.

## Persistence guidance

V1 persistence should support at least:
- an instrument-level PEA registry
- optional persisted scoring snapshot metadata if history is needed
- explicit policy/version fields

The persistence layer must not recompute business truth.
It stores registry facts and optional score snapshots.
Scoring logic remains in services.

## Provider guidance

V1 provider split must be explicit.

Yahoo Finance is acceptable for:
- raw ratio acquisition
- general market metadata acquisition

Yahoo Finance is not acceptable as the product truth for:
- PEA eligibility status

If a stronger dedicated provider is introduced later, it must replace or complement the registry input path explicitly.
No silent provider switching.

## AI agent integration rules

Any agent implementing this capability must do all of the following.

### Before coding

- inspect the real current watchlist runtime path
- inspect the current market-data provider path
- inspect whether a fundamentals capability already exists
- classify each statement as `PROVEN`, `DECIDED`, `PROPOSED`, `DEROGATION`, or `REMAINING TO ARBITRATE`
- stop if a required classification is ambiguous

### Architectural constraints

- do not put scoring business contracts into `ViewModels`
- do not put scoring entities outside `BackPredictFinance.Datas`
- do not duplicate metric-direction truth across frontend and backend
- do not add broad generic abstractions without repository proof
- do not modify pattern-analysis contracts unless a real touched path requires it
- keep the scoring capability separated from chart-pattern logic

### Implementation sequence

1. introduce the backend business contracts
2. introduce the transport models
3. introduce or extend the persistence model for the PEA registry
4. implement the scoring service
5. expose the scoring endpoint
6. optionally enrich the watchlist endpoint or add a scored-watchlist endpoint
7. validate backend output before touching the frontend

### Validation expectations

At minimum, prove:
- a confirmed eligible instrument with enough coverage returns a composite score
- an instrument with insufficient coverage returns no composite score
- an `UNKNOWN` PEA instrument returns no composite score in the PEA universe
- lower-is-better metrics are inverted correctly
- tied values are handled deterministically
- watchlist enrichment does not recompute in the frontend
- scoring metadata is returned

## Deferred items

The following are explicitly deferred from V1 unless separately approved:
- automatic legal certification of PEA status
- broker execution workflows
- free-form AI explanations
- using payout ratio in the composite score
- cross-market expansion beyond the declared universe
- mixing chart-pattern score and fundamental score into one composite super-score

## Integration verdict

This capability is compatible with the existing project if and only if:
- it remains backend-owned
- it remains separated from the pattern engine
- PEA eligibility remains explicit and traceable
- Yahoo stays limited to ratio sourcing, not legal eligibility truth
- the frontend is only a consumer of backend projections


## Parameter explanation integration

Fundamental scoring is not sufficient by itself for beginner pedagogy.
If the product exposes the underlying parameters to the user, the product must also expose deterministic explanations for those parameters.

For each visible parameter, the product should be able to expose:
- parameter identity
- simple definition
- current-value interpretation
- why the parameter matters in the support reading
- what the parameter does not prove
- what the parameter implies for a user without a position
- what the parameter implies for a user with a position

These explanations must remain separate from:
- technical timing truth
- final recommendation truth
- legal claims about eligibility or quality

## Admin and governance requirements for this capability

This capability becomes operationally unsafe if all of the following are missing:
- governed PEA registry
- visible scoring policy versioning
- governed parameter dictionary
- governed wording versions for deterministic support explanations
- snapshot audit visibility for later review

A pure frontend-only implementation of these explanations is not acceptable for V1 product truth.
