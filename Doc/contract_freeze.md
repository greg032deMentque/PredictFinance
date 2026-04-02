# V1 CONTRACT FREEZE

## 1.1 Instrument
- Contract scope: domain-facing canonical instrument, persisted by the API, partially exposed API-facing.
- Owner: API domain and persistence layer.
- Purpose: represent one analyzable financial instrument independently from any provider payload.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `instrumentId` | Mandatory | string | Stable internal identifier used by API and persistence. |
| `symbol` | Mandatory | string | Normalized user-facing market symbol. |
| `providerSymbol` | Mandatory | string | Symbol used by the active market-data provider. |
| `displayName` | Mandatory | string | Beginner-readable instrument name. |
| `marketCode` | Mandatory | string | Normalized market/exchange code used by the API domain. |
| `countryCode` | Mandatory | string | Country of listing; V1 uses this to constrain scope to France-first. |
| `currencyCode` | Mandatory | string | ISO currency code of the instrument quote currency. |
| `assetType` | Mandatory | enum/string | Canonical type; V1 active value is `EQUITY`. |
| `isActive` | Mandatory | boolean | Whether the instrument is eligible for new watchlist, portfolio, and analysis actions. |
| `lastProfileSyncUtc` | Optional | datetime | Technical freshness marker for metadata refresh. |
| `summary` | Optional | string | Short pedagogical description, not provider raw text. |

### Invariants
- `symbol`, `providerSymbol`, `displayName`, `marketCode`, `currencyCode`, `assetType` are never empty.
- Uniqueness is enforced by `symbol + marketCode + assetType`.
- `providerSymbol` is provider-specific; `symbol` is domain-specific.
- `isActive=false` means no new analysis requests, but historical snapshots remain valid.
- V1 does not treat provider payload structure as the source of truth.

### Scope clarification
- The `Instrument` contract is a canonical and extensible domain contract.
- Its structure is intentionally broader than the V1 enabled business scope so future instruments, providers, and asset classes can be added without redefining the contract.
- V1 enabled instruments are restricted to active French listed equities only.
- In V1, an instrument is analyzable only if:
  - `assetType = EQUITY`
  - `countryCode = FR`
  - `isActive = true`
- Contract extensibility does not imply immediate V1 business support.
- Instruments outside V1 enabled scope may exist in canonical form but must not be treated as analyzable V1 instruments.

### Explicitly deferred
- ISIN
- sector taxonomy
- corporate actions
- lot size
- issuer fundamentals
- cross-listing relationships
- news metadata

---

## 1.2 PortfolioLine
- Contract scope: domain-facing and persistence-facing holding lot; API-facing only when explicitly requested by portfolio screens.
- Owner: API domain and persistence layer.
- Purpose: represent one open holding line for one user and one instrument.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `portfolioLineId` | Mandatory | string | Stable internal identifier. |
| `userId` | Mandatory | string | Owning user. |
| `instrumentId` | Mandatory | string | Held instrument. |
| `quantity` | Mandatory | decimal | Remaining quantity currently held on this line. |
| `unitBuyPrice` | Mandatory | decimal | Original buy price per unit for this line. |
| `buyDate` | Mandatory | date | Buy date for this line. |
| `feesAmount` | Mandatory | decimal | Fees attributable to this line. |
| `currencyCode` | Mandatory | string | Currency of the line valuation basis. |
| `sourceReference` | Optional | string | Link to originating transaction or import source. |
| `note` | Optional | string | User-entered memo, non-analytical. |

### Invariants
- `quantity > 0`
- `unitBuyPrice > 0`
- `feesAmount >= 0`
- `buyDate` cannot be after the analysis `asOfDate`
- One user can own many `PortfolioLine` rows for the same `instrumentId`
- PRU is derived from open lines; it is not a stored source-of-truth field in V1.

### Relationship
- `User 1 -> N PortfolioLine`
- `Instrument 1 -> N PortfolioLine`

### What V1 needs now
- Multi-line support
- Per-line quantity, cost basis, fees, and buy date
- Derived total quantity and PRU for recommendation context

### Explicitly deferred
- Closed-lot tax accounting
- FIFO/LIFO tax policies
- dividend events
- FX conversion history
- broker import reconciliation

---

