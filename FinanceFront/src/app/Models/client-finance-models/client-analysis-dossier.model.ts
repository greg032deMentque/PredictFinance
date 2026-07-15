/** Contrat exact : POST /api/ClientFinance/analysis/run → AnalysisDossierViewModel */

export type AnalysisOutcome =
  | 'CrediblePatternFound'
  | 'MultipleCompatiblePatterns'
  | 'NoCrediblePattern'
  | 'InsufficientData'
  | 'UnsupportedInstrument'
  | 'UnsupportedContext';

export interface PriceCandle {
  Timestamp: string;
  Open: number;
  High: number;
  Low: number;
  Close: number;
  Volume: number;
}

export interface StructuralPoint {
  PointType: string;
  Timestamp: string;
  Price: number;
}

export interface SrZone {
  PriceLow: number;
  PriceHigh: number;
  PriceMid: number;
  TouchCount: number;
  ZoneType: 'support' | 'resistance' | 'both';
  Strength: number;
}

export interface AnalysisWindow {
  Interval: string;
  StartDate: string;
  EndDate: string;
  RequiredCandles: number;
  ActualCandles: number;
}

export interface AnalysisPattern {
  PatternId: string;
  DisplayName: string;
  PedagogicalDescription: string;
  PhaseCode: string;
  PhaseLabel: string;
  Status: string;
  IsCompatible: boolean;
  ConfidenceScore: number;
  ConfidenceLabel: string;
  /** Fiabilité historique Bulkowski [0-1]. Null si snapshot antérieur à M1. */
  ProbabilityScore: number | null;
  ProbabilityLabel: string | null;
  IsCredible: boolean;
  ScoreReasons: string[];
  CurrentPrice: number;
  /** Toujours null actuellement — ne pas afficher */
  NecklinePrice: number | null;
  ValidationState: string;
  ValidationLevel: number | null;
  ValidationDate: string | null;
  InvalidationState: string;
  InvalidationLevel: number | null;
  InvalidationDate: string | null;
  HasRiskPlan: boolean;
  SuggestedStopLoss: number | null;
  SuggestedTakeProfit: number | null;
  RiskRewardRatio: number | null;
  PositioningNote: string | null;
  StructuralPoints: StructuralPoint[];
  WhyListed: string;
  PedagogicalSummary: string;
  AmbiguityNote: string | null;
  LimitationsNote: string | null;
  IsActionable: boolean;
  RecommendationAction: string;
  RecommendationReason: string;
  RiskLevel: string;
  RecommendationHorizonDays: number;
}

/**
 * Contrat partiel de BackPredictFinance.Common.AnalysisV1.AnalysisRiskContext.
 * Seuls les champs earnings sont mappés côté front pour l'instant — le reste
 * du contrat (ATR, stop-loss, targets, volume) n'est pas consommé par l'UI.
 */
export interface AnalysisRiskContext {
  EarningsWithinHorizonWarning: boolean;
  NextEarningsDateUtc: string | null;
}

export interface AnalysisDossier {
  Id: string;
  Symbol: string;
  CompanyName: string;
  Outcome: AnalysisOutcome;
  OutcomeMessage: string;
  GlobalSummary: string;
  PredictedAt: string;
  ModelStatus: string;
  ModelMessage: string;
  AnalysisWindow: AnalysisWindow | null;
  PriceSeries: PriceCandle[];
  MainPattern: AnalysisPattern | null;
  AlternativePatterns: AnalysisPattern[];
  SrZones: SrZone[];
  RiskContext: AnalysisRiskContext | null;
}
