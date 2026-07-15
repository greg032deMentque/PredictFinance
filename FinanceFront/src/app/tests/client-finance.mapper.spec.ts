import { ClientFinanceMapper } from '../services/client-finance.mapper';
import { ClientDashboardOverview, ClientWatchlistItem } from '../Models/client-finance-models/client-finance-models';

describe('ClientFinanceMapper', () => {
  const createSut = () => new ClientFinanceMapper();

  describe('readString / readNumber / readBoolean (lecture tolérante camelCase/PascalCase)', () => {
    it('readString should prefer the first non-empty key and trim whitespace', () => {
      const mapper = createSut();

      expect(mapper.readString({ symbol: '  AAPL  ' }, ['Symbol', 'symbol'])).toBe('AAPL');
      expect(mapper.readString({ Symbol: 'MSFT' }, ['Symbol', 'symbol'])).toBe('MSFT');
    });

    it('readString should skip blank values and fall back to null when nothing matches', () => {
      const mapper = createSut();

      expect(mapper.readString({ Symbol: '   ' }, ['Symbol', 'symbol'])).toBeNull();
      expect(mapper.readString({}, ['Symbol', 'symbol'])).toBeNull();
    });
  });

  describe('mapOverview', () => {
    it('should map a camelCase payload with all fields present', () => {
      const mapper = createSut();

      const result = mapper.mapOverview({
        totalPortfolioValue: 1000,
        dayProfitLoss: -12.5,
        openPositions: 3,
        analysesThisWeek: 2,
        watchlistCount: 5,
        nextMarketOpenAt: '2026-07-14T09:00:00Z',
        totalInvested: 900,
        totalOutstanding: 950
      });

      expect(result).toEqual(new ClientDashboardOverview({
        TotalPortfolioValue: 1000,
        DayProfitLoss: -12.5,
        OpenPositions: 3,
        AnalysesThisWeek: 2,
        WatchlistCount: 5,
        NextMarketOpenAt: '2026-07-14T09:00:00Z',
        TotalInvested: 900,
        TotalOutstanding: 950
      }));
    });

    it('should default every numeric/string field when the payload is empty', () => {
      const mapper = createSut();

      expect(mapper.mapOverview({})).toEqual(new ClientDashboardOverview());
    });

    it('should prefer PascalCase keys over camelCase when both are present', () => {
      const mapper = createSut();

      const result = mapper.mapOverview({
        totalPortfolioValue: 1,
        TotalPortfolioValue: 2
      });

      expect(result.TotalPortfolioValue).toBe(1);
    });
  });

  describe('mapAsset', () => {
    it('should map a well-formed source and default Currency to USD when absent', () => {
      const mapper = createSut();

      const result = mapper.mapAsset({ Symbol: 'AAPL', CompanyName: 'Apple', LastPrice: '187.5' });

      expect(result.Symbol).toBe('AAPL');
      expect(result.Currency).toBe('USD');
      expect(result.LastPrice).toBe(187.5);
      expect(result.Isin).toBeNull();
    });

    it('should tolerate a non-object source and return an empty-shaped asset', () => {
      const mapper = createSut();

      const result = mapper.mapAsset(null);

      expect(result.Symbol).toBe('');
      expect(result.IsPeaEligible).toBeFalse();
    });
  });

  describe('mapWatchlistItem', () => {
    it('should map nested MarketReading/Recommendation/Freshness and default missing sub-objects', () => {
      const mapper = createSut();

      const result = mapper.mapWatchlistItem({
        UserAssetId: 'ua-1',
        Symbol: 'MSFT',
        Recommendation: { DisplayLabel: 'Conserver', ExplanationSummary: 'Tendance stable' }
      });

      expect(result).toEqual(jasmine.objectContaining({
        UserAssetId: 'ua-1',
        Symbol: 'MSFT',
        MarketReading: {
          OutcomeDisplayLabel: '',
          PrimaryPatternDisplayName: null,
          ConfidenceLabel: null,
          RiskHint: null
        },
        Recommendation: {
          DisplayLabel: 'Conserver',
          ExplanationSummary: 'Tendance stable',
          WarningText: null
        },
        Freshness: {
          Status: 'Missing',
          DisplayLabel: '',
          CheckedAtUtc: null
        }
      }));
      expect(result instanceof ClientWatchlistItem).toBeTrue();
    });
  });

  describe('mapAnalysis (readEnumCode)', () => {
    it('should resolve RecommendationAction/RiskLevel/ModelStatus from string codes', () => {
      const mapper = createSut();

      const result = mapper.mapAnalysis({
        RecommendationAction: 'Buy',
        RiskLevel: 'High',
        ModelStatus: 'Go'
      });

      expect(result.RecommendationAction).toBe('Buy');
      expect(result.RiskLevel).toBe('High');
      expect(result.ModelStatus).toBe('Go');
    });

    it('should resolve RecommendationAction/RiskLevel/ModelStatus from legacy integer codes', () => {
      const mapper = createSut();

      const result = mapper.mapAnalysis({
        RecommendationAction: 1,
        RiskLevel: 3,
        ModelStatus: 0
      });

      expect(result.RecommendationAction).toBe('Sell');
      expect(result.RiskLevel).toBe('High');
      expect(result.ModelStatus).toBe('NoGo');
    });

    it('should return an empty string when the code is neither a known string nor a mapped integer', () => {
      const mapper = createSut();

      const result = mapper.mapAnalysis({ RecommendationAction: 99 });

      expect(result.RecommendationAction).toBe('');
    });
  });

  describe('mapAnalysisDossier', () => {
    it('should return null for AnalysisWindow/RiskContext when their raw payload is absent', () => {
      const mapper = createSut();

      const result = mapper.mapAnalysisDossier({ Id: 'a1', Symbol: 'AAPL' });

      expect(result.AnalysisWindow).toBeNull();
      expect(result.RiskContext).toBeNull();
      expect(result.PriceSeries).toEqual([]);
      expect(result.MainPattern).toBeNull();
      expect(result.Outcome).toBe('NoCrediblePattern');
    });

    it('should map AnalysisWindow, PriceSeries and MainPattern when present', () => {
      const mapper = createSut();

      const result = mapper.mapAnalysisDossier({
        Id: 'a1',
        Symbol: 'AAPL',
        AnalysisWindow: { Interval: '1d', StartDate: '2026-01-01', EndDate: '2026-06-01', RequiredCandles: 100, ActualCandles: 98 },
        PriceSeries: [{ Timestamp: '2026-01-01', Open: 1, High: 2, Low: 0.5, Close: 1.5, Volume: 1000 }],
        MainPattern: { PatternId: 'p1', DisplayName: 'Head and shoulders', RecommendationAction: 0, RiskLevel: 1 }
      });

      expect(result.AnalysisWindow).toEqual({
        Interval: '1d',
        StartDate: '2026-01-01',
        EndDate: '2026-06-01',
        RequiredCandles: 100,
        ActualCandles: 98
      });
      expect(result.PriceSeries.length).toBe(1);
      expect(result.MainPattern?.PatternId).toBe('p1');
      expect(result.MainPattern?.RecommendationAction).toBe('Buy');
      expect(result.MainPattern?.RiskLevel).toBe('Low');
    });
  });

  describe('mapPortfolio', () => {
    it('should map positions with nested Instrument/MarketReading/Recommendation and null Allocation by default', () => {
      const mapper = createSut();

      const result = mapper.mapPortfolio({
        Positions: [{
          UserAssetId: 'ua-1',
          Instrument: { InstrumentId: 'i-1', Symbol: 'AAPL' },
          QuantityHeld: 10
        }]
      });

      expect(result.Allocation).toBeNull();
      expect(result.Positions.length).toBe(1);
      expect(result.Positions[0].Instrument.Symbol).toBe('AAPL');
      expect(result.Positions[0].QuantityHeld).toBe(10);
      expect(result.OpenPositionCount).toBe(1);
    });

    it('should keep an explicit OpenPositionCount instead of falling back to positions.length', () => {
      const mapper = createSut();

      const result = mapper.mapPortfolio({ Positions: [], OpenPositionCount: 4 });

      expect(result.OpenPositionCount).toBe(4);
    });

    it('should default DiversificationRating to Moderate when the raw value is not a known category', () => {
      const mapper = createSut();

      const result = mapper.mapPortfolio({
        Positions: [],
        Allocation: { DiversificationRating: 'Unknown' }
      });

      expect(result.Allocation?.DiversificationRating).toBe('Moderate');
    });
  });
});
