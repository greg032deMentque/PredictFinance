# PredictFinance V1 — Frontend Display Object Classes and Types

These object classes are intentionally proposed as a **frontend display-model layer** for the UI.
They are invented on purpose because the request explicitly asked for object classes and types.

They are constrained by the v3 spec and designed to preserve:
- separation between market, support, PEA, recommendation, and data completeness
- held / not-held recommendation split
- traceability for admin governance surfaces

Language chosen here: TypeScript.

## Core enums

```ts
export type TechnicalOutcome =
  | "CREDIBLE_PATTERN_FOUND"
  | "MULTIPLE_COMPATIBLE_PATTERNS"
  | "NO_CREDIBLE_PATTERN"
  | "INSUFFICIENT_DATA"
  | "UNSUPPORTED_INSTRUMENT"
  | "UNSUPPORTED_CONTEXT";

export type PatternProgressState =
  | "FORMING"
  | "MONITORING"
  | "CONFIRMED"
  | "INVALIDATED"
  | "COMPLETED"
  | "ABSENT";

export type RecommendationVerbNotHeld =
  | "MONITOR"
  | "WAIT"
  | "BUY";

export type RecommendationVerbHeld =
  | "WAIT"
  | "HOLD"
  | "REINFORCE"
  | "LIGHTEN"
  | "SELL";

export type PeaStatus =
  | "PEA_CONFIRMED_ELIGIBLE"
  | "PEA_CONFIRMED_INELIGIBLE"
  | "PEA_UNKNOWN";

export type CompositeScoreAvailability =
  | "AVAILABLE"
  | "INSUFFICIENT_COVERAGE"
  | "PEA_UNKNOWN"
  | "CONFIRMED_INELIGIBLE_IN_UNIVERSE"
  | "UNSUPPORTED_UNIVERSE"
  | "PROVIDER_DATA_INCOMPLETE";

export type SupportAvailability =
  | "FULL"
  | "PARTIAL"
  | "UNAVAILABLE";

export type DataFreshnessState =
  | "FRESH"
  | "AGING"
  | "STALE"
  | "MISSING";

export type UserRole =
  | "USER"
  | "ADMIN";

export type UserStatus =
  | "ACTIVE"
  | "PENDING"
  | "SUSPENDED";
```

## Identity and metadata

```ts
export interface InstrumentIdentityVm {
  instrumentId: string;
  displayName: string;
  isin: string | null;
  ticker: string | null;
  assetTypeLabel: string;
  countryCode: string | null;
  exchangeLabel: string | null;
}

export interface FreshnessVm {
  state: DataFreshnessState;
  checkedUtc: string | null;
  displayLabel: string;
}

export interface VersionContextVm {
  scoringVersion: string | null;
  wordingVersion: string | null;
  policyVersion: string | null;
  rulesVersion: string | null;
}
```

## User-facing reading objects

```ts
export interface AlternativePatternVm {
  patternCode: string;
  displayLabel: string;
  progressState: PatternProgressState;
}

export interface MarketReadingVm {
  outcome: TechnicalOutcome;
  primaryPatternCode: string | null;
  primaryPatternLabel: string | null;
  progressState: PatternProgressState;
  confidenceLabel: string | null;
  invalidationHint: string | null;
  riskHint: string | null;
  alternatives: AlternativePatternVm[];
}

export interface SupportReadingVm {
  supportAvailability: SupportAvailability;
  compositeScoreValue: number | null;
  compositeScoreDisplay: string;
  compositeScoreAvailability: CompositeScoreAvailability;
  peaStatus: PeaStatus;
  peaDisplayLabel: string;
  providerDataCompletenessLabel: string | null;
}

export interface RecommendationVm {
  context: "NOT_HELD" | "HELD";
  verb: RecommendationVerbNotHeld | RecommendationVerbHeld;
  displayLabel: string;
  explanationSummary: string | null;
}

export interface ParameterReadingVm {
  parameterId: string;
  label: string;
  simpleDefinition: string;
  advancedDefinition: string | null;
  categoryLabel: string;
  readingDirectionSemantics: string;
  interpretationGuardrails: string[];
  interpretationLimits: string[];
  whatItDoesNotProve: string[];
  implicationWithoutPosition: string[];
  implicationWithPosition: string[];
  wordingVersionStatus: string;
}
```

## Page-level user cards

