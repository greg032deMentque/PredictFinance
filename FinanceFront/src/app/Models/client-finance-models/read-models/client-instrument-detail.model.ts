export interface ClientInstrumentDetail {
  Symbol: string;
  InstrumentSummary: { Instrument: { InstrumentId: string; Symbol: string; DisplayName: string; AssetType: string; Exchange: string; Currency: string; CountryCode: string | null }; PerimeterLabel: string; PeaDisplayLabel: string; Freshness: { AsOfUtc: string | null; DisplayLabel: string; IsStale: boolean }; HasPersistedAnalysis: boolean; AnalysisAvailabilityLabel: string; LatestAnalysisId: string | null; LatestSnapshotId: string | null };
  MarketReading: { OutcomeDisplayLabel: string; PrimaryPatternDisplayName: string | null; ConfidenceLabel: string | null; ValidationSummary: string; InvalidationLevel: number | null; RiskHint: string | null; PedagogicalSummary: string; Alternatives: Array<{ PatternId: string; DisplayName: string; ConfidenceLabel: string | null; ProgressStatusLabel: string | null }> };
  SupportReading: { AvailabilityDisplayLabel: string; ScoringVersion: string | null; ActiveUniverseId: string | null; PeaDisplayLabel: string; CoverageRatio: number | null; CompositeScore: number | null; MissingCategorySummaries: string[]; Notes: string[] };
  PersonalSituation: { HoldsInstrument: boolean; TotalQuantityHeld: number; AverageUnitCost: number | null; OpenLineCount: number | null; CurrencyCode: string; Recommendation: { DisplayLabel: string; ExplanationSummary: string; WarningText: string | null }; GuidanceSummary: string };
  NavigationLinks: { HistoryUrl: string; SimulationUrl: string; ComparisonUrl: string };
  LatestAnalysisId: string | null; LatestSnapshotId: string | null;
}