## 1.3 PortfolioContext
- Contract scope: domain-facing input to recommendation and explanation services; persistence-facing as a summarized snapshot field.
- Owner: API application layer.
- Purpose: give the analysis workflow holding context without polluting pattern detection.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `userId` | Mandatory | string | Authenticated user owning the context. |
| `instrumentId` | Mandatory | string | Instrument being analyzed. |
| `holdsInstrument` | Mandatory | boolean | Whether the user currently holds any quantity. |
| `openLineCount` | Mandatory | int | Count of open holding lines. |
| `totalQuantityHeld` | Mandatory | decimal | Sum of open line quantities. |
| `averageUnitCost` | Mandatory when `holdsInstrument=true` | decimal? | Derived PRU from open lines. |
| `currencyCode` | Mandatory | string | Currency used for the holdings context. |
| `openLines` | Mandatory | array | List of open line summaries: `quantity`, `unitBuyPrice`, `buyDate`, `feesAmount`, `currencyCode`. |
| `oldestOpenBuyDate` | Optional | date | Oldest open line date. |
| `latestOpenBuyDate` | Optional | date | Most recent open line date. |

### Invariants
- If `holdsInstrument=false`, then `openLineCount=0`, `totalQuantityHeld=0`, `averageUnitCost=null`, `openLines=[]`.
- If `holdsInstrument=true`, then `openLineCount>0`, `totalQuantityHeld>0`, `openLines` is non-empty.
- Pattern detection may read `instrumentId` and `asOfDate`, but must not depend on holdings fields.

### Exact usage boundary
- Allowed consumers: recommendation policy, pedagogical explanation, snapshot persistence.
- Forbidden consumers: provider adapters, market-data normalization, per-pattern detection rules.

---

## 1.4 AnalysisRequest
- Contract scope: API-facing request plus resolved domain-facing request.
- Owner: API controller boundary and application orchestration layer.
- Purpose: request one on-demand V1 analysis for one instrument on daily candles.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `instrumentId` | Mandatory API-facing | string | Canonical instrument to analyze. |
| `requestedPatternIds` | Optional API-facing | string[] | Explicit subset of enabled patterns to run. Empty or null means default enabled set. |
| `asOfDate` | Optional API-facing | date | Analysis date; defaults to latest available daily close. |
| `userId` | Mandatory resolved | string | Taken from auth context, never client-authored. |
| `instrument` | Mandatory resolved | Instrument | Canonical instrument context loaded by API. |
| `portfolioContext` | Mandatory resolved | PortfolioContext | Holdings context loaded by API. |
| `candleInterval` | Mandatory resolved | string | Fixed to `1d` in V1. |
| `analysisMode` | Mandatory resolved | string | Fixed to `on_demand` in V1. |
| `resolvedPatternIds` | Mandatory resolved | string[] | Final executable pattern set after policy and enablement checks. |
| `historyStartDate` | Mandatory resolved | date | Computed by API from the deepest required pattern window. |
| `historyEndDate` | Mandatory resolved | date | Equals `asOfDate` after resolution. |

### Invariants
- Caller does not control raw `startDate`/`endDate` for standard V1 analysis.
- History range is computed server-side from pattern requirements.
- `requestedPatternIds` may only contain enabled V1 pattern ids.
- V1 request is single-instrument, daily-candle only.

### What V1 needs now
- One authenticated user
- One instrument
- Optional explicit pattern subset
- Optional analysis date

### Explicitly deferred
- Batch analysis
- scheduled job request shape
- intraday intervals
- ad hoc custom history windows
- AI explanation toggles

---

## 1.5 PatternAssessment
- Contract scope: domain-facing core output; persisted per-pattern; API-facing after mapping.
- Owner: pattern execution pipeline.
- Purpose: capture one pattern’s full assessment without mixing detection, validation, invalidation, scoring, risk, and explanation.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `assessmentId` | Mandatory persistence-facing | string | Stable internal assessment id. |
| `patternId` | Mandatory | string | Canonical pattern identifier. |
| `displayName` | Mandatory | string | Beginner-readable pattern name. |
| `pedagogicalDescription` | Mandatory | string | Short educational description of the pattern concept. |
| `analysisWindow` | Mandatory | object | `interval`, `startDate`, `endDate`, `requiredCandles`, `actualCandles`. |
| `detection` | Mandatory | object | Detection facts only. |
| `validation` | Mandatory | object | Validation state only. |
| `invalidation` | Mandatory | object | Invalidation state only. |
| `scoring` | Mandatory | object | Confidence/scoring only. |
| `riskHints` | Mandatory | object | Risk-related hints only. |
| `explanation` | Mandatory | object | Explanation metadata only. |
| `trace` | Mandatory | object | Pattern/rule version traceability only. |

### `detection`
Mandatory fields:
- `isCompatible: boolean`
- `status: PatternStatus`
- `currentPhaseCode: string`
- `currentPhaseLabel: string`
- `statusReason: string`
- `currentPrice: decimal`
- `structuralPoints: array`

`structuralPoints` item fields:
- `pointType: string`
- `timestamp: datetime`
- `price: decimal`

Meaning:
- Pure market-observation facts
- No recommendation wording
- No risk policy decision