```ts
export interface WatchlistItemVm {
  instrument: InstrumentIdentityVm;
  marketReadingSummary: string;
  supportReadingSummary: string;
  peaSummary: string;
  dataCompletenessSummary: string;
  recommendationSummary: string;
  lastAnalysisUtc: string | null;
  freshness: FreshnessVm | null;
}

export interface PortfolioPositionVm {
  instrument: InstrumentIdentityVm;
  quantityHeld: number;
  averageCostDisplay: string;
  marketReadingSummary: string;
  supportReadingSummary: string;
  recommendationSummary: string;
  riskHint: string | null;
  historyEntryUrl: string | null;
  simulationUrl: string | null;
}

export interface InstrumentDetailVm {
  instrument: InstrumentIdentityVm;
  marketReading: MarketReadingVm;
  supportReading: SupportReadingVm;
  recommendationForNotHeld: RecommendationVm;
  recommendationForHeld: RecommendationVm;
  visibleParameters: ParameterReadingVm[];
  versions: VersionContextVm;
  freshness: FreshnessVm | null;
}

export interface AnalysisResultVm {
  instrument: InstrumentIdentityVm;
  marketReading: MarketReadingVm;
  supportReading: SupportReadingVm;
  recommendationForNotHeld: RecommendationVm;
  recommendationForHeld: RecommendationVm;
  versions: VersionContextVm;
}
```

## History and comparison

```ts
export interface PersistedSnapshotSummaryVm {
  snapshotId: string;
  instrument: InstrumentIdentityVm;
  timestampUtc: string;
  outcome: TechnicalOutcome;
  primaryPatternLabel: string | null;
  recommendationSummary: string;
  supportAvailabilitySummary: string;
  peaSummary: string;
  versionContext: VersionContextVm;
}

export interface SnapshotComparisonVm {
  left: PersistedSnapshotSummaryVm;
  right: PersistedSnapshotSummaryVm;
  marketChangeSummary: string[];
  supportChangeSummary: string[];
  recommendationChangeSummary: string[];
  nonComparabilityReasons: string[];
}
```

## Authentication and account

```ts
export interface LoginOptionVm {
  role: UserRole;
  email: string;
  mockPassword: string;
  targetUrl: string;
}

export interface AccountProfileVm {
  userId: string;
  displayName: string;
  email: string;
  role: UserRole;
  notificationsEnabled: boolean;
  contextualHelpEnabled: boolean;
  densityMode: "COMFORT" | "COMPACT";
  themeMode: "LIGHT" | "DARK" | "SYSTEM";
}

export interface NotificationItemVm {
  notificationId: string;
  category: "ALERT" | "UPDATE" | "PEDAGOGY";
  title: string;
  summary: string;
  createdUtc: string;
  actionLabel: string | null;
  actionUrl: string | null;
  isRead: boolean;
}
```

## Admin governance objects

```ts
export interface AdminUserVm {
  userId: string;
  displayName: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  lastActivityUtc: string | null;
}

export interface InstrumentRegistryRowVm {
  instrument: InstrumentIdentityVm;
  providerIdentityMapping: string | null;
  assetTypeLabel: string;
  activeUniverseMemberships: string[];
  supportStateLabel: string;
  freshness: FreshnessVm;
}

export interface PeaRegistryRowVm {
  instrument: InstrumentIdentityVm;
  peaStatus: PeaStatus;
  sourceType: string;
  sourceReference: string | null;
  checkedUtc: string | null;
  policyVersion: string;
  notes: string | null;
  statusHistorySummary: string | null;
}

export interface ScoringPolicyVm {
  scoringVersion: string;
  activeUniverseIds: string[];
  activeCategories: string[];
  metricInclusionRules: string[];
  metricDirectionRules: string[];
  minimumCoverageRules: string[];
  coveragePenaltyRules: string[];
  versionHistory: string[];
}

export interface WordingVersionVm {
  wordingVersion: string;
  publicationState: string;
  notHeldActionVerbSet: RecommendationVerbNotHeld[];
  heldActionVerbSet: RecommendationVerbHeld[];
  recommendationStrengths: string[];
  adviceScenarioCodes: string[];
  deterministicTextTemplatesSummary: string[];
}

export interface SnapshotAuditVm {
  snapshotId: string;
  timestampUtc: string;
  versionContext: VersionContextVm;
  marketReadingPayloadSummary: string;
  supportReadingPayloadSummary: string;
  recommendationPayloadSummary: string;
  comparisonUrl: string | null;
}

export interface DataQualityVm {
  missingMetricsByCategoryCount: number;
  unsupportedOrStaleInstrumentCount: number;
  providerFreshnessIssuesCount: number;
  peaRegistryIncompletenessCount: number;
  coverageDegradationTrendLabel: string;
  issueSummaries: string[];
}
```

## Architectural note

These are **display models**, not domain truth.
The API remains the business source of truth.
The frontend should map backend DTOs into these objects so the UI preserves the v3 semantic separation rules.
