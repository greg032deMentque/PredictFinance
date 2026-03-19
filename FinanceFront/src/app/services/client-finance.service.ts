import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { ClientAnalysisLaunchRequest } from '../Models/client-finance-models/client-analysis-launch-request.model';
import { ClientAnalysisResult } from '../Models/client-finance-models/client-analysis-result.model';
import { ClientDashboardOverview } from '../Models/client-finance-models/client-dashboard-overview.model';
import { ClientLiveQuote } from '../Models/client-finance-models/client-live-quote.model';
import { ClientSimulationRequest } from '../Models/client-finance-models/client-simulation-request.model';
import { ClientSimulationResult } from '../Models/client-finance-models/client-simulation-result.model';
import { ClientTransactionCreateRequest } from '../Models/client-finance-models/client-transaction-create-request.model';
import { ClientTransactionItem } from '../Models/client-finance-models/client-transaction-item.model';
import { ClientWatchlistItem } from '../Models/client-finance-models/client-watchlist-item.model';
import { MarketAssetOption } from '../Models/client-finance-models/market-asset-option.model';


@Injectable({ providedIn: 'root' })
export class ClientFinanceService {

  constructor(private readonly http: HttpClient) {}

  getDashboardOverview(): Observable<ClientDashboardOverview> {
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/dashboard`)
      .pipe(map((payload) => this.mapOverview(payload)));
  }

  searchAssets(query: string): Observable<MarketAssetOption[]> {
    const normalizedQuery = query.trim();
    if (normalizedQuery.length < 1) {
      return of([]);
    }

    const params = new HttpParams().set('query', normalizedQuery);

    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/assets/search`, { params })
      .pipe(map((items) => items.map((item) => this.mapAsset(item))));
  }

  getWatchlist(): Observable<ClientWatchlistItem[]> {
    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/watchlist`)
      .pipe(map((items) => items.map((item) => this.mapWatchlistItem(this.toRecord(item)))));
  }

  addToWatchlist(symbol: string, companyName: string, market: string): Observable<ClientWatchlistItem> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/watchlist`, {
        Symbol: symbol,
        CompanyName: companyName,
        Market: market
      })
      .pipe(map((item) => this.mapWatchlistItem(item)));
  }

  removeFromWatchlist(symbol: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}ClientFinance/watchlist/${encodeURIComponent(symbol)}`);
  }

  getLiveQuote(symbol: string): Observable<ClientLiveQuote> {
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/quote/${encodeURIComponent(symbol)}`)
      .pipe(map((payload) => this.mapQuote(payload)));
  }

  registerTransaction(request: ClientTransactionCreateRequest): Observable<ClientTransactionItem> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/transactions`, {
        Symbol: request.Symbol,
        TransactionType: request.TransactionType,
        Quantity: request.Quantity,
        UnitPrice: request.UnitPrice,
        Fees: request.Fees,
        TimestampUtc: request.TimestampUtc
      })
      .pipe(map((payload) => this.mapTransaction(payload)));
  }

  getTransactions(take = 100): Observable<ClientTransactionItem[]> {
    const params = new HttpParams().set('take', take);

    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/transactions`, { params })
      .pipe(map((items) => items.map((item) => this.mapTransaction(this.toRecord(item)))));
  }

  deleteTransaction(transactionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}ClientFinance/transactions/${encodeURIComponent(transactionId)}`);
  }

  runAnalysis(request: ClientAnalysisLaunchRequest): Observable<ClientAnalysisResult> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/analysis/run`, {
        Symbol: request.Symbol,
        RequestedPattern: request.RequestedPattern
      })
      .pipe(map((payload) => this.mapAnalysis(payload)));
  }

  getRecentAnalyses(limit = 8): Observable<ClientAnalysisResult[]> {
    const params = new HttpParams().set('take', limit);

    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/analysis/recent`, { params })
      .pipe(map((items) => items.map((item) => this.mapAnalysis(this.toRecord(item)))));
  }

  runSimulation(request: ClientSimulationRequest): Observable<ClientSimulationResult> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/simulation/run`, {
        Symbol: request.Symbol,
        Pattern: request.Pattern,
        InvestmentAmount: request.InvestmentAmount,
        HorizonDays: request.HorizonDays
      })
      .pipe(map((payload) => this.mapSimulation(payload)));
  }

  private mapOverview(payload: Record<string, unknown>): ClientDashboardOverview {
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

  private mapAsset(source: unknown): MarketAssetOption {
    const payload = this.toRecord(source);

    return new MarketAssetOption({
      Symbol: this.readString(payload, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(payload, ['companyName', 'CompanyName']) ?? '',
      Market: this.readString(payload, ['market', 'Market']) ?? '',
      Currency: this.readString(payload, ['currency', 'Currency']) ?? 'USD',
      LastPrice: this.readNumber(payload, ['lastPrice', 'LastPrice']) ?? 0,
      DayVariationPct: this.readNumber(payload, ['dayVariationPct', 'DayVariationPct']) ?? 0
    });
  }

  private mapWatchlistItem(source: Record<string, unknown>): ClientWatchlistItem {
    return new ClientWatchlistItem({
      UserAssetId: this.readString(source, ['userAssetId', 'UserAssetId']) ?? '',
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(source, ['companyName', 'CompanyName']) ?? '',
      Market: this.readString(source, ['market', 'Market']) ?? '',
      LastPrice: this.readNumber(source, ['lastPrice', 'LastPrice']) ?? 0,
      DayVariationPct: this.readNumber(source, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      HeldQuantity: this.readNumber(source, ['heldQuantity', 'HeldQuantity']) ?? 0,
      AverageBuyPrice: this.readNumber(source, ['averageBuyPrice', 'AverageBuyPrice']) ?? 0,
      InvestedAmount: this.readNumber(source, ['investedAmount', 'InvestedAmount']) ?? 0,
      OutstandingAmount: this.readNumber(source, ['outstandingAmount', 'OutstandingAmount']) ?? 0
    });
  }

  private mapQuote(source: Record<string, unknown>): ClientLiveQuote {
    return new ClientLiveQuote({
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      LastPrice: this.readNumber(source, ['lastPrice', 'LastPrice']) ?? 0,
      DayVariationPct: this.readNumber(source, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      AsOfUtc: this.readString(source, ['asOfUtc', 'AsOfUtc']) ?? ''
    });
  }

  private mapTransaction(source: Record<string, unknown>): ClientTransactionItem {
    return new ClientTransactionItem({
      Id: this.readString(source, ['id', 'Id']) ?? '',
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(source, ['companyName', 'CompanyName']) ?? '',
      TransactionType: this.readString(source, ['transactionType', 'TransactionType']) ?? '',
      Quantity: this.readNumber(source, ['quantity', 'Quantity']) ?? 0,
      UnitPrice: this.readNumber(source, ['unitPrice', 'UnitPrice']) ?? 0,
      Fees: this.readNumber(source, ['fees', 'Fees']) ?? 0,
      GrossAmount: this.readNumber(source, ['grossAmount', 'GrossAmount']) ?? 0,
      NetAmount: this.readNumber(source, ['netAmount', 'NetAmount']) ?? 0,
      TimestampUtc: this.readString(source, ['timestampUtc', 'TimestampUtc']) ?? ''
    });
  }

  private mapAnalysis(source: Record<string, unknown>): ClientAnalysisResult {
    return new ClientAnalysisResult({
      Id: this.readString(source, ['id', 'Id']) ?? '',
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      CompanyName: this.readString(source, ['companyName', 'CompanyName']) ?? '',
      Pattern: this.readString(source, ['pattern', 'Pattern']) ?? '',
      Phase: this.readString(source, ['phase', 'Phase']) ?? '',
      Confidence: this.readNumber(source, ['confidence', 'Confidence']) ?? 0,
      Recommendation: this.readString(source, ['recommendation', 'Recommendation']) ?? '',
      Reason: this.readString(source, ['reason', 'Reason']) ?? '',
      RiskLevel: this.readString(source, ['riskLevel', 'RiskLevel']) ?? '',
      HorizonDays: this.readNumber(source, ['horizonDays', 'HorizonDays']) ?? 0,
      PredictedAt: this.readString(source, ['predictedAt', 'PredictedAt']) ?? '',
      IsActionable: this.readBoolean(source, ['isActionable', 'IsActionable']) ?? false,
      ModelStatus: this.readString(source, ['modelStatus', 'ModelStatus']) ?? '',
      ModelMessage: this.readString(source, ['modelMessage', 'ModelMessage']) ?? '',
      CurrentPrice: this.readNumber(source, ['currentPrice', 'CurrentPrice']) ?? 0,
      TargetPrice: this.readNumber(source, ['targetPrice', 'TargetPrice']),
      InvalidationPrice: this.readNumber(source, ['invalidationPrice', 'InvalidationPrice'])
    });
  }

  private mapSimulation(source: Record<string, unknown>): ClientSimulationResult {
    return new ClientSimulationResult({
      Symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      Phase: this.readString(source, ['phase', 'Phase']) ?? '',
      InvestmentAmount: this.readNumber(source, ['investmentAmount', 'InvestmentAmount']) ?? 0,
      HorizonDays: this.readNumber(source, ['horizonDays', 'HorizonDays']) ?? 0,
      EstimatedReturnAmount: this.readNumber(source, ['estimatedReturnAmount', 'EstimatedReturnAmount']) ?? 0,
      EstimatedReturnPct: this.readNumber(source, ['estimatedReturnPct', 'EstimatedReturnPct']) ?? 0,
      EstimatedFinalAmount: this.readNumber(source, ['estimatedFinalAmount', 'EstimatedFinalAmount']) ?? 0,
      Recommendation: this.readString(source, ['recommendation', 'Recommendation']) ?? '',
      Assumption: this.readString(source, ['assumption', 'Assumption']) ?? '',
      CurrentPrice: this.readNumber(source, ['currentPrice', 'CurrentPrice']) ?? 0,
      TargetPrice: this.readNumber(source, ['targetPrice', 'TargetPrice']),
      InvalidationPrice: this.readNumber(source, ['invalidationPrice', 'InvalidationPrice']),
      IsActionable: this.readBoolean(source, ['isActionable', 'IsActionable']) ?? false
    });
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