### `validation`
Mandatory fields:
- `state: 'NOT_VALIDATED' | 'VALIDATED' | 'NOT_APPLICABLE'`
- `reason: string`

Optional fields:
- `validatedAtDate: date`
- `validatedAtPrice: decimal`
- `validationRuleCode: string`

### `invalidation`
Mandatory fields:
- `state: 'ACTIVE' | 'INVALIDATED' | 'NOT_APPLICABLE'`
- `reason: string`

Optional fields:
- `invalidationLevel: decimal`
- `breachedAtDate: date`
- `breachedAtPrice: decimal`
- `invalidationRuleCode: string`

### `scoring`
Mandatory fields:
- `confidenceScore: decimal`
- `confidenceLabel: 'LOW' | 'MEDIUM' | 'HIGH'`
- `isCredible: boolean`
- `scoreReasons: string[]`

Optional fields:
- `scoreVersion: string`

Invariants:
- `confidenceScore` is bounded to `0..1`
- Scores are per-pattern and are not normalized to sum to 1 across patterns

### `riskHints`
Mandatory fields:
- `hasRiskPlan: boolean`

Optional fields:
- `suggestedStopLoss: decimal`
- `suggestedTakeProfit: decimal`
- `riskRewardRatio: decimal`
- `positioningNote: string`

Invariants:
- Risk hints may be empty even when a pattern is compatible.
- Risk hints do not choose the final recommendation.

### `explanation`
Mandatory fields:
- `whyListed: string`
- `pedagogicalSummary: string`

Optional fields:
- `ambiguityNote: string`
- `limitationsNote: string`

### `trace`
Mandatory fields:
- `patternVersion: string`
- `ruleSetVersion: string`
- `isPrimaryDisplayCandidate: boolean`

Optional fields:
- `scoringVersion: string`

### Deterministic text rule for V1
All mandatory explanatory texts in V1 are deterministic, rule-generated, and versionable.

This applies at minimum to:
- `detection.statusReason`
- `validation.reason`
- `invalidation.reason`
- `explanation.whyListed`
- `explanation.pedagogicalSummary`
- `recommendation.rationale`

Meaning:
- These texts are generated from explicit business rules and structured analysis facts.
- These texts must be reproducible for the same input data and same rule version.
- These texts must be attributable to a persisted rule/policy version.

Deferred:
- Free-form human-authored explanatory text
- AI-authored explanatory text
- Non-deterministic prose variation

Invariant:
- V1 mandatory explanatory text is not optional narration; it is part of the explainable business output contract.

### What V1 needs now
- Clean separation of responsibilities
- Enough structure to support one or many compatible patterns
- Explainable frontend display

### Explicitly deferred
- AI-authored explanations
- probability distributions over all patterns
- chart overlays
- confidence calibration diagnostics

---

## 1.6 Recommendation
- Contract scope: domain-facing and API-facing; persisted separately from per-pattern rows.
- Owner: recommendation policy service.
- Purpose: express user guidance after pattern assessment and holdings context are known.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `recommendationId` | Mandatory persistence-facing | string | Stable internal identifier. |
| `kind` | Mandatory | RecommendationKind | Frozen V1 guidance action. |
| `holdingContext` | Mandatory | `'NOT_HELD' | 'HELD'` | Whether the recommendation was generated for a non-held or held position. |
| `rationale` | Mandatory | string | Pedagogical justification for the guidance. |
| `basedOnPatternIds` | Mandatory | string[] | Pattern ids that informed the recommendation. |
| `reviewHorizonDays` | Optional | int | Suggested review horizon only when explicit deterministic V1 rules can justify it. |
| `policyVersion` | Mandatory | string | Version of the recommendation policy rules. |
| `warningText` | Optional | string | Extra caution note for ambiguous scenarios. |

### Invariants
- If `holdingContext='NOT_HELD'`, allowed `kind` values are `MONITOR`, `BUY`, `WAIT`.
- If `holdingContext='HELD'`, allowed `kind` values are `HOLD`, `REINFORCE`, `LIGHTEN`, `SELL`, `WAIT`.
- Recommendation never contains stop loss, take profit, invalidation level, or risk/reward ratio; those stay in `riskHints`.
- Recommendation never contains detection facts; it references them through `basedOnPatternIds`.
- Absence of `reviewHorizonDays` must not invalidate the recommendation contract.
- Recommendation validity depends on `kind`, `holdingContext`, `rationale`, `basedOnPatternIds`, and `policyVersion`, not on horizon presence.

### What V1 needs now
- Portfolio-aware wording
- Deterministic policy versioning
- Clear separation from detection and risk

### Explicitly deferred
- Personalized coaching style
- alert subscriptions
- AI-generated rationale variants

---

## 1.7 AnalysisSnapshot
- Contract scope: persistence-facing canonical history model.
- Owner: snapshot persistence service in the API.
- Purpose: preserve one complete analysis event for audit, comparison, and later ex-post evaluation.

