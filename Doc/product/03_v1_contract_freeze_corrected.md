# V1 contract freeze — corrected

## Instrument
Canonical and extensible domain contract.
V1 enabled scope is restricted to active French listed equities only.

## PortfolioLine
Open holding lot with multi-line support.
PRU is derived, not stored as authoritative V1 truth.
V1 open-line reconstruction uses strict FIFO for portfolio contextualization:
- each Buy creates one line
- each Sell consumes the oldest still-open buy lines first
- only remaining open quantities stay in `openLines`
- this is a V1 contextualization rule, not a tax/accounting policy

## PortfolioContext
Available to recommendation and explanation.
Forbidden as an input to pattern detection logic.
`openLines` must reflect the remaining open buy lines after strict FIFO sell consumption in V1.

## AnalysisRequest
Single instrument, daily candles, server-resolved history window.

## PatternAssessment
Separated into:
- detection
- validation
- invalidation
- scoring
- riskHints
- explanation
- trace

## Recommendation
Separate from detection and risk.
Portfolio-aware wording.
`reviewHorizonDays` is optional.

## AnalysisSnapshot
Versioned analysis history with root snapshot, per-pattern rows, and recommendation history.

## AnalysisResponse
`mainPattern` is only a display-primary selection from persisted compatible assessments.
It is not a separate business object.

## PatternStatus
UX-facing synthesized status only:
- FORMING
- MONITORING
- CONFIRMED
- INVALIDATED
- COMPLETED

Detailed truth remains in:
- `validation.state`
- `invalidation.state`

## AnalysisOutcome
Single business-level taxonomy covering:
- executable outcomes
- business-level non-executable outcomes
Technical failures remain HTTP/API errors.

## Deterministic text rule
All mandatory explanatory texts in V1 are deterministic, rule-generated, and versionable.
No free-form or AI-authored mandatory text in V1.
