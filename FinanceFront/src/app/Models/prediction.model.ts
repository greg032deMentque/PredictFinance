export class PatternProbability {
  pattern = '';
  probability = 0;

  constructor(init?: Partial<PatternProbability>) {
    Object.assign(this, init);
  }
}

export class PredictionResult {
  symbol = '';
  predictedAt = '';
  pattern = '';
  lastProbability = 0;
  meanProbability = 0;
  maxProbability = 0;
  probabilityPct = 0;
  suggestedAction: 'buy' | 'hold' | 'sell' = 'hold';
  actionConfidence = 0;
  actionReason = '';
  nWindows = 0;
  patterns: PatternProbability[] = [];

  constructor(init?: Partial<PredictionResult>) {
    Object.assign(this, init);
  }
}
