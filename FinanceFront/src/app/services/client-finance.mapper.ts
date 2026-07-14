import { Injectable } from '@angular/core';
import {
  ClientAnalysisDetail,
  ClientAnalysisResult,
  ClientDashboardOverview,
  ClientHistoryFeed,
  ClientHistoryPage,
  ClientInstrumentDetail,
  ClientInstrumentHistory,
  ClientInstrumentHistoryPage,
  ClientLiveQuote,
  ClientPortfolio,
  ClientSimulationResult,
  ClientTransactionItem,
  ClientWatchlistItem,
  MarketAssetOption
} from '../Models/client-finance-models/client-finance-models';
import type {
  ClientModelStatusCode,
  ClientPatternCode,
  ClientRecommendationActionCode,
  ClientRiskLevelCode
} from '../Models/client-finance-models/client-domain-metadata';

/**
 * Traduit les payloads bruts de l'API ClientFinance vers les modèles applicatifs typés.
 * Logique pure et déterministe, isolée des appels HTTP pour être testable indépendamment.
 */
@Injectable({ providedIn: 'root' })
export class ClientFinanceMapper {

  mapOverview(payload: Record<string, unknown>): ClientDashboardOverview {
    return new ClientDashboardOverview({
      TotalPortfolioValue: this.readNumber(payload, ['totalPortfolioValue', 'TotalPortfolioValue']) ?? 0,
      DayProfitLoss: this.readNumber(payload, ['dayProfitLoss', 'DayProfitLoss']) ?? 0,
      OpenPositions: this.readNumber(payload, ['openPositions', 'OpenPositions']) ?? 0,
      AnalysesThisWeek: this.readNumber(payload, ['analysesThisWeek', 'AnalysesThisWeek']) ?? 0,
      WatchlistCount: this.readNumber(payload, ['watchlistCount', 'WatchlistCount']) ?? 0,
      RecommendationWinRate: this.readNumber(payload, ['recommendationWinRate', 'RecommendationWinRate']) ?? 0,
      NextMarketOpenAt: this.readString(payload, ['nextMarketOpenAt', 'NextMarketOpenAt']) ?? '',
      TotalInvested: this.readNumber(payload, ['totalInvested', 'TotalInvested']) ?? 0,
      TotalOutstanding: this.readNumber(payload, ['totalOutstanding', 'TotalOutstanding']) ?? 0
    });
  }

