# V1 state of play and remaining work

## Purpose

This document gives a readable V1 state from the real repository and from the current documentation set.
It separates repository proof from target decisions.

## PROVEN in the current solution

- The solution contains `BackPredictFinance.API`, `BackPredictFinance.Services`, `BackPredictFinance.Datas`, `BackPredictFinance.ViewModels`, `BackPredictFinance.Common`, `BackPredictFinance.Patterns`, and `BackPredictFinance.Tests`.
- The solution does not currently contain `BackPredictFinance.Contracts`.
- `BackPredictFinance.Services` currently references `BackPredictFinance.ViewModels`.
- Core analysis-domain contracts are still present under `BackPredictFinance.Common/AnalysisV1`.
- `BackPredictFinance.Patterns` already contains continuation-pattern identifiers for `RectangleContinuation`, `SymmetricalTriangleContinuation`, `BullFlagContinuation`, and `BearFlagContinuation`.
- The DI bootstrap already registers continuation-pattern definitions and the analysis registry.
- The solution still keeps legacy compatibility residue around `DOUBLE_TOP`.
- A fundamentals provider path already exists through `IFundamentalsProvider` and `YahooFinanceMarketDataProvider`.
- Persistence already includes `AnalysisRun`, `PatternAssessment`, `DecisionSignal`, and `ModelSnapshot`.
- No dedicated persisted PEA registry, no scoring endpoint, and no end-to-end fundamental scoring path were found in the provided backend repository.

## DECIDED in the documentation set

- V1 remains backend-owned and deterministic.
- V1 technical analysis is multi-pattern in business scope.
- `mainPattern` is display-primary only and must never erase compatible alternatives.
- Fundamental scoring is a separate capability from technical pattern analysis.
- PEA eligibility must be explicit and traceable when that capability is implemented.
- V1 wording must remain deterministic and stable.
- V1 must surface insufficient, unknown, unsupported, and unavailable states explicitly.

## Challenge of the previous global analysis

The previous global analysis was directionally correct, but two corrections are important.

### What was too broad

- Not all criticized areas were truly undocumented.
- Some history, wording, and V1 perimeter rules already existed in the docs or in the repository.

### What is the real gap

The real remaining problem is not the total absence of specification.
It is that the specification was still too distributed and not closed enough on decision rules.
Without tighter closure, two careful implementations could still diverge while both claiming compliance.

## What is now well locked

### Technical-analysis V1
- product intent
- V1 perimeter
- strict FIFO portfolio contextualization rule
- multi-pattern continuation target
- snapshot concept
- pattern reference pack
- main versus alternative pattern semantics

### Fundamental scoring / PEA
- additive V1 capability
- explicit PEA statuses
- explicit composite-score availability states
- category and coverage rules
- no hidden merge with technical recommendation

### Wording
- stable beginner-facing pattern labels
- stable recommendation verbs
- explicit no-pattern wording
- explicit score-unavailable wording causes

## Remaining work after the documentation update

### Repository implementation work
- remove remaining mono-pattern runtime residue from the active path where still present
- reduce or remove `Services -> ViewModels` coupling in touched scope
- stop growing `Common/AnalysisV1` as the de facto analysis-domain contract area
- decide and implement the concrete persistence model for PEA eligibility registry facts
- implement the deterministic fundamental scoring service
- expose scoring through explicit transport models and API routes
- add tests for scoring, PEA status handling, and unavailable states

### Specification areas that are now closed by doc but still unimplemented
- fundamental scoring category contract
- composite-score availability states
- side-by-side coexistence rule between technical analysis and scoring
- closed wording taxonomy for explicit absence and insufficiency states
- V1 decision tables for recommendation and UI wording

## Explicit V1 non-ready points

The repository must not yet be presented as fully V1-ready for the complete intended product perimeter if any of the following remains true:
- continuation multi-pattern support is not fully proven end to end on the active runtime path
- technical recommendation still depends on residual mono-pattern assumptions
- PEA eligibility has no explicit persisted product truth
- fundamental scoring has no deterministic backend-owned implementation path
- required unavailable-state outputs are not tested

## Practical next-step priority

1. finish the active runtime-path cleanup for technical multi-pattern truth
2. implement persisted PEA eligibility product truth
3. implement deterministic fundamental scoring service and transport
4. persist score snapshots only after the score contract is real
5. add proof-oriented tests for all explicit unavailable states
