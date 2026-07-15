export interface ClientPortfolioPosition {
  UserAssetId: string;
  Instrument: { InstrumentId: string; Symbol: string; DisplayName: string; AssetType: string; Exchange: string; Currency: string; CountryCode: string | null };
  QuantityHeld: number;
  AverageCost: number;
  Fees: number;
  OutstandingAmount: number;
  CurrentPriceNative: number;
  Currency: string;
  ForexRateUsed: number;
  MarketReading: { OutcomeDisplayLabel: string; PrimaryPatternDisplayName: string | null; ConfidenceLabel: string | null; RiskHint: string | null };
  SupportReading: { AvailabilityDisplayLabel: string; PeaDisplayLabel: string };
  Recommendation: { DisplayLabel: string; ExplanationSummary: string; WarningText: string | null };
  RiskHint: string | null;
  HistoryEntryUrl: string | null;
  SimulationUrl: string | null;
}

export interface AllocationSlice {
  Label: string;
  WeightPct: number;
  ValueEur: number;
}

export interface ConcentrationAlert {
  Message: string;
}

export type DiversificationRating = 'Concentrated' | 'Moderate' | 'Diversified';

export interface PortfolioAllocation {
  SectorAllocation: AllocationSlice[];
  CountryAllocation: AllocationSlice[];
  CurrencyAllocation: AllocationSlice[];
  ConcentrationScore: number;
  DiversificationRating: DiversificationRating;
  ConcentrationAlerts: ConcentrationAlert[];
  PortfolioReturn30d: number | null;
  PortfolioReturn90d: number | null;
  PortfolioReturn365d: number | null;
  BenchmarkReturn30d: number | null;
  BenchmarkReturn90d: number | null;
  BenchmarkReturn365d: number | null;
  BenchmarkUnavailable: boolean;
}

export interface ClientPortfolio {
  Positions: ClientPortfolioPosition[];
  TotalInvestedAmount: number;
  TotalOutstandingAmount: number;
  OpenPositionCount: number;
  Allocation: PortfolioAllocation | null;
}