  mapAsset(source: unknown): MarketAssetOption {
    const payload = this.toRecord(source);

    return new MarketAssetOption({
      Symbol: this.readString(payload, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(payload, ['companyName', 'CompanyName']) ?? '',
      Market: this.readString(payload, ['market', 'Market']) ?? '',
      Currency: this.readString(payload, ['currency', 'Currency']) ?? 'USD',
      LastPrice: this.readNumber(payload, ['lastPrice', 'LastPrice']) ?? 0,
      DayVariationPct: this.readNumber(payload, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      Isin: this.readString(payload, ['isin', 'Isin']),
      IsPeaEligible: this.readBoolean(payload, ['isPeaEligible', 'IsPeaEligible']) ?? false
    });
  }

  mapWatchlistItem(source: unknown): ClientWatchlistItem {
    const payload = this.toRecord(source);

    return new ClientWatchlistItem({
      UserAssetId: this.readString(payload, ['userAssetId', 'UserAssetId']) ?? '',
      Symbol: this.readString(payload, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(payload, ['companyName', 'CompanyName']) ?? '',
      Market: this.readString(payload, ['market', 'Market']) ?? '',
      LastPrice: this.readNumber(payload, ['lastPrice', 'LastPrice']) ?? 0,
      DayVariationPct: this.readNumber(payload, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      HeldQuantity: this.readNumber(payload, ['heldQuantity', 'HeldQuantity']) ?? 0,
      AverageBuyPrice: this.readNumber(payload, ['averageBuyPrice', 'AverageBuyPrice']) ?? 0,
      InvestedAmount: this.readNumber(payload, ['investedAmount', 'InvestedAmount']) ?? 0,
      OutstandingAmount: this.readNumber(payload, ['outstandingAmount', 'OutstandingAmount']) ?? 0
    });
  }

  mapQuote(source: Record<string, unknown>): ClientLiveQuote {
    return new ClientLiveQuote({
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      LastPrice: this.readNumber(source, ['lastPrice', 'LastPrice']) ?? 0,
      DayVariationPct: this.readNumber(source, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      AsOfUtc: this.readString(source, ['asOfUtc', 'AsOfUtc']) ?? ''
    });
  }

  mapTransaction(source: unknown): ClientTransactionItem {
    const payload = this.toRecord(source);

    return new ClientTransactionItem({
      Id: this.readString(payload, ['id', 'Id']) ?? '',
      Symbol: this.readString(payload, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(payload, ['companyName', 'CompanyName']) ?? '',
      TransactionType: this.readString(payload, ['transactionType', 'TransactionType']) ?? '',
      Quantity: this.readNumber(payload, ['quantity', 'Quantity']) ?? 0,
      UnitPrice: this.readNumber(payload, ['unitPrice', 'UnitPrice']) ?? 0,
      Fees: this.readNumber(payload, ['fees', 'Fees']) ?? 0,
      GrossAmount: this.readNumber(payload, ['grossAmount', 'GrossAmount']) ?? 0,
      NetAmount: this.readNumber(payload, ['netAmount', 'NetAmount']) ?? 0,
      TimestampUtc: this.readString(payload, ['timestampUtc', 'TimestampUtc']) ?? '',
      PortfolioId: this.readString(payload, ['portfolioId', 'PortfolioId']) ?? '',
      PortfolioName: this.readString(payload, ['portfolioName', 'PortfolioName']) ?? ''
    });
  }

  mapAnalysis(source: unknown): ClientAnalysisResult {
    const payload = this.toRecord(source);

    return new ClientAnalysisResult({
      Id: this.readString(payload, ['id', 'Id']) ?? '',
      Symbol: this.readString(payload, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(payload, ['companyName', 'CompanyName']) ?? '',
      Pattern: this.readString(payload, ['pattern', 'Pattern']) as ClientPatternCode,
      Phase: this.readString(payload, ['phase', 'Phase']) ?? '',
      Probability: this.readNumber(payload, ['probability', 'Probability']) ?? 0,
      RecommendationAction: this.readString(payload, ['recommendationAction', 'RecommendationAction']) as ClientRecommendationActionCode,
      RecommendationReason: this.readString(payload, ['recommendationReason', 'RecommendationReason']) ?? '',
      RiskLevel: this.readString(payload, ['riskLevel', 'RiskLevel']) as ClientRiskLevelCode,
      RecommendationHorizonDays: this.readNumber(payload, ['recommendationHorizonDays', 'RecommendationHorizonDays']) ?? 0,
      PredictedAt: this.readString(payload, ['predictedAt', 'PredictedAt']) ?? '',
      IsActionable: this.readBoolean(payload, ['isActionable', 'IsActionable']) ?? false,
      ModelStatus: this.readString(payload, ['modelStatus', 'ModelStatus']) as ClientModelStatusCode,
      ModelMessage: this.readString(payload, ['modelMessage', 'ModelMessage']) ?? '',
      CurrentPrice: this.readNumber(payload, ['currentPrice', 'CurrentPrice']) ?? 0,
      NecklinePrice: this.readNumber(payload, ['necklinePrice', 'NecklinePrice']),
      TargetPrice: this.readNumber(payload, ['targetPrice', 'TargetPrice']),
      InvalidationPrice: this.readNumber(payload, ['invalidationPrice', 'InvalidationPrice'])
    });
  }

  mapSimulation(source: Record<string, unknown>): ClientSimulationResult {
    return new ClientSimulationResult({
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      Pattern: this.readString(source, ['pattern', 'Pattern']) as ClientPatternCode,
      Phase: this.readString(source, ['phase', 'Phase']) ?? '',
      InvestmentAmount: this.readNumber(source, ['investmentAmount', 'InvestmentAmount']) ?? 0,
      HorizonDays: this.readNumber(source, ['horizonDays', 'HorizonDays']) ?? 0,
      EstimatedReturnAmount: this.readNumber(source, ['estimatedReturnAmount', 'EstimatedReturnAmount']) ?? 0,
      EstimatedReturnPct: this.readNumber(source, ['estimatedReturnPct', 'EstimatedReturnPct']) ?? 0,
      EstimatedFinalAmount: this.readNumber(source, ['estimatedFinalAmount', 'EstimatedFinalAmount']) ?? 0,
      Assumption: this.readString(source, ['assumption', 'Assumption']) ?? '',
      CurrentPrice: this.readNumber(source, ['currentPrice', 'CurrentPrice']) ?? 0,
      Probability: this.readNumber(source, ['probability', 'Probability']) ?? 0,
      RecommendationAction: this.readString(source, ['recommendationAction', 'RecommendationAction']) as ClientRecommendationActionCode,
      RecommendationReason: this.readString(source, ['recommendationReason', 'RecommendationReason']) ?? '',
      RiskLevel: this.readString(source, ['riskLevel', 'RiskLevel']) as ClientRiskLevelCode,
      TargetPrice: this.readNumber(source, ['targetPrice', 'TargetPrice']),
      InvalidationPrice: this.readNumber(source, ['invalidationPrice', 'InvalidationPrice']),
      IsActionable: this.readBoolean(source, ['isActionable', 'IsActionable']) ?? false
    });
  }

  mapPortfolio(source: Record<string, unknown>): ClientPortfolio {
    const positions = this.readArray(source, ['positions', 'Positions']).map((item) => {
      const payload = this.toRecord(item);
      const instrument = this.toRecord(payload['instrument'] ?? payload['Instrument']);
      const marketReading = this.toRecord(payload['marketReading'] ?? payload['MarketReading']);
      const supportReading = this.toRecord(payload['supportReading'] ?? payload['SupportReading']);
      const recommendation = this.toRecord(payload['recommendation'] ?? payload['Recommendation']);
      return {
        UserAssetId: this.readString(payload, ['userAssetId', 'UserAssetId']) ?? '',
        Instrument: this.mapInstrumentIdentity(instrument),
        QuantityHeld: this.readNumber(payload, ['quantityHeld', 'QuantityHeld']) ?? 0,
        AverageCost: this.readNumber(payload, ['averageCost', 'AverageCost']) ?? 0,
        Fees: this.readNumber(payload, ['fees', 'Fees']) ?? 0,
        OutstandingAmount: this.readNumber(payload, ['outstandingAmount', 'OutstandingAmount']) ?? 0,
        MarketReading: {
          OutcomeDisplayLabel: this.readString(marketReading, ['outcomeDisplayLabel', 'OutcomeDisplayLabel']) ?? '',
          PrimaryPatternDisplayName: this.readString(marketReading, ['primaryPatternDisplayName', 'PrimaryPatternDisplayName']),
          ConfidenceLabel: this.readString(marketReading, ['confidenceLabel', 'ConfidenceLabel']),
          RiskHint: this.readString(marketReading, ['riskHint', 'RiskHint'])
        },
        SupportReading: {
          AvailabilityDisplayLabel: this.readString(supportReading, ['availabilityDisplayLabel', 'AvailabilityDisplayLabel']) ?? '',
          PeaDisplayLabel: this.readString(supportReading, ['peaDisplayLabel', 'PeaDisplayLabel']) ?? ''
        },
        Recommendation: this.mapRecommendationSummary(recommendation),
        RiskHint: this.readString(payload, ['riskHint', 'RiskHint']),
        HistoryEntryUrl: this.readString(payload, ['historyEntryUrl', 'HistoryEntryUrl']),
        SimulationUrl: this.readString(payload, ['simulationUrl', 'SimulationUrl'])
      };
    });
    return {
      Positions: positions,
      TotalInvestedAmount: this.readNumber(source, ['totalInvestedAmount', 'TotalInvestedAmount']) ?? 0,
      TotalOutstandingAmount: this.readNumber(source, ['totalOutstandingAmount', 'TotalOutstandingAmount']) ?? 0,
      OpenPositionCount: this.readNumber(source, ['openPositionCount', 'OpenPositionCount']) ?? positions.length
    };
  }

  mapHistoryFeed(source: Record<string, unknown>): ClientHistoryFeed {
    const items = this.readArray(source, ['items', 'Items']).map((item) => this.mapHistoryItem(this.toRecord(item)));
    return { Items: items, ReturnedCount: this.readNumber(source, ['returnedCount', 'ReturnedCount']) ?? items.length };
  }

  mapHistoryPage(source: Record<string, unknown>): ClientHistoryPage {
    const items = this.readArray(source, ['items', 'Items']).map((item) => this.mapHistoryItem(this.toRecord(item)));
    return {
      Items: items,
      Total: this.readNumber(source, ['total', 'Total']) ?? items.length,
      Page: this.readNumber(source, ['page', 'Page']) ?? 1,
      PageSize: this.readNumber(source, ['pageSize', 'PageSize']) ?? items.length
    };
  }

  mapInstrumentHistoryPage(source: Record<string, unknown>): ClientInstrumentHistoryPage {
    const instrument = this.toRecord(source['instrument'] ?? source['Instrument']);
    const items = this.readArray(source, ['items', 'Items']).map((item) => {
      const payload = this.toRecord(item);
      return {
        AnalysisId: this.readString(payload, ['analysisId', 'AnalysisId']) ?? '',
        SnapshotId: this.readString(payload, ['snapshotId', 'SnapshotId']) ?? '',
        TimestampUtc: this.readString(payload, ['timestampUtc', 'TimestampUtc']) ?? '',
        OutcomeDisplayLabel: this.readString(payload, ['outcomeDisplayLabel', 'OutcomeDisplayLabel']) ?? '',
        PrimaryPatternLabel: this.readString(payload, ['primaryPatternLabel', 'PrimaryPatternLabel']),
        RecommendationSummary: this.readString(payload, ['recommendationSummary', 'RecommendationSummary']) ?? '',
        SupportAvailabilitySummary: this.readString(payload, ['supportAvailabilitySummary', 'SupportAvailabilitySummary']) ?? '',
        PeaSummary: this.readString(payload, ['peaSummary', 'PeaSummary']) ?? '',
        AnalysisEngineVersion: this.readString(payload, ['analysisEngineVersion', 'AnalysisEngineVersion']) ?? '',
        RecommendationPolicyVersion: this.readString(payload, ['recommendationPolicyVersion', 'RecommendationPolicyVersion']),
        ExplanationPolicyVersion: this.readString(payload, ['explanationPolicyVersion', 'ExplanationPolicyVersion']),
        DetailUrl: this.readString(payload, ['detailUrl', 'DetailUrl']) ?? '',
        ComparisonUrl: this.readString(payload, ['comparisonUrl', 'ComparisonUrl']) ?? ''
      };
    });
    return {
      Instrument: this.mapInstrumentIdentity(instrument),
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? this.readString(instrument, ['symbol', 'Symbol']) ?? '',
      Items: items,
      Total: this.readNumber(source, ['total', 'Total']) ?? items.length,
      Page: this.readNumber(source, ['page', 'Page']) ?? 1,
      PageSize: this.readNumber(source, ['pageSize', 'PageSize']) ?? items.length
    };
  }

  mapInstrumentHistory(source: Record<string, unknown>): ClientInstrumentHistory {
    const instrument = this.toRecord(source['instrument'] ?? source['Instrument']);
    const items = this.readArray(source, ['items', 'Items']).map((item) => {
      const payload = this.toRecord(item);
      return {
        AnalysisId: this.readString(payload, ['analysisId', 'AnalysisId']) ?? '',
        SnapshotId: this.readString(payload, ['snapshotId', 'SnapshotId']) ?? '',
        TimestampUtc: this.readString(payload, ['timestampUtc', 'TimestampUtc']) ?? '',
        OutcomeDisplayLabel: this.readString(payload, ['outcomeDisplayLabel', 'OutcomeDisplayLabel']) ?? '',
        PrimaryPatternLabel: this.readString(payload, ['primaryPatternLabel', 'PrimaryPatternLabel']),
        RecommendationSummary: this.readString(payload, ['recommendationSummary', 'RecommendationSummary']) ?? '',
        SupportAvailabilitySummary: this.readString(payload, ['supportAvailabilitySummary', 'SupportAvailabilitySummary']) ?? '',
        PeaSummary: this.readString(payload, ['peaSummary', 'PeaSummary']) ?? '',
        AnalysisEngineVersion: this.readString(payload, ['analysisEngineVersion', 'AnalysisEngineVersion']) ?? '',
        RecommendationPolicyVersion: this.readString(payload, ['recommendationPolicyVersion', 'RecommendationPolicyVersion']),
        ExplanationPolicyVersion: this.readString(payload, ['explanationPolicyVersion', 'ExplanationPolicyVersion']),
        DetailUrl: this.readString(payload, ['detailUrl', 'DetailUrl']) ?? '',
        ComparisonUrl: this.readString(payload, ['comparisonUrl', 'ComparisonUrl']) ?? ''
      };
    });
    return { Instrument: this.mapInstrumentIdentity(instrument), Symbol: this.readString(source, ['symbol', 'Symbol']) ?? this.readString(instrument, ['symbol', 'Symbol']) ?? '', Items: items, ReturnedCount: this.readNumber(source, ['returnedCount', 'ReturnedCount']) ?? items.length };
  }

  mapAnalysisDetail(source: Record<string, unknown>): ClientAnalysisDetail {
    const instrument = this.toRecord(source['instrument'] ?? source['Instrument']);
    const confidenceBreakdownRaw = this.toRecord(source['confidenceBreakdown'] ?? source['ConfidenceBreakdown']);
    const actionPlanRaw = this.toRecord(source['actionPlan'] ?? source['ActionPlan']);
    const exPostRaw = this.toRecord(source['exPostEvaluation'] ?? source['ExPostEvaluation']);
    return {
      AnalysisId: this.readString(source, ['analysisId', 'AnalysisId']) ?? '',
      Instrument: this.mapInstrumentIdentity(instrument),
      GeneratedAtUtc: this.readString(source, ['generatedAtUtc', 'GeneratedAtUtc']) ?? '',
      OutcomeDisplayLabel: this.readString(source, ['outcomeDisplayLabel', 'OutcomeDisplayLabel']) ?? '',
      MarketReading: this.mapDetailedMarketReading(this.toRecord(source['marketReading'] ?? source['MarketReading'])),
      SupportReading: this.mapDetailedSupportReading(this.toRecord(source['supportReading'] ?? source['SupportReading'])),
      Recommendation: this.mapRecommendationSummary(this.toRecord(source['recommendation'] ?? source['Recommendation'])),
      WhyRecommendation: this.readString(source, ['whyRecommendation', 'WhyRecommendation']) ?? '',
      PedagogicalSummary: this.readString(source, ['pedagogicalSummary', 'PedagogicalSummary']) ?? '',
      SnapshotId: this.readString(source, ['snapshotId', 'SnapshotId']) ?? '',
      HistoryRoute: this.readString(source, ['historyRoute', 'HistoryRoute']) ?? '',
      CompactSummary: this.readString(source, ['compactSummary', 'CompactSummary']) ?? '',
      ModelMessage: this.readString(source, ['modelMessage', 'ModelMessage']) ?? '',
      ConfidenceBreakdown: {
        Level: this.readString(confidenceBreakdownRaw, ['level', 'Level']) ?? '',
        Criteria: this.readArray(confidenceBreakdownRaw, ['criteria', 'Criteria']).map(item => {
          const p = this.toRecord(item);
          return { Code: this.readString(p, ['code', 'Code']) ?? '', Label: this.readString(p, ['label', 'Label']) ?? '', State: this.readString(p, ['state', 'State']) ?? '', Source: this.readString(p, ['source', 'Source']) ?? '' };
        })
      },
      ActionPlan: {
        HoldingStatus: this.readString(actionPlanRaw, ['holdingStatus', 'HoldingStatus']) ?? '',
        PolicyVersion: this.readString(actionPlanRaw, ['policyVersion', 'PolicyVersion']) ?? '',
        Steps: this.readArray(actionPlanRaw, ['steps', 'Steps']).map(item => {
          const p = this.toRecord(item);
          return { Kind: this.readString(p, ['kind', 'Kind']) ?? '', Label: this.readString(p, ['label', 'Label']) ?? '', Source: this.readString(p, ['source', 'Source']) ?? '', Value: this.readString(p, ['value', 'Value']), AlertTrigger: this.readString(p, ['alertTrigger', 'AlertTrigger']) };
        })
      },
      ExPostEvaluation: {
        Status: this.readString(exPostRaw, ['status', 'Status']) ?? '',
        StatusLabel: this.readString(exPostRaw, ['statusLabel', 'StatusLabel']) ?? '',
        ReviewScheduledAtUtc: this.readString(exPostRaw, ['reviewScheduledAtUtc', 'ReviewScheduledAtUtc']),
        PriceAtReview: this.readNumber(exPostRaw, ['priceAtReview', 'PriceAtReview']),
        TargetPrice: this.readNumber(exPostRaw, ['targetPrice', 'TargetPrice']),
        InvalidationPrice: this.readNumber(exPostRaw, ['invalidationPrice', 'InvalidationPrice']),
        PedagogicalNote: this.readString(exPostRaw, ['pedagogicalNote', 'PedagogicalNote'])
      }
    };
  }

  mapInstrumentDetail(source: Record<string, unknown>): ClientInstrumentDetail {
    const instrumentSummary = this.toRecord(source['instrumentSummary'] ?? source['InstrumentSummary']);
    const identity = this.toRecord(instrumentSummary['instrument'] ?? instrumentSummary['Instrument']);
    const freshness = this.toRecord(instrumentSummary['freshness'] ?? instrumentSummary['Freshness']);
    const personalSituation = this.toRecord(source['personalSituation'] ?? source['PersonalSituation']);
    return {
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      InstrumentSummary: {
        Instrument: this.mapInstrumentIdentity(identity),
        PerimeterLabel: this.readString(instrumentSummary, ['perimeterLabel', 'PerimeterLabel']) ?? '',
        PeaDisplayLabel: this.readString(instrumentSummary, ['peaDisplayLabel', 'PeaDisplayLabel']) ?? '',
        Freshness: { AsOfUtc: this.readString(freshness, ['asOfUtc', 'AsOfUtc']), DisplayLabel: this.readString(freshness, ['displayLabel', 'DisplayLabel']) ?? '', IsStale: this.readBoolean(freshness, ['isStale', 'IsStale']) ?? false },
        HasPersistedAnalysis: this.readBoolean(instrumentSummary, ['hasPersistedAnalysis', 'HasPersistedAnalysis']) ?? false,
        AnalysisAvailabilityLabel: this.readString(instrumentSummary, ['analysisAvailabilityLabel', 'AnalysisAvailabilityLabel']) ?? '',
        LatestAnalysisId: this.readString(instrumentSummary, ['latestAnalysisId', 'LatestAnalysisId']),
        LatestSnapshotId: this.readString(instrumentSummary, ['latestSnapshotId', 'LatestSnapshotId'])
      },
      MarketReading: this.mapDetailedMarketReading(this.toRecord(source['marketReading'] ?? source['MarketReading'])),
      SupportReading: this.mapDetailedSupportReading(this.toRecord(source['supportReading'] ?? source['SupportReading'])),
      PersonalSituation: {
        HoldsInstrument: this.readBoolean(personalSituation, ['holdsInstrument', 'HoldsInstrument']) ?? false,
        TotalQuantityHeld: this.readNumber(personalSituation, ['totalQuantityHeld', 'TotalQuantityHeld']) ?? 0,
        AverageUnitCost: this.readNumber(personalSituation, ['averageUnitCost', 'AverageUnitCost']),
        OpenLineCount: this.readNumber(personalSituation, ['openLineCount', 'OpenLineCount']),
        CurrencyCode: this.readString(personalSituation, ['currencyCode', 'CurrencyCode']) ?? '',
        Recommendation: this.mapRecommendationSummary(this.toRecord(personalSituation['recommendation'] ?? personalSituation['Recommendation'])),
        GuidanceSummary: this.readString(personalSituation, ['guidanceSummary', 'GuidanceSummary']) ?? ''
      },
      NavigationLinks: {
        HistoryUrl: this.readString(this.toRecord(source['navigationLinks'] ?? source['NavigationLinks']), ['historyUrl', 'HistoryUrl']) ?? '',
        SimulationUrl: this.readString(this.toRecord(source['navigationLinks'] ?? source['NavigationLinks']), ['simulationUrl', 'SimulationUrl']) ?? '',
        ComparisonUrl: this.readString(this.toRecord(source['navigationLinks'] ?? source['NavigationLinks']), ['comparisonUrl', 'ComparisonUrl']) ?? ''
      },
      LatestAnalysisId: this.readString(source, ['latestAnalysisId', 'LatestAnalysisId']),
      LatestSnapshotId: this.readString(source, ['latestSnapshotId', 'LatestSnapshotId'])
    };
  }

  private mapHistoryItem(source: Record<string, unknown>) {
    const instrument = this.toRecord(source['instrument'] ?? source['Instrument']);
    return {
      AnalysisId: this.readString(source, ['analysisId', 'AnalysisId']) ?? '',
      SnapshotId: this.readString(source, ['snapshotId', 'SnapshotId']) ?? '',
      Instrument: this.mapInstrumentIdentity(instrument),
      TimestampUtc: this.readString(source, ['timestampUtc', 'TimestampUtc']) ?? '',
      OutcomeDisplayLabel: this.readString(source, ['outcomeDisplayLabel', 'OutcomeDisplayLabel']) ?? '',
      PrimaryPatternLabel: this.readString(source, ['primaryPatternLabel', 'PrimaryPatternLabel']),
      RecommendationSummary: this.readString(source, ['recommendationSummary', 'RecommendationSummary']) ?? '',
      SupportAvailabilitySummary: this.readString(source, ['supportAvailabilitySummary', 'SupportAvailabilitySummary']) ?? '',
      PeaSummary: this.readString(source, ['peaSummary', 'PeaSummary']) ?? '',
      AnalysisEngineVersion: this.readString(source, ['analysisEngineVersion', 'AnalysisEngineVersion']) ?? '',
      RecommendationPolicyVersion: this.readString(source, ['recommendationPolicyVersion', 'RecommendationPolicyVersion']),
      ExplanationPolicyVersion: this.readString(source, ['explanationPolicyVersion', 'ExplanationPolicyVersion']),
      DetailUrl: this.readString(source, ['detailUrl', 'DetailUrl']) ?? '',
      HistoryUrl: this.readString(source, ['historyUrl', 'HistoryUrl']) ?? '',
      ComparisonUrl: this.readString(source, ['comparisonUrl', 'ComparisonUrl']) ?? ''
    };
  }

  private mapDetailedMarketReading(source: Record<string, unknown>) {
    const alternatives = this.readArray(source, ['alternatives', 'Alternatives']).map((item) => {
      const payload = this.toRecord(item);
      return { PatternId: this.readString(payload, ['patternId', 'PatternId']) ?? '', DisplayName: this.readString(payload, ['displayName', 'DisplayName']) ?? '', ConfidenceLabel: this.readString(payload, ['confidenceLabel', 'ConfidenceLabel']), ProgressStatusLabel: this.readString(payload, ['progressStatusLabel', 'ProgressStatusLabel']) };
    });
    return { OutcomeDisplayLabel: this.readString(source, ['outcomeDisplayLabel', 'OutcomeDisplayLabel']) ?? '', PrimaryPatternDisplayName: this.readString(source, ['primaryPatternDisplayName', 'PrimaryPatternDisplayName']), ConfidenceLabel: this.readString(source, ['confidenceLabel', 'ConfidenceLabel']), ValidationSummary: this.readString(source, ['validationSummary', 'ValidationSummary']) ?? '', InvalidationLevel: this.readNumber(source, ['invalidationLevel', 'InvalidationLevel']), RiskHint: this.readString(source, ['riskHint', 'RiskHint']), PedagogicalSummary: this.readString(source, ['pedagogicalSummary', 'PedagogicalSummary']) ?? '', Alternatives: alternatives };
  }

  private mapDetailedSupportReading(source: Record<string, unknown>) {
    return { AvailabilityDisplayLabel: this.readString(source, ['availabilityDisplayLabel', 'AvailabilityDisplayLabel']) ?? '', ScoringVersion: this.readString(source, ['scoringVersion', 'ScoringVersion']), ActiveUniverseId: this.readString(source, ['activeUniverseId', 'ActiveUniverseId']), PeaDisplayLabel: this.readString(source, ['peaDisplayLabel', 'PeaDisplayLabel']) ?? '', CoverageRatio: this.readNumber(source, ['coverageRatio', 'CoverageRatio']), CompositeScore: this.readNumber(source, ['compositeScore', 'CompositeScore']), MissingCategorySummaries: this.readStringArray(source, ['missingCategorySummaries', 'MissingCategorySummaries']), Notes: this.readStringArray(source, ['notes', 'Notes']) };
  }

  private mapRecommendationSummary(source: Record<string, unknown>) {
    return { DisplayLabel: this.readString(source, ['displayLabel', 'DisplayLabel']) ?? '', ExplanationSummary: this.readString(source, ['explanationSummary', 'ExplanationSummary']) ?? '', WarningText: this.readString(source, ['warningText', 'WarningText']) };
  }

  private mapInstrumentIdentity(source: Record<string, unknown>) {
    return { InstrumentId: this.readString(source, ['instrumentId', 'InstrumentId']) ?? '', Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '', DisplayName: this.readString(source, ['displayName', 'DisplayName']) ?? '', AssetType: this.readString(source, ['assetType', 'AssetType']) ?? '', Exchange: this.readString(source, ['exchange', 'Exchange']) ?? '', Currency: this.readString(source, ['currency', 'Currency']) ?? '', CountryCode: this.readString(source, ['countryCode', 'CountryCode']) };
  }

  private readArray(source: Record<string, unknown>, keys: string[]): unknown[] {
    for (const key of keys) {
      const value = source[key];
      if (Array.isArray(value)) return value;
    }
    return [];
  }

  private readStringArray(source: Record<string, unknown>, keys: string[]): string[] {
    return this.readArray(source, keys).map((item) => typeof item === 'string' ? item.trim() : '').filter((item) => item.length > 0);
  }

  private readBoolean(source: Record<string, unknown>, keys: string[]): boolean | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'boolean') {
        return value;
      }

      if (typeof value === 'string') {
        const normalized = value.trim().toLowerCase();
        if (normalized === 'true') {
          return true;
        }

        if (normalized === 'false') {
          return false;
        }
      }
    }

    return null;
  }

  private readString(source: Record<string, unknown>, keys: string[]): string | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value.trim();
      }
    }

    return null;
  }

  private readNumber(source: Record<string, unknown>, keys: string[]): number | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'number' && Number.isFinite(value)) {
        return value;
      }

      if (typeof value === 'string') {
        const parsed = Number(value);
        if (Number.isFinite(parsed)) {
          return parsed;
        }
      }
    }

    return null;
  }

  private toRecord(value: unknown): Record<string, unknown> {
    if (value && typeof value === 'object') {
      return value as Record<string, unknown>;
    }

    return {};
  }
}
