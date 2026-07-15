export interface PatternStatisticsItem {
  readonly PatternId: string;
  readonly HasEarningsInWindow: boolean;
  readonly SampleSize: number;
  readonly InsufficientData: boolean;
  readonly WinRate: number | null;
  readonly WinRateLow: number | null;
  readonly WinRateHigh: number | null;
  readonly SelectionBiasDisclaimer: boolean;
}

export interface PatternStatisticsResult {
  readonly PatternStats: PatternStatisticsItem[];
  readonly SelectionBiasDisclaimer: boolean;
}
