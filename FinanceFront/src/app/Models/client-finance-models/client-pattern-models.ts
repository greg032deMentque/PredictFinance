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

import type { PatternCatalogItem } from './pattern-catalog.model';

export interface AnalysisConcept {
  Code: string;
  Label: string;
  Explanation: string;
}

/** Ligne d'affichage de l'explorateur : un pattern du catalogue, éventuellement détecté en live. */
export interface PatternExplorerRow {
  catalog: PatternCatalogItem;
  candidate: ClientPatternCandidate | null;
}

export interface ClientSupportResistanceZone {
  PriceLow: number;
  PriceHigh: number;
  PriceMid: number;
  TouchCount: number;
  ZoneType: 'support' | 'resistance' | 'both';
  Strength: number;
}

export interface ClientPatternEvaluateResult {
  AnalysisId: string;
  Symbol: string;
  Candidates: ClientPatternCandidate[];
  SupportResistanceZones: ClientSupportResistanceZone[];
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
