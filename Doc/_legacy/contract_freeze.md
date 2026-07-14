# V1 CONTRACT FREEZE

## 1.0 Scope of this freeze

This document freezes the V1 canonical contracts that are necessary to support:
- the market-analysis core;
- the authenticated user experience;
- the first self-service surfaces visible in the V1 screen specification;
- the first admin user-governance surface visible in the V1 screen specification.

It does not freeze implementation details, UI composition, transport technology, or framework-specific shapes.

---

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
| `assetType` | Mandatory | enum `AssetType` | Canonical type; V1 active value is `EQUITY`. |
| `isActive` | Mandatory | boolean | Whether the instrument is eligible for new watchlist, portfolio, and analysis actions. |
| `lastProfileSyncUtc` | Optional | datetime | Technical freshness marker for metadata refresh. |
| `summary` | Optional | string | Short pedagogical description, not provider raw text. |

### `AssetType`
- `EQUITY`

### Invariants
- `symbol`, `providerSymbol`, `displayName`, `marketCode`, `currencyCode`, `assetType` are never empty.
- Uniqueness is enforced by `symbol + marketCode + assetType`.
- `providerSymbol` is provider-specific; `symbol` is domain-specific.
- `isActive=false` means no new analysis requests, but historical snapshots remain valid.
- V1 does not treat provider payload structure as the source of truth.
- V1 enabled instruments are restricted to active French listed equities only.

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
- `openLines` are the remaining open buy lines after strict FIFO consumption of sell quantities in V1.
- Pattern detection may read `instrumentId` and `asOfDate`, but must not depend on holdings fields.

### V1 open-line reconstruction rule
- In V1, open holding lines are reconstructed with a strict FIFO consumption rule for portfolio contextualization.
- Each `Buy` creates one open line candidate.
- Each `Sell` consumes the remaining quantity of the oldest still-open buy lines first.
- `PortfolioContext.openLines` contains only the remaining open quantities after applying FIFO consumption.
- This rule is used to reconstruct holding context, derive open-line quantities, and derive PRU from open lines.
- This V1 rule is a product simplification for contextualization; it must not be silently reinterpreted as a tax or broker accounting policy.

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
| `candleInterval` | Mandatory resolved | enum `CandleInterval` | Fixed to `ONE_DAY` in V1. |
| `analysisMode` | Mandatory resolved | enum `AnalysisMode` | Fixed to `ON_DEMAND` in V1. |
| `resolvedPatternIds` | Mandatory resolved | string[] | Final executable pattern set after policy and enablement checks. |
| `historyStartDate` | Mandatory resolved | date | Computed by API from the deepest required pattern window. |
| `historyEndDate` | Mandatory resolved | date | Equals `asOfDate` after resolution. |

### `CandleInterval`
- `ONE_DAY`

### `AnalysisMode`
- `ON_DEMAND`

### Invariants
- Caller does not control raw `startDate`/`endDate` for standard V1 analysis.
- History range is computed server-side from pattern requirements.
- `requestedPatternIds` may only contain enabled V1 pattern ids.
- V1 request is single-instrument, daily-candle only.

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

### `PatternStatus`
- `FORMING`
- `MONITORING`
- `CONFIRMED`
- `INVALIDATED`
- `COMPLETED`

### `ValidationState`
- `NOT_VALIDATED`
- `VALIDATED`
- `NOT_APPLICABLE`

### `InvalidationState`
- `ACTIVE`
- `INVALIDATED`
- `NOT_APPLICABLE`

### `ConfidenceLabel`
- `LOW`
- `MEDIUM`
- `HIGH`

### Deterministic text rule for V1
All mandatory explanatory texts in V1 are deterministic, rule-generated, and versionable.

This applies at minimum to:
- `detection.statusReason`
- `validation.reason`
- `invalidation.reason`
- `explanation.whyListed`
- `explanation.pedagogicalSummary`
- `recommendation.rationale`

---

## 1.6 Recommendation
- Contract scope: domain-facing and API-facing; persisted separately from per-pattern rows.
- Owner: recommendation policy service.
- Purpose: express user guidance after pattern assessment and holdings context are known.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `recommendationId` | Mandatory persistence-facing | string | Stable internal identifier. |
| `kind` | Mandatory | enum `RecommendationKind` | Frozen V1 guidance action. |
| `holdingContext` | Mandatory | enum `HoldingContext` | Whether the recommendation was generated for a non-held or held position. |
| `rationale` | Mandatory | string | Pedagogical justification for the guidance. |
| `basedOnPatternIds` | Mandatory | string[] | Pattern ids that informed the recommendation. |
| `reviewHorizonDays` | Optional | int | Suggested review horizon only when explicit deterministic V1 rules can justify it. |
| `policyVersion` | Mandatory | string | Version of the recommendation policy rules. |
| `warningText` | Optional | string | Extra caution note for ambiguous scenarios. |