### Snapshot root

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `snapshotId` | Mandatory | string | Stable root identifier. |
| `userId` | Mandatory | string | Analysis owner. |
| `instrumentId` | Mandatory | string | Analyzed instrument. |
| `requestedPatternIds` | Mandatory | string[] | Pattern ids requested by caller or default policy. |
| `executedPatternIds` | Mandatory | string[] | Pattern ids actually executed. |
| `outcome` | Mandatory | AnalysisOutcome | Final business outcome. |
| `requestedAtUtc` | Mandatory | datetime | Analysis start timestamp. |
| `completedAtUtc` | Mandatory | datetime | Analysis completion timestamp. |
| `asOfDate` | Mandatory | date | Market date analyzed. |
| `candleInterval` | Mandatory | string | `1d` in V1. |
| `marketDataProviderCode` | Mandatory | string | Provider used to fetch candles. |
| `marketDataRangeStart` | Mandatory | date | Oldest candle date used. |
| `marketDataRangeEnd` | Mandatory | date | Latest candle date used. |
| `portfolioContextSnapshot` | Mandatory | object | Summary only: `holdsInstrument`, `totalQuantityHeld`, `averageUnitCost`, `openLineCount`, `currencyCode`. |
| `primaryPatternId` | Optional | string | Display-primary compatible pattern, if any. |
| `recommendationId` | Optional | string | Related recommendation snapshot, if any. |
| `traceId` | Mandatory | string | Correlation id across layers. |
| `errorCode` | Optional | string | Business failure code when no normal assessment exists. |
| `errorMessage` | Optional | string | Safe persisted error message. |
| `analysisEngineVersion` | Mandatory | string | Version of the analysis engine. |
| `marketNormalizationVersion` | Mandatory | string | Version of normalization rules. |
| `recommendationPolicyVersion` | Mandatory | string | Version of recommendation policy. |
| `explanationPolicyVersion` | Mandatory | string | Version of explanation policy. |

### Per-pattern rows

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `snapshotPatternRowId` | Mandatory | string | Stable row identifier. |
| `snapshotId` | Mandatory | string | Parent snapshot. |
| `patternId` | Mandatory | string | Assessed pattern. |
| `displayRank` | Mandatory | int | Deterministic display ordering among compatible patterns. |
| `isCompatible` | Mandatory | boolean | Whether the pattern met the credible display threshold. |
| `isPrimaryDisplayCandidate` | Mandatory | boolean | Whether the row was eligible to become `primaryPatternId`. |
| `patternAssessmentPayload` | Mandatory | PatternAssessment | Full separated assessment contract. |

### Recommendation history

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `snapshotRecommendationId` | Mandatory | string | Stable row identifier. |
| `snapshotId` | Mandatory | string | Parent snapshot. |
| `recommendationPayload` | Mandatory | Recommendation | Frozen recommendation at that point in time. |
| `createdAtUtc` | Mandatory | datetime | Recommendation timestamp. |

### Engine and rule versioning
- Snapshot root owns:
  - `analysisEngineVersion`
  - `marketNormalizationVersion`
  - `recommendationPolicyVersion`
  - `explanationPolicyVersion`
- Per-pattern row owns:
  - `patternVersion`
  - `ruleSetVersion`
  - `scoringVersion`

### V1 rule
- Engine/rule versioning is persisted as explicit fields, not inferred later from deployment state.

### What belongs where
- Root stores run-level context, range used, outcome, context summary, and cross-cutting versions.
- Per-pattern rows store pattern-specific facts and scoring.
- Recommendation history stores only recommendation payload and its own versioned rationale.

### What does not belong in V1 snapshot root
- Raw provider payload blobs
- UI-only labels and CSS semantics
- AI-generated prose as the only explanation source

---

## 1.8 AnalysisResponse
- Contract scope: API-facing response returned to Angular for one on-demand analysis.
- Owner: API application layer.
- Purpose: expose a full beginner-readable analysis without losing traceability.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `analysisId` | Mandatory | string | Public reference to the persisted snapshot. |
| `generatedAtUtc` | Mandatory | datetime | Response generation time. |
| `asOfDate` | Mandatory | date | Market date analyzed. |
| `outcome` | Mandatory | AnalysisOutcome | Final business outcome. |
| `instrument` | Mandatory | object | `instrumentId`, `symbol`, `displayName`, `marketCode`, `currencyCode`, `assetType`. |
| `requestedPatternIds` | Mandatory | string[] | Caller-requested or default patterns. |
| `executedPatternIds` | Mandatory | string[] | Patterns actually run. |
| `mainPattern` | Optional | PatternAssessment | Deterministic display-primary compatible pattern. |
| `alternativePatterns` | Mandatory | PatternAssessment[] | Other compatible patterns kept separate and traceable. |
| `recommendation` | Optional | Recommendation | Final guidance, separate from pattern data. |
| `pedagogicalSummary` | Mandatory | string | Cross-analysis beginner summary. |
| `noCrediblePatternReason` | Optional | string | Required when `outcome=NO_CREDIBLE_PATTERN`. |
| `trace` | Mandatory | object | `traceId`, `analysisEngineVersion`, `ruleSetVersion`. |
| `warnings` | Mandatory | string[] | Non-fatal cautions such as ambiguity or reduced confidence. |

