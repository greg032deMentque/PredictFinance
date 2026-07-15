export interface IFundamentalScoreRequest {
  UniverseId: string;
  Symbols: string[];
  MinCategoriesRequired?: number;
  CoveragePenaltyEnabled?: boolean;
  IncludeRankPosition?: boolean;
}

export interface IFundamentalScoreResult {
  Symbol: string;
  DisplayName: string;
  UsableScore: boolean;
  TotalScore: number | null;
  CategoriesPresent: number;
  CategoryCoverage: number;
  ProfitabilityScore: number | null;
  LiquidityScore: number | null;
  DebtScore: number | null;
  ValuationScore: number | null;
  DividendScore: number | null;
  GrowthScore: number | null;
  PercentileGroupLabel: string;
  UsedGlobalUniverseFallback: boolean;
  MissingMetrics: string[];
  RankPosition: number | null;
  UniverseSize: number | null;
  Notes: string[];
  PeaEligibilityStatus: string;
  PeaEligibilitySourceType: string;
  PeaEligibilitySourceReference: string;
  PeaEligibilityCheckedUtc: string | null;
  PeaEligibilityPolicyVersion: string;
  PeaEligibilityReviewerNote: string;
}

export interface IFundamentalScoreResponse {
  UniverseId: string;
  ScoringVersion: string;
  EligibilityPolicyVersion: string;
  ProviderId: string;
  AsOfUtc: string;
  AsOfUtcSemantics: string;
  DataSnapshotId: string;
  Results: IFundamentalScoreResult[];
}