### `HoldingContext`
- `NOT_HELD`
- `HELD`

### `RecommendationKind`
- `MONITOR`
- `WAIT`
- `BUY`
- `HOLD`
- `REINFORCE`
- `LIGHTEN`
- `SELL`

### Invariants
- If `holdingContext='NOT_HELD'`, allowed `kind` values are `MONITOR`, `WAIT`, `BUY`.
- If `holdingContext='HELD'`, allowed `kind` values are `HOLD`, `REINFORCE`, `LIGHTEN`, `SELL`, `WAIT`.
- Recommendation never contains stop loss, take profit, invalidation level, or risk/reward ratio; those stay in `riskHints`.
- Recommendation never contains detection facts; it references them through `basedOnPatternIds`.

---

## 1.7 AnalysisSnapshot
- Contract scope: persistence-facing canonical history model.
- Owner: snapshot persistence service in the API.
- Purpose: preserve one complete analysis event for audit, comparison, and later ex-post evaluation.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `snapshotId` | Mandatory | string | Stable root identifier. |
| `userId` | Mandatory | string | Analysis owner. |
| `instrumentId` | Mandatory | string | Analyzed instrument. |
| `requestedPatternIds` | Mandatory | string[] | Pattern ids requested by caller or default policy. |
| `executedPatternIds` | Mandatory | string[] | Pattern ids actually executed. |
| `outcome` | Mandatory | enum `AnalysisOutcome` | Final business outcome. |
| `requestedAtUtc` | Mandatory | datetime | Request time. |
| `completedAtUtc` | Mandatory | datetime | Completion time. |
| `engineVersion` | Mandatory | string | Analysis engine version. |
| `portfolioContext` | Mandatory | PortfolioContext | Holding context used for recommendation and explanation. |
| `recommendation` | Optional | Recommendation | Recommendation output when an executable result exists. |
| `patternAssessments` | Mandatory | PatternAssessment[] | Per-pattern output rows. |

### `AnalysisOutcome`
- `ANALYSIS_COMPLETED`
- `INSTRUMENT_NOT_ELIGIBLE`
- `INSUFFICIENT_MARKET_DATA`
- `NO_ENABLED_PATTERN`
- `NO_CREDIBLE_PATTERN`

---

## 1.8 AuthenticatedUserProfile
- Contract scope: API-facing and persistence-facing self-service account profile.
- Owner: identity/account domain.
- Purpose: support the V1 `account` and `account-security` screens without mixing profile data, notification preferences, and credentials.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `userId` | Mandatory | string | Stable user identifier. |
| `displayName` | Mandatory | string | Beginner-readable account name. |
| `email` | Mandatory | string | Primary login and contact email. |
| `role` | Mandatory | enum `UserRole` | User-facing access role. |
| `status` | Mandatory | enum `UserStatus` | Current account lifecycle state. |
| `preferredLanguageCode` | Optional | string | Preferred UI language when supported. |
| `preferredMarketScope` | Optional | string | Optional preference only; not a V1 scope override. |
| `securitySummary` | Mandatory | object | Security summary shown in account-security. |
| `notificationPreferences` | Mandatory | NotificationPreferenceSet | User notification settings. |

### `UserRole`
- `USER`
- `ADMIN`

### `UserStatus`
- `ACTIVE`
- `PENDING`
- `DISABLED`

### `securitySummary`
Mandatory fields:
- `hasPassword: boolean`
- `lastPasswordChangeUtc: datetime?`

Optional fields:
- `recoveryEmailMasked: string`

### Invariants
- `role` drives access routing after login.
- `notificationPreferences` are managed from account surfaces, not from the notification center.
- `securitySummary` summarizes credential state; it is not the credential itself.

---

## 1.9 NotificationItem
- Contract scope: API-facing user notification center item.
- Owner: user-communication/application layer.
- Purpose: expose one actionable notification in the V1 notification center.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `notificationId` | Mandatory | string | Stable item identifier. |
| `userId` | Mandatory | string | Target user. |
| `category` | Mandatory | enum `NotificationCategory` | Product category of the item. |
| `status` | Mandatory | enum `NotificationStatus` | Read state. |
| `title` | Mandatory | string | Short actionable title. |
| `summary` | Mandatory | string | Concise supporting text. |
| `createdAtUtc` | Mandatory | datetime | Creation time. |
| `targetScreen` | Optional | enum `NotificationTargetScreen` | Product screen to open from the notification. |
| `targetEntityId` | Optional | string | Optional linked entity identifier. |

