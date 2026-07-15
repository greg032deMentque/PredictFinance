/** Statut de fraîcheur de l'analyse — sérialisé en chaîne par le backend. */
export type WatchlistFreshnessStatus = 'Fresh' | 'Aging' | 'Stale' | 'Missing';

export interface WatchlistMarketReading {
  OutcomeDisplayLabel: string;
  PrimaryPatternDisplayName: string | null;
  ConfidenceLabel: string | null;
  RiskHint: string | null;
}

export interface WatchlistRecommendation {
  /** Label affiché (ex. "Acheter", "Conserver", "Vendre") */
  DisplayLabel: string;
  ExplanationSummary: string;
  WarningText: string | null;
}

export interface WatchlistFreshness {
  /** Fresh | Aging | Stale | Missing */
  Status: WatchlistFreshnessStatus;
  DisplayLabel: string;
  CheckedAtUtc: string | null;
}

export class ClientWatchlistItem {
  UserAssetId = '';
  Symbol = '';
  CompanyName = '';
  Market = '';
  LastPrice = 0;
  DayVariationPct = 0;
  HeldQuantity = 0;
  AverageBuyPrice = 0;
  InvestedAmount = 0;
  OutstandingAmount = 0;
  LastAnalysisAtUtc: string | null = null;
  /** Propriété fournie directement par le back (WatchlistItemViewModel.HasPersistedAnalysis). */
  HasPersistedAnalysis = false;
  /** Prochaine date de résultats (WatchlistItemViewModel.NextEarningsDateUtc). */
  NextEarningsDateUtc: string | null = null;
  /** True si la fenêtre d'évaluation chevauche la prochaine date de résultats. */
  EarningsWithinHorizonWarning = false;

  MarketReading: WatchlistMarketReading = {
    OutcomeDisplayLabel: '',
    PrimaryPatternDisplayName: null,
    ConfidenceLabel: null,
    RiskHint: null
  };

  Recommendation: WatchlistRecommendation = {
    DisplayLabel: '',
    ExplanationSummary: '',
    WarningText: null
  };

  Freshness: WatchlistFreshness = {
    Status: 'Missing',
    DisplayLabel: '',
    CheckedAtUtc: null
  };

  constructor(init?: Partial<ClientWatchlistItem>) {
    Object.assign(this, init);
  }
}
