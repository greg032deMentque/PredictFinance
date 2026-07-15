export interface RsiIndicator {
  Value: number;
  Signal: string;
}

export interface MacdIndicator {
  Line: number;
  SignalLine: number;
  Histogram: number;
  Trend: string;
}

export interface BollingerBands {
  Upper: number;
  Middle: number;
  Lower: number;
  CurrentPrice: number;
  Position: string;
}

export interface MovingAverages {
  Ma20: number | null;
  Ma50: number | null;
  Ma200: number | null;
  CurrentPrice: number;
}

export interface IndicatorSynthesis {
  Label: string;
  BullishSignals: number;
  BearishSignals: number;
  TotalSignals: number;
}

export interface TechnicalIndicators {
  Symbol: string;
  ComputedAtUtc: string;
  DataPointsUsed: number;
  Rsi: RsiIndicator | null;
  Macd: MacdIndicator | null;
  BollingerBands: BollingerBands | null;
  MovingAverages: MovingAverages;
  Obv: number | null;
  Synthesis: IndicatorSynthesis | null;
}
