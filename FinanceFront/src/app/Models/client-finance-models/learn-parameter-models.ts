// --- Learning ---
export interface LearnTopic {
  TopicId: string;
  Title: string;
  Summary: string;
  RoutePath: string;
}

export interface OnboardingStep {
  Order: number;
  StepCode: string;
  Label: string;
  RoutePath: string;
}

export interface OnboardingGuidance {
  ShouldDisplay: boolean;
  GuidanceCode: string;
  Title: string;
  Summary: string;
  SuggestedSteps: OnboardingStep[];
}

export interface RuntimeScopePattern {
  PatternCode: string;
  DisplayLabel: string;
}

export interface RuntimeScope {
  RuntimePerimeterId: string;
  MarketScopeLabel: string;
  TimeGranularity: string;
  EtfSupportEnabled: boolean;
  BrokerExecutionEnabled: boolean;
  SupportedPatterns: RuntimeScopePattern[];
}

export interface LearnOverview {
  RuntimeScope: RuntimeScope;
  Topics: LearnTopic[];
  Onboarding: OnboardingGuidance | null;
}

// --- Parameter Detail ---
export interface ParameterCurrentValue {
  IsAvailable: boolean;
  NumericValue: number | null;
  DisplayValue: string;
  AvailabilityLabel: string;
  SourceLabel: string;
  AsOfUtc: string | null;
}

export interface ParameterDetail {
  ParameterId: string;
  CategoryCode: string;
  Label: string;
  RoleInCategory: string;
  SimpleDefinition: string;
  HowToReadCurrentValue: string;
  WhyItMatters: string;
  LimitsOfInterpretation: string;
  WhatItSupports: string;
  WhatItDoesNotProve: string;
  ImplicationWithoutPosition: string;
  ImplicationWithPosition: string;
  Instrument: { Symbol: string; DisplayName: string };
  HoldingStatus: string;
  HoldingContextLabel: string;
  CurrentValue: ParameterCurrentValue;
  PeaEligibilityStatus: string;
  PeaDisplayLabel: string;
}

// --- Snapshot Comparison ---
export interface SnapshotDeltaItem {
  FieldCode: string;
  DisplayLabel: string;
  LeftValue: string | null;
  RightValue: string | null;
  ChangeKind: string;
  EvidenceType: string;
}

export interface SnapshotComparison {
  Left: Record<string, unknown>;
  Right: Record<string, unknown>;
  MarketChanges: SnapshotDeltaItem[];
  SupportChanges: SnapshotDeltaItem[];
  RecommendationChanges: SnapshotDeltaItem[];
  NonComparabilityReasons: string[];
}