### Support requirements
- Main pattern: `mainPattern`
- Alternative compatible patterns: `alternativePatterns`
- Confidence: `mainPattern.scoring.confidenceScore` and `alternativePatterns[*].scoring.confidenceScore`
- Status/progress: `PatternAssessment.detection.status`, `currentPhaseCode`, `currentPhaseLabel`
- Recommendation: `recommendation`
- Invalidation level: `mainPattern.invalidation.invalidationLevel` and same on alternatives
- Suggested stop loss: `mainPattern.riskHints.suggestedStopLoss`
- Suggested take profit: `mainPattern.riskHints.suggestedTakeProfit`
- Risk/reward ratio: `mainPattern.riskHints.riskRewardRatio`
- Pedagogical summary: root `pedagogicalSummary` plus per-pattern `explanation.pedagogicalSummary`
- Explicit no-credible-pattern outcome: `outcome=NO_CREDIBLE_PATTERN`, `mainPattern=null`, `alternativePatterns=[]`, non-empty `noCrediblePatternReason`

### Main pattern semantics
- `mainPattern` is only the display-primary selection from the compatible `PatternAssessment` set.
- `mainPattern` is not a separate business object.
- `mainPattern` must be semantically identical to one persisted compatible pattern assessment already present in the snapshot’s per-pattern rows.
- `mainPattern` must never contain fields, meanings, or calculations that diverge from the persisted compatible assessments set.
- `alternativePatterns` are the remaining compatible persisted assessments after excluding the one selected for `mainPattern` display.
- If `mainPattern` is present, it must correspond to exactly one persisted compatible assessment.
- If no credible compatible assessment exists, `mainPattern=null`.

### Invariants
- `mainPattern` is a display-primary pattern, never a destructive overwrite of alternatives.
- Technical failures do not use `AnalysisOutcome`; they remain HTTP error responses.
- `recommendation` may be null for `INSUFFICIENT_DATA`, `UNSUPPORTED_INSTRUMENT`, or `UNSUPPORTED_CONTEXT`.
- `recommendation.kind=WAIT` is allowed for `NO_CREDIBLE_PATTERN`.

---

# 2. TAXONOMY FREEZE

## 2.1 RecommendationKind
Exact V1 values:
- `MONITOR`
- `BUY`
- `WAIT`
- `HOLD`
- `REINFORCE`
- `LIGHTEN`
- `SELL`

### Definitions
- `MONITOR`: non-holder; setup is worth following but not yet a buy signal.
- `BUY`: non-holder; setup is credible enough for an entry-oriented educational recommendation.
- `WAIT`: either non-holder or holder; no immediate change in posture is recommended.
- `HOLD`: holder; keep the current position without adding or reducing.
- `REINFORCE`: holder; add to an existing position.
- `LIGHTEN`: holder; reduce part of an existing position.
- `SELL`: holder; exit or strongly reduce because the current scenario is adverse.

### Why this list is closed for V1
- It exactly covers the product guardrail for held vs non-held wording.
- It avoids broker/execution semantics.
- It is small enough for beginner UX and deterministic API/frontend mapping.

### Deferred
- `ALERT_ONLY`
- `HEDGE`
- `SWITCH`
- `TAKE_PROFIT_PARTIAL`
- `DCA`
- strategy-specific labels

---

## 2.2 PatternStatus
`PatternStatus` is a UX-facing synthesized status only.

### Ownership clarification
- `validation.state` = detailed validation truth
- `invalidation.state` = detailed invalidation truth
- `detection.status` / `PatternStatus` = simplified V1 display status derived from detection, validation, and invalidation facts for beginner UX

### Exact V1 values
- `FORMING`
- `MONITORING`
- `CONFIRMED`
- `INVALIDATED`
- `COMPLETED`

### Definitions
- `FORMING`: a partial structure is visible, but the setup is still early.
- `MONITORING`: the pattern is compatible and worth watching, but not yet confirmed.
- `CONFIRMED`: the pattern validation condition has been met and the scenario remains active.
- `INVALIDATED`: the scenario has been contradicted by price action.
- `COMPLETED`: the scenario was previously valid but is no longer an active setup for the current analysis date because its expected move has already played out or the setup is no longer actionable.

