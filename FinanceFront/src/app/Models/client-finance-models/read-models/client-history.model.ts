export interface ClientHistoryItem {
  AnalysisId: string; SnapshotId: string;
  Instrument: { InstrumentId: string; Symbol: string; DisplayName: string; AssetType: string; Exchange: string; Currency: string; CountryCode: string | null };
  TimestampUtc: string; OutcomeDisplayLabel: string; PrimaryPatternLabel: string | null; RecommendationSummary: string; SupportAvailabilitySummary: string; PeaSummary: string; AnalysisEngineVersion: string; RecommendationPolicyVersion: string | null; ExplanationPolicyVersion: string | null; DetailUrl: string; HistoryUrl: string; ComparisonUrl: string;
}

export interface ClientHistoryPage {
  Items: ClientHistoryItem[];
  Total: number;
  Page: number;
  PageSize: number;
}

export interface ClientInstrumentHistoryItem { AnalysisId: string; SnapshotId: string; TimestampUtc: string; OutcomeDisplayLabel: string; PrimaryPatternLabel: string | null; RecommendationSummary: string; SupportAvailabilitySummary: string; PeaSummary: string; AnalysisEngineVersion: string; RecommendationPolicyVersion: string | null; ExplanationPolicyVersion: string | null; DetailUrl: string; ComparisonUrl: string; }

export interface ClientInstrumentHistoryPage {
  Instrument: { InstrumentId: string; Symbol: string; DisplayName: string; AssetType: string; Exchange: string; Currency: string; CountryCode: string | null };
  Symbol: string;
  Items: ClientInstrumentHistoryItem[];
  Total: number;
  Page: number;
  PageSize: number;
}

export interface HistoryQueryOptions {
  page?: number;
  pageSize?: number;
  symbol?: string;
  recommendation?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface InstrumentHistoryQueryOptions {
  page?: number;
  pageSize?: number;
  sortDirection?: 'asc' | 'desc';
}
