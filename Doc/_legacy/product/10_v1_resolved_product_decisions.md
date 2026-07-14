# V1 resolved product decisions

## Purpose

This file records the product decisions that are now considered closed for the V1 documentation baseline.
It exists so coding agents do not reopen already-decided questions while still respecting repository-truth mismatches that remain in code.

## Closed decision 1 — active market scope
- V1 active supported market scope = French listed equities only
- daily analysis remains the active V1 time granularity
- ETF support is out of V1 scope

## Closed decision 2 — ETF roadmap posture
- ETF support is reserved for V2
- the architecture may remain extensible through instrument-aware policies
- no active V1 runtime path may expose ETF support as already implemented
- any future ETF support requires dedicated scoring, support-reading, and wording contracts

## Closed decision 3 — support reading role
- support reading is a separate capability from technical market reading
- it may inform prioritization, pedagogy, and recommendation nuance
- it must not replace market-reading truth or become a hidden timing engine

## Closed decision 3bis — canonical PatternStatus taxonomy
The only canonical V1 PatternStatus codes are:
- `FORMING`
- `MONITORING`
- `CONFIRMED`
- `INVALIDATED`
- `COMPLETED`

Product-facing French wording may vary slightly for pedagogy, but:
- no other status code is allowed in V1 snapshots, API contracts, or screen contracts;
- expressions such as “pattern envisagé” or “proche de validation” are explanatory wording only, not separate canonical states.

## Closed decision 3ter — AnalysisOutcome first-rank visibility
`AnalysisOutcome` is a first-rank business result in V1.
It must distinguish:
- normal executable analysis results,
- business-level non-executable outcomes,
- without confusing them with technical transport failures.

## Closed decision 4 — parameter explanation truth model
- parameter explanations are backend deterministic outputs
- their source of truth is a governed parameter dictionary
- the frontend consumes explanation payloads but does not create explanation truth

## Closed decision 5 — recommendation composition rule
- recommendation action verbs are selected from structured backend truth only
- technical market reading remains the primary source for timing-sensitive action selection
- support reading may qualify confidence, caution, or completeness, but must not silently replace technical timing truth
- parameter-level explanations may nuance wording, but not select action verbs on their own

## Closed decision 5bis — recommendation verb partition by holding context
- if `holdingContext = NOT_HELD`, the only allowed recommendation verbs are `MONITOR`, `WAIT`, `BUY`
- if `holdingContext = HELD`, the only allowed recommendation verbs are `HOLD`, `REINFORCE`, `LIGHTEN`, `SELL`, `WAIT`
- `MONITOR` is forbidden for held positions
- `BUY` is forbidden for held positions
- held-versus-not-held wording is a product contract, not a frontend preference

## Closed decision 5ter — AnalysisOutcome hierarchy
- `AnalysisOutcome` remains the only first-rank technical-analysis business taxonomy in V1
- labels such as `INSUFFICIENT_PRICE_HISTORY`, `UNSUPPORTED_PATTERN_REQUEST`, `INSTRUMENT_OUTSIDE_V1_SCOPE`, and `MARKET_DATA_UNAVAILABLE` may exist only as explanatory detail, diagnostics, or transport/provider failure indicators
- no screen, API contract, or persistence contract may promote those labels into a competing first-rank outcome taxonomy

## Closed decision 6 — snapshot support-reading depth
Minimum persisted support-reading truth for V1 snapshots:
- universe id
- scoring version
- PEA status
- category-score summary
- coverage summary
- composite-score presence/absence and reason
- support-reading outcome/status values needed to render later history and comparison views

## Closed decision 7 — minimal admin perimeter
The minimal shippable admin perimeter for V1 includes:
- instrument registry visibility
- PEA registry read/update surface
- scoring policy visibility
- parameter dictionary governance
- wording version visibility
- snapshot audit read surface
- data-quality visibility

## Closed decision 8 — documentation discipline
- repository-truth and target-state language must remain distinguishable
- unresolved code mismatches must not be documented as already fixed
- coding agents must not silently reopen the V1 market scope decision


## Closed decision 9 — deterministic scoring tie handling
- fundamental-scoring tie handling is closed for V1
- average-rank style handling is the preferred rule when directly supported by the implementation
- otherwise the fallback must remain stable and deterministic, based on declared metric ordering and stable instrument identifier ordering
- repeated executions on the same scoring snapshot must produce the same ordering and same scores