### Invariants
- `PatternStatus` must never be used as the sole source of validation or invalidation truth.
- `PatternStatus` is display-oriented and derived.
- Persistence and business logic must rely on `validation.state` and `invalidation.state` for detailed truth.

### Why this list is closed for V1
- It is sufficient for beginner UX.
- It avoids duplicating technical sub-states already captured elsewhere.
- More granular lifecycle states are deferred.

---

## 2.3 AnalysisOutcome
`AnalysisOutcome` is a single closed V1 business-level taxonomy covering both:
- normal executable business outcomes
- business-level non-executable outcomes

### Normal executable business outcomes
- `CREDIBLE_PATTERN_FOUND`
- `MULTIPLE_COMPATIBLE_PATTERNS`
- `NO_CREDIBLE_PATTERN`

### Business-level non-executable outcomes
- `INSUFFICIENT_DATA`
- `UNSUPPORTED_INSTRUMENT`
- `UNSUPPORTED_CONTEXT`

### Clarification
- `AnalysisOutcome` does not include technical transport/runtime failures.
- Technical failures remain HTTP/API error handling concerns and are not encoded as `AnalysisOutcome`.
- `INSUFFICIENT_DATA` and unsupported-scope cases are included because they are meaningful business results of an accepted analysis request, not infrastructure exceptions.

### Why this list is closed for V1
- It covers all normal business outcomes requested.
- It keeps technical failures out of the business taxonomy.
- It is enough to freeze frontend branching and snapshot persistence behavior.

### Deferred
- Scheduled-run outcomes
- partial batch outcomes
- ex-post evaluation outcomes
- AI-explanation-only outcomes

---

# 3. RESPONSIBILITY MATRIX

| Module / Service | Responsibility | Required inputs | Produced outputs | Allowed dependencies | Forbidden dependencies | Why it exists |
|---|---|---|---|---|---|---|
| `IMarketDataProvider` | Fetch raw market data from one provider | `providerSymbol`, `interval`, `startDate`, `endDate` | Provider-specific payload or fetch result | HTTP client, provider config, retry/logging infra | Controllers, pattern logic, recommendation policy, Angular DTOs | Isolates provider integration and keeps provider switching possible |
| `MarketDataNormalizationService` | Convert provider payloads to canonical daily candles and instrument metadata | Raw provider result, instrument/provider mapping | Normalized candle series, normalized quote/profile data | `IMarketDataProvider`, canonical market-data contracts | Controllers, recommendation policy, pattern registry internals, UI models | Prevents provider payload shape from leaking into the domain |
| `PatternRegistryService` | Resolve enabled patterns and their metadata | Requested pattern ids, instrument scope | Executable pattern set with metadata | Pattern implementations, pattern catalog config | Controllers, provider DTOs, Angular models, recommendation policy internals | Freezes which patterns are available without hardcoded if-chains |
| `AnalysisOrchestrator` | End-to-end analysis workflow | Resolved `AnalysisRequest`, normalized candles, `PortfolioContext` | `AnalysisResponse`, snapshot command | Pattern registry, normalizer, risk, recommendation, explanation, snapshot persistence | Controllers doing domain logic, UI concerns, raw provider DTOs | Makes the API the source of truth for business analysis |
| `IAnalysisPattern` implementation | Execute one pattern’s detection, validation, invalidation, scoring facts | Canonical candles, `asOfDate`, pattern config | `PatternAssessment` | Shared market-data contracts, pattern-specific helpers | Controllers, recommendation policy, persistence, Angular models | Keeps pattern logic extensible and isolated |
| `RiskEvaluationService` | Derive stop, take profit, risk/reward, and risk notes | One `PatternAssessment` draft, instrument data | `riskHints` section | Pattern output, instrument metadata | Recommendation policy, controllers, provider DTOs | Separates risk evaluation from detection and recommendation |
| `RecommendationPolicyService` | Produce portfolio-aware recommendation wording | Compatible assessments, `PortfolioContext`, outcome | `Recommendation` | Pattern assessments, portfolio context, policy config | Provider DTOs, controllers, Angular models, raw persistence entities | Prevents recommendation logic from bleeding into detection or UI |
| `PedagogicalExplanationService` | Produce beginner-readable summaries from deterministic facts | Outcome, compatible assessments, recommendation, portfolio context | Root `pedagogicalSummary`, per-pattern explanation text | Pattern assessments, recommendation, localization/text resources | Python as mandatory source, controllers, provider DTOs | Keeps education text separate from business rules and allows optional future AI |
| `PortfolioContextLoader` | Load and derive holdings context for one user/instrument | `userId`, `instrumentId` | `PortfolioContext` | Portfolio lines / transactions persistence | Pattern detection, provider integration, controllers doing calculations | Centralizes multi-line position context and PRU derivation |
| `AnalysisSnapshotPersistenceService` | Persist root snapshot, per-pattern rows, and recommendation history | Final outcome, assessments, recommendation, trace/version data | Persisted `AnalysisSnapshot` ids | Persistence entities, repositories, version providers | Controllers, provider DTOs, Angular models | Makes history/versioning explicit and auditable |
| API endpoint orchestration (`ClientFinanceController` + thin app service) | Validate transport contract, resolve auth context, call orchestrator, map HTTP response | API request DTO, auth context | HTTP response / HTTP error | `AnalysisOrchestrator`, DTO mappers, auth context | Pattern rules, provider logic, recommendation logic | Keeps controllers thin and blocks domain drift into HTTP layer |
| Optional `PythonPatternAdapter` | Invoke legacy Python analysis as a non-authoritative adapter or parity tool | Canonical candles or translated adapter request | Adapter result that must be revalidated/mapped by API | CLI/runtime client, adapter mapping | Controllers, frontend, direct persistence writes, final recommendation ownership | Allows transition without keeping Python as mandatory business truth |

