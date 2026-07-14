# V1 contract freeze — corrected

## Instrument
Canonical and extensible domain contract.
V1 enabled scope is restricted to active French listed equities only.
V2 extension rule:
- ETF support is reserved for V2
- V1 screens, scoring policies, parameter semantics, and recommendation rules must not silently assume ETF support
- architecture may remain extensible through instrument-aware policies, but runtime V1 remains French-equity only


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
Allowed action verbs are constrained by holding context:
- `NOT_HELD` -> `MONITOR`, `WAIT`, `BUY`
- `HELD` -> `HOLD`, `REINFORCE`, `LIGHTEN`, `SELL`, `WAIT`

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

French wording may vary for pedagogy, but no extra canonical status may be introduced.
Expressions like “pattern envisagé” or “proche de validation” remain explanatory wording only.

Detailed truth remains in:
- `validation.state`
- `invalidation.state`

## AnalysisOutcome
Single business-level taxonomy covering:
- executable outcomes
- business-level non-executable outcomes

Technical failures remain HTTP/API errors.
AnalysisOutcome is a first-rank visible business concept in V1 documentation and UI behavior.

## Deterministic text rule
All mandatory explanatory texts in V1 are deterministic, rule-generated, and versionable.
No free-form or AI-authored mandatory text in V1.

## Continuation pattern reference pack
The following reference ids are now reserved in the specification for the intended V1 multi-pattern continuation scope:
- `PATTERN-REF-RECTANGLE-CONTINUATION` → `Doc/product/pattern_reference_pack/PATTERN-REF-RECTANGLE-CONTINUATION.md`
- `PATTERN-REF-SYMMETRICAL-TRIANGLE-CONTINUATION` → `Doc/product/pattern_reference_pack/PATTERN-REF-SYMMETRICAL-TRIANGLE-CONTINUATION.md`
- `PATTERN-REF-BULL-FLAG-CONTINUATION` → `Doc/product/pattern_reference_pack/PATTERN-REF-BULL-FLAG-CONTINUATION.md`
- `PATTERN-REF-BEAR-FLAG-CONTINUATION` → `Doc/product/pattern_reference_pack/PATTERN-REF-BEAR-FLAG-CONTINUATION.md`

These files define business interpretation intent and wording guardrails only.
They do not prove that the active API runtime already supports the four patterns end to end.


## Support reading and parameter pedagogy
The product may expose a separate support reading composed of:
- explicit PEA product status
- category-level fundamental scoring
- parameter-by-parameter explanations

A visible parameter explanation must remain pedagogical and deterministic.
A visible parameter explanation must not become a hidden timing engine or a standalone final recommendation.

## Admin and governance
V1 governance requires an admin/back-office perimeter for:
- PEA registry truth
- scoring policy version visibility
- parameter dictionary governance
- recommendation wording version visibility
- snapshot auditability

Admin is part of the functional product architecture.
It is not only a technical utility concern.
