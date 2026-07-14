export interface ClientPortfolioPosition {
  UserAssetId: string;
  Instrument: { InstrumentId: string; Symbol: string; DisplayName: string; AssetType: string; Exchange: string; Currency: string; CountryCode: string | null };
  QuantityHeld: number;
  AverageCost: number;
  Fees: number;
  OutstandingAmount: number;
  MarketReading: { OutcomeDisplayLabel: string; PrimaryPatternDisplayName: string | null; ConfidenceLabel: string | null; RiskHint: string | null };
  SupportReading: { AvailabilityDisplayLabel: string; PeaDisplayLabel: string };
  Recommendation: { DisplayLabel: string; ExplanationSummary: string; WarningText: string | null };
  RiskHint: string | null;
  HistoryEntryUrl: string | null;
  SimulationUrl: string | null;
}
export interface ClientPortfolio { Positions: ClientPortfolioPosition[]; TotalInvestedAmount: number; TotalOutstandingAmount: number; OpenPositionCount: number; }