---

# 4. MIGRATION IMPACT MATRIX

| File / Area | Current role | Target role | Why change is needed | Risk | Likely regression surface | Validation strategy | Classification |
|---|---|---|---|---|---|---|---|
| `ClientFinanceService.cs` | Large mixed service for watchlist, quote, transactions, analysis, history, legacy fallback | Thin finance facade plus delegated analysis/portfolio services | It currently mixes validation, symbol policy, persistence, recommendation, and orchestration | High | Client dashboard, watchlist, analysis run/history, simulation | Unit tests by slice, end-to-end API smoke, build | Rewrite |
| `TradingRecommendationService.cs` | Mono-pattern sell/hold logic | Portfolio-aware recommendation policy separate from risk | V1 requires holdings context and broader recommendation kinds | High | Analysis wording, simulation wording, frontend badges | Policy unit tests for held/non-held paths | Rewrite |
| `PythonApiService.cs` | Python-centric analysis bridge and parser | Optional adapter only, never core truth | Current analysis truth sits too close to Python | High | Analysis and simulation output contracts, IA health | Adapter tests, service isolation tests | Adapt / Move |
| `PatternCatalogService.cs` | Config lookup for model directory/version | API-side pattern metadata registry seed | Good base exists but is too Python/model-oriented | Medium | Pattern enablement and resolution | Unit tests on enable/disable/default selection | Adapt |
| `TickerService.cs` | Mixed provider abstraction and market service | Market access boundary feeding normalization | Needed for provider-neutral domain contracts and French scope | High | Search, quotes, profile loading, chart loading | Service tests, DI validation, API smoke | Adapt |
| `ProgramServiceDeclarator.cs` | Flat service registration | Composition root for frozen V1 services | New service boundaries must be wired explicitly | Medium | API startup/runtime DI errors | `dotnet build`, startup smoke test | Adapt |
| `Program.cs` | Host pipeline with duplicated controller registration and permissive CORS | Clean composition root honoring new contracts | Public API contract and infrastructure wiring will change | Medium | Startup, serialization, auth, CORS | Build, startup smoke, contract tests | Adapt |
| `ClientFinanceController.cs` | Client finance HTTP surface | Thin endpoint boundary over orchestrators | Endpoint payloads must match frozen V1 contracts | Medium | Analysis request/response compatibility | API contract tests | Adapt |
| `TradingController.cs` | Direct Python exposure | Legacy/internal or removed path | It bypasses the API-as-truth rule | Medium | Any hidden callers of legacy endpoint | Repo search, smoke before removal | Defer / Remove |
| `AnalysisRun.cs` | Root analysis history entity | Snapshot root aligned with frozen contract | Missing explicit outcome/range/version/context details | Medium | History persistence and reads | Persistence tests, migration planning later | Adapt |
| `PatternAssessment.cs` | Limited per-pattern row | Full per-pattern snapshot row | Needs separated detection/validation/invalidation/scoring/risk/explanation sections | Medium | History reads and writes | Persistence tests | Adapt |
| `DecisionSignal.cs` | Current recommendation-like snapshot | Recommendation history row | Recommendation must stay separate and versioned | Medium | Analysis history reads | Persistence tests | Adapt |
| `ModelSnapshot.cs` | Model quality snapshot | Engine/rule version snapshot | V1 needs explicit rule/version traceability, not only model metrics | Medium | History persistence and admin status | Persistence tests | Adapt |
| `Recommendation.cs` | Legacy recommendation table used as fallback history | Legacy/deprecated analysis fallback only | New analyses must not use legacy history as source of truth | High | Dashboard metrics and recent analysis fallback | Transitional read tests, cutover checks | Defer |
| `AnalysisResultViewModel.cs` | Thin single-pattern response DTO | Rich V1 response DTO mapping | Public response contract is too poor for V1 | High | Frontend analysis page and history page | Contract tests, frontend mapping tests | Rewrite |
| `SimulationResultViewModel.cs` | Python-led simulation response | Either aligned with V1 analysis contract or explicitly peripheral | Current simulation semantics are not the V1 core contract | Medium | Simulation screen | API contract tests | Adapt / Defer |
| `AnalysisRunRequestViewModel.cs` | `symbol + requestedPattern` input | Frozen V1 request DTO | Needs canonical instrument identity and pattern-list semantics | High | Frontend launch analysis flow | Contract tests | Rewrite |
| `client-finance.service.ts` | Maps current DTOs to frontend models | Maps frozen V1 API contracts | Frontend contract must become explicit and stable | High | Client/admin analysis and history views | Angular build, mapping tests | Adapt |
| `client-finance-models/*` | Flat models with mono-pattern assumptions | Rich analysis models aligned to frozen contract | Current frontend model shape cannot represent V1 outputs | High | All finance UI components | Angular build, model mapping tests | Adapt |
| `client-patterns.constants.ts` | Hardcoded `DOUBLE_TOP` support | Temporary UI metadata only or removed in favor of API catalog | Mono-pattern guard must not remain in UI | Medium | Pattern selector and labels | Angular build, UI smoke | Adapt |
| `user-finance-page.component.ts` and sibling finance components | Single-result orchestration and rendering | UI consuming main + alternative patterns and frozen recommendation contract | Current UI cannot display V1 multi-scenario output cleanly | High | Client finance page, analysis history, watchlist shortcut | Angular build, manual smoke | Adapt |
| `admin-analyse-finance.ts` | Admin-triggered analysis using client model | Admin UI over same V1 response contract | Admin must not depend on a separate analysis truth | Medium | Admin analysis page | Angular build, manual smoke | Adapt |
| `FinanceIA/src/finance_ia/patterns/*` | Runtime pattern analyzer reference | Optional adapter or parity reference | Useful logic exists but cannot remain the authoritative contract | Medium | Python parity/health only | Python tests, adapter tests if retained | Adapt / Defer |
| `FinanceIA/src/finance_ia/dataset/build_dataset.py`, `validate.py`, `simulate.py` | Mono-pattern training/validation/simulation core | Peripheral tooling, not V1 analysis truth | These are not the first implementation target for the API refactor | Low | Python CLI behavior | Existing pytest suite | Defer |
| `IAController.cs` and `IAStatusService.cs` | IA health/supervision | Optional infrastructure supervision only | Should remain peripheral when Python becomes optional | Low | Admin IA status | API smoke | Adapt |

