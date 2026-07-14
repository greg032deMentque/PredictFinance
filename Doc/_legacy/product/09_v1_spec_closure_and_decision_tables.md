# V1 spec closure and decision tables

## Purpose

This file closes the remaining weak areas of the V1 specification so agents do not fill the gaps by interpretation.
It is a V1 lock file, not a brainstorming file.

## 1. Closed separation between technical analysis and fundamental scoring

### DECIDED
- Technical analysis and fundamental scoring are two distinct product capabilities.
- V1 may expose both in the same user journey.
- V1 must not combine both into one hidden final score or one hidden final recommendation formula.

### Consequence
- Technical analysis may produce a technical recommendation.
- Fundamental scoring may produce a relative ranking signal.
- A UI may show both, but each block must remain named and traceable.

## 2. Closed instrument-scope rule for V1

An instrument is analyzable in the V1 technical perimeter only if all of the following are true:
- canonical instrument exists
- `assetType = EQUITY`
- `countryCode = FR`
- `isActive = true`
- the backend can resolve the requested daily price history

If one of these conditions is false:
- technical analysis must not silently continue
- the response must explicitly return a closed V1 `AnalysisOutcome` value or an explicit transport / provider error depending on the failure class

## 3. Closed unavailable-state taxonomy

### Technical-analysis first-rank business outcomes
The only first-rank V1 business outcomes are:
- `CREDIBLE_PATTERN_FOUND`
- `MULTIPLE_COMPATIBLE_PATTERNS`
- `NO_CREDIBLE_PATTERN`
- `INSUFFICIENT_DATA`
- `UNSUPPORTED_INSTRUMENT`
- `UNSUPPORTED_CONTEXT`

### Technical-analysis second-rank explanatory causes
The following labels may exist only as explanatory causes, diagnostics, or detail fields. They are not a competing first-rank taxonomy:
- `INSUFFICIENT_PRICE_HISTORY` -> maps to first-rank `INSUFFICIENT_DATA`
- `UNSUPPORTED_PATTERN_REQUEST` -> maps to first-rank `UNSUPPORTED_CONTEXT`
- `INSTRUMENT_OUTSIDE_V1_SCOPE` -> maps to first-rank `UNSUPPORTED_INSTRUMENT`
- `MARKET_DATA_UNAVAILABLE` -> transport / provider failure, not a business `AnalysisOutcome`

### Fundamental-scoring explicit availability states
- `COMPOSITE_AVAILABLE`
- `INSUFFICIENT_COVERAGE`
- `PEA_STATUS_UNKNOWN`
- `PEA_CONFIRMED_INELIGIBLE`
- `UNSUPPORTED_UNIVERSE`
- `PROVIDER_DATA_UNAVAILABLE`

No implementation may invent a new user-facing first-rank business outcome in the touched scope without updating this file.

## 4. Closed recommendation decision matrix for V1

This matrix closes the product intent at V1 level.
It does not claim the current repository already implements all these rows.

### Inputs
- `holdsInstrument`
- primary compatible pattern validation state
- primary compatible pattern invalidation state
- recommendation action from technical policy

### Allowed output verbs by context

#### When `holdsInstrument = false`
- allowed outputs: `Surveiller`, `Attendre`, `Acheter`
- forbidden outputs: `Conserver`, `Renforcer`, `Alléger`, `Vendre`

#### When `holdsInstrument = true`
- allowed outputs: `Attendre`, `Conserver`, `Renforcer`, `Alléger`, `Vendre`
- forbidden outputs: `Surveiller`, `Acheter`

### Closed V1 interpretation rules
- bullish confirmed pattern + does not hold -> `Acheter`
- bullish confirmed pattern + holds -> `Conserver` or `Renforcer` depending on the structured technical policy
- neutral or early-forming compatible pattern + does not hold -> `Surveiller` or `Attendre`
- neutral or early-forming compatible pattern + holds -> `Conserver` or `Attendre`
- invalidated bearish reading while holding -> `Alléger` or `Vendre` depending on the structured technical policy
- invalidated bullish reading without position -> never output `Acheter`
- `Surveiller` is never allowed for an already-held instrument

### Fundamental scoring interaction rule
- a fundamental score may be shown alongside the technical recommendation
- the technical recommendation remains owned by the technical policy
- V1 does not define any automatic recommendation override from fundamental scoring

## 5. Closed history and snapshot reading rule

### Snapshot truth
A persisted technical snapshot must remain readable later without reconstructing hidden business logic from UI text alone.

A persisted fundamental scoring snapshot, when implemented, must preserve at least:
- universe id
- scoring version
- PEA status
- category scores
- coverage ratio
- composite score status
- composite score when available
- provider and timestamp trace

### Comparison rule
When comparing two snapshots in V1:
- compare only fields explicitly persisted in snapshot truth
- do not present a simple score delta as meaningful if the scoring version changed
- do not present a composite-score delta as directly comparable if coverage status changed from available to unavailable or the reverse

## 6. Closed definition of done per capability

### Technical multi-pattern runtime is done only if
- enabled continuation patterns can be requested or default-resolved in the active runtime path
- the active runtime path no longer rejects a supported pattern only because it is not `DOUBLE_TOP`
- response preserves displayed primary pattern and compatible alternatives distinctly
- persistence preserves the executed pattern set without lossy mono-pattern fallback
- tests prove at least one non-legacy path per enabled continuation pattern

### Fundamental scoring V1 is done only if
- the backend owns the scoring calculation
- the backend owns the PEA product truth source
- score transport exposes category scores, coverage ratio, PEA status, and composite-score status
- unavailable states are explicit and tested
- the frontend does not recompute scoring truth

### Wording lock is done only if
- the API or UI uses the stable wording families of `08_v1_wording_and_ui_semantics.md`
- no untracked synonyms are introduced in the touched scope for the same business state

## 7. Closed anti-guessing rule for agents

If a future implementation needs one of these decisions and cannot prove it from repository reality plus this document:
- stop
- classify the point as `REMAINING TO ARBITRATE`
- ask the minimum blocking question instead of completing the gap by interpretation
