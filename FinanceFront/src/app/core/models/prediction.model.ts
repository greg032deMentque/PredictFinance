export interface PatternProbability {
  pattern: string;
  probability: number;
}

export interface PredictionResult {
  symbol: string;
  predictedAt: string;
  pattern: string;
  lastProbability: number;
  meanProbability: number;
  maxProbability: number;
  probabilityPct: number;
  suggestedAction: 'buy' | 'hold' | 'sell';
  actionConfidence: number;
  actionReason: string;
  nWindows: number;
  patterns: PatternProbability[];
}
