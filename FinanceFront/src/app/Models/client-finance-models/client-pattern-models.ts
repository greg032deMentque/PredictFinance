export interface ClientPatternCandidate {
  PatternId: string;
  DisplayName: string;
  Confidence: number;
  Probability: number;
  ConfidenceLabel: string;
  Phase: string;
  IsPrimary: boolean;
  NecklinePrice: number | null;
  TargetPrice: number | null;
  InvalidationPrice: number | null;
}

export interface ClientPatternEvaluateResult {
  AnalysisId: string;
  Symbol: string;
  Candidates: ClientPatternCandidate[];
}

export interface ClientScenarioBranch {
  TriggerLabel: string;
  TriggerLevel: number | null;
  Direction: 'Up' | 'Down';
  ResultingState: 'Confirmed' | 'Invalidated';
  Posture: string;
  Rationale: string;
}

export interface ClientPatternConfidenceCriterion {
  Code: string;
  Label: string;
  State: string;
  Source: string;
}

export interface ClientPatternConfidenceBreakdown {
  Level: string;
  Criteria: ClientPatternConfidenceCriterion[];
}

export interface ClientPatternPosture {
  DisplayLabel: string;
  ExplanationSummary: string;
  WarningText: string | null;
  Kind: string | null;
  HoldingStatus: string | null;
}

export interface ClientPatternDetail {
  PatternId: string;
  DisplayName: string;
  Phase: string;
  ConfidenceBreakdown: ClientPatternConfidenceBreakdown;
  NecklinePrice: number | null;
  TargetPrice: number | null;
  InvalidationPrice: number | null;
  LifecyclePhaseCode: string;
  DetectionStatus: string;
  ValidationState: string;
  InvalidationState: string;
  ScenarioBranches: ClientScenarioBranch[];
  Posture: ClientPatternPosture;
}

export interface ClientPatternEvaluateRequest {
  Symbol: string;
  HoldingContext: 'NotHeld' | 'Held' | null;
}

export interface CreateClientAlertRequest {
  Symbol: string;
  Trigger: 'LevelCrossed' | 'PatternStateChange';
  LevelValue: number | null;
  PatternId: string | null;
}
