export interface IInstrumentFundamentals {
  Symbol: string;
  CompanyName: string;
  Sector: string;
  Currency: string;
  AsOfUtc: string;
  TrailingPe: number | null;
  DividendYield: number | null;
  ReturnOnEquity: number | null;
  OperatingMargin: number | null;
  CurrentRatio: number | null;
  DebtToEquity: number | null;
  RevenueGrowth: number | null;
  EarningsGrowth: number | null;
  PegRatio: number | null;
  PriceToBook: number | null;
  RecommendationKey: string | null;
  RecommendationMean: number | null;
  TargetMeanPrice: number | null;
}
