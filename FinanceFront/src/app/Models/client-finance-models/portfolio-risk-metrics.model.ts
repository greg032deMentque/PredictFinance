export interface PortfolioRiskMetrics {
  DataPointsUsed: number;
  Twr: number | null;
  SharpeRatio: number | null;
  AnnualizedVolatility: number | null;
  MaxDrawdown: number | null;
  PeriodStartUtc: string | null;
  PeriodEndUtc: string | null;
}