---

# 5. IMPLEMENTATION PRECONDITIONS

## Contract ambiguities now resolved
- Canonical instrument identity is API-owned.
- Portfolio lines are open lots; PRU is derived, not authoritative storage.
- Portfolio context is available to recommendation/explanation, not to detection rules.
- Analysis requests are single-instrument, daily only, and server-resolve the history window.
- Pattern assessments are structurally split into detection, validation, invalidation, scoring, risk hints, explanation, and trace.
- Recommendation is a separate object with portfolio-aware constraints.
- Snapshot history root vs per-pattern vs recommendation payload ownership is fixed.
- Confidence is per-pattern `0..1` and is not forced into 100% semantics across patterns.
- Technical failures stay as HTTP errors, not `AnalysisOutcome` values.
- `mainPattern` is a display-primary pattern only; alternatives remain persisted and exposed separately.

## Remaining blocking unknowns
- No blocker remains for Step 1 contract-led service extraction.
- Before public API contract cutover, confirm whether frontend can move to `instrumentId` immediately or needs a short transition period from `symbol`.
- Before schema work later, confirm whether existing environments have the analysis-history tables already applied and what legacy fallback removal sequence is acceptable.
- Before French-scope implementation, confirm the V1 market-data provider strategy for French equities if Yahoo coverage is judged insufficient.

## Must be confirmed before migrations or API contract changes
- Deployment sequencing for removing legacy `Recommendation` writes.
- Whether public endpoints can change in place or need a temporary compatibility response shape.
- Validation environment for Angular, since current repo state did not build locally without installed builder packages.

## Repository readiness
- The repository is ready to start Step 1 safely for contract-first implementation in the API and frontend mapping layers.
- The repository is not yet ready for schema cutover or legacy endpoint removal until rollout sequencing is explicitly agreed.
- The next safe move is implementation of service boundaries and DTOs against this freeze, with no migration generation in the first increment.