### `NotificationCategory`
- `WATCHLIST`
- `ANALYSIS`
- `LEARNING`
- `ACCOUNT`

### `NotificationStatus`
- `UNREAD`
- `READ`

### `NotificationTargetScreen`
- `INSTRUMENT_DETAIL`
- `ANALYSIS_RESULT`
- `HELP_CENTER`
- `ACCOUNT`

### Invariants
- The notification center prioritizes and routes; it does not own notification preference editing.
- A notification may exist without a deep link, but actionable items should link when possible.

---

## 1.10 NotificationPreferenceSet
- Contract scope: API-facing and persistence-facing user preferences.
- Owner: identity/account domain.
- Purpose: store user choices about V1 notification categories.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `watchlistEnabled` | Mandatory | boolean | Whether watchlist-related notifications are enabled. |
| `analysisEnabled` | Mandatory | boolean | Whether new-analysis notifications are enabled. |
| `learningEnabled` | Mandatory | boolean | Whether learning/help notifications are enabled. |
| `accountEnabled` | Mandatory | boolean | Whether account-related notifications are enabled. |

---

## 1.11 HelpEntry
- Contract scope: API-facing contextual help item.
- Owner: product-content/application layer.
- Purpose: support the V1 help center with contextual, deterministic, non-conversational guidance.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `helpEntryId` | Mandatory | string | Stable help item identifier. |
| `category` | Mandatory | enum `HelpCategory` | Help grouping. |
| `title` | Mandatory | string | User-facing question or topic title. |
| `summary` | Mandatory | string | Concise explanation. |
| `targetScreen` | Optional | enum `HelpTargetScreen` | Product screen that the help item clarifies. |
| `displayOrder` | Mandatory | int | Stable ordering in the help center. |

### `HelpCategory`
- `ANALYSIS_RESULT`
- `SNAPSHOTS`
- `ACCOUNT`
- `GENERAL`

### `HelpTargetScreen`
- `ANALYSIS_RESULT`
- `SNAPSHOT_COMPARISON`
- `HISTORY`
- `ACCOUNT`

### Invariants
- V1 help is contextual and deterministic.
- V1 help is not a chat assistant, ticketing workflow, or notification feed.

---

## 1.12 PasswordRecoveryRequest
- Contract scope: API-facing anonymous request.
- Owner: identity/authentication boundary.
- Purpose: start the V1 forgot-password flow.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `email` | Mandatory | string | Email entered by the user. |

### Invariants
- The response must not disclose whether the account exists.

---

## 1.13 PasswordResetCommand
- Contract scope: API-facing anonymous request.
- Owner: identity/authentication boundary.
- Purpose: complete the V1 reset-password flow.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `resetToken` | Mandatory | string | Password reset token issued by the system. |
| `newPassword` | Mandatory | string | New password to set. |
| `confirmPassword` | Mandatory | string | Confirmation value used by the client and/or API validation. |

### Invariants
- `newPassword` and `confirmPassword` must match at validation boundary.
- The token lifecycle is part of auth implementation, not of product screen wording.

---

## 1.14 AdminUserSummary
- Contract scope: API-facing admin user-management row and detail summary.
- Owner: admin/user-governance domain.
- Purpose: support the V1 `admin-users` screen.

| Field | V1 status | Type | Meaning |
|---|---|---:|---|
| `userId` | Mandatory | string | Stable user identifier. |
| `displayName` | Mandatory | string | Admin-readable user name. |
| `email` | Mandatory | string | Primary email. |
| `role` | Mandatory | enum `UserRole` | User role. |
| `status` | Mandatory | enum `UserStatus` | Lifecycle state. |
| `lastAccessUtc` | Optional | datetime | Last known access timestamp. |

### Invariants
- V1 admin user management covers listing, filtering, and opening user management detail/actions.
- This contract does not imply multi-tenant organization modeling.

---

## 2. Screen-alignment rule

The contracts frozen here are sufficient to support the V1 product surfaces that matter for the next API backlog phase:
- `login`
- `forgot-password`
- `reset-password`
- `account`
- `account-security`
- `notifications`
- `help-center`
- `admin-users`
- all core analysis screens already covered by the analysis contracts

If a future screen needs more data than these contracts provide, the gap must be called out explicitly instead of silently inferred.

---

## 3. Deferred on purpose

The following remain outside this V1 freeze:
- SSO
- MFA
- organization/workspace contracts
- chat-based help
- notification delivery channel orchestration details
- real-time push infrastructure
- free-form AI explanations
- broker synchronization
- execution/trading contracts
