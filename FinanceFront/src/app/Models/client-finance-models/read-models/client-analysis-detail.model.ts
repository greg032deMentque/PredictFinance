export interface ClientAnalysisDetail {
  AnalysisId: string;
  Instrument: { InstrumentId: string; Symbol: string; DisplayName: string; AssetType: string; Exchange: string; Currency: string; CountryCode: string | null };
  GeneratedAtUtc: string;
  OutcomeDisplayLabel: string;
  MarketReading: {
    OutcomeDisplayLabel: string;
    PrimaryPatternDisplayName: string | null;
    ConfidenceLabel: string | null;
    ValidationSummary: string;
    InvalidationLevel: number | null;
    RiskHint: string | null;
    PedagogicalSummary: string;
    Alternatives: { PatternId: string; DisplayName: string; ConfidenceLabel: string | null; ProgressStatusLabel: string | null }[];
  };
  SupportReading: {
    AvailabilityDisplayLabel: string;
    ScoringVersion: string | null;
    ActiveUniverseId: string | null;
    PeaDisplayLabel: string;
    CoverageRatio: number | null;
    CompositeScore: number | null;
    MissingCategorySummaries: string[];
    Notes: string[];
  };
  Recommendation: { DisplayLabel: string; ExplanationSummary: string; WarningText: string | null };
  WhyRecommendation: string;
  PedagogicalSummary: string;
  SnapshotId: string;
  HistoryRoute: string;
  CompactSummary: string;
  ModelMessage: string;
  ConfidenceBreakdown: {
    Level: string;
    Criteria: {
      Code: string;
      Label: string;
      State: string;
      Source: string;
    }[];
  };
  ActionPlan: {
    HoldingStatus: string;
    PolicyVersion: string;
    Steps: {
      Kind: string;
      Label: string;
      Source: string;
      Value: string | null;
      AlertTrigger: string | null;
    }[];
  };
  ExPostEvaluation: {
    Status: string;
    StatusLabel: string;
    ReviewScheduledAtUtc: string | null;
    PriceAtReview: number | null;
    TargetPrice: number | null;
    InvalidationPrice: number | null;
    PedagogicalNote: string | null;
    DaysToOutcome: number | null;
    OutcomeDate: string | null;
  };
}
