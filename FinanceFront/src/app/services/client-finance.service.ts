import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClientAnalysisLaunchRequest,
  ClientAnalysisResult,
  ClientDashboardOverview,
  ClientLiveQuote,
  ClientSimulationRequest,
  ClientSimulationResult,
  ClientTransactionCreateRequest,
  ClientTransactionItem,
  ClientWatchlistItem,
  MarketAssetOption
} from '../Models/client-finance';

@Injectable({ providedIn: 'root' })
export class ClientFinanceService {
  private readonly baseUrl = `${environment.apiUrl}ClientFinance`;

  constructor(private readonly http: HttpClient) {}

  getDashboardOverview(): Observable<ClientDashboardOverview> {
    return this.http
      .get<Record<string, unknown>>(`${this.baseUrl}/dashboard`)
      .pipe(map((payload) => this.mapOverview(payload)));
  }

  searchAssets(query: string): Observable<MarketAssetOption[]> {
    const normalizedQuery = query.trim();
    if (normalizedQuery.length < 2) {
      return of([]);
    }

    const params = new HttpParams().set('query', normalizedQuery);

    return this.http
      .get<unknown[]>(`${this.baseUrl}/assets/search`, { params })
      .pipe(map((items) => items.map((item) => this.mapAsset(item))));
  }

  getWatchlist(): Observable<ClientWatchlistItem[]> {
    return this.http
      .get<unknown[]>(`${this.baseUrl}/watchlist`)
      .pipe(map((items) => items.map((item) => this.mapWatchlistItem(this.toRecord(item)))));
  }

  addToWatchlist(symbol: string, companyName: string, market: string): Observable<ClientWatchlistItem> {
    return this.http
      .post<Record<string, unknown>>(`${this.baseUrl}/watchlist`, {
        Symbol: symbol,
        CompanyName: companyName,
        Market: market
      })
      .pipe(map((item) => this.mapWatchlistItem(item)));
  }

  removeFromWatchlist(symbol: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/watchlist/${encodeURIComponent(symbol)}`);
  }

  getLiveQuote(symbol: string): Observable<ClientLiveQuote> {
    return this.http
      .get<Record<string, unknown>>(`${this.baseUrl}/quote/${encodeURIComponent(symbol)}`)
      .pipe(map((payload) => this.mapQuote(payload)));
  }

  registerTransaction(request: ClientTransactionCreateRequest): Observable<ClientTransactionItem> {
    return this.http
      .post<Record<string, unknown>>(`${this.baseUrl}/transactions`, {
        Symbol: request.symbol,
        TransactionType: request.transactionType,
        Quantity: request.quantity,
        UnitPrice: request.unitPrice,
        Fees: request.fees,
        TimestampUtc: request.timestampUtc
      })
      .pipe(map((payload) => this.mapTransaction(payload)));
  }

  getTransactions(take = 100): Observable<ClientTransactionItem[]> {
    const params = new HttpParams().set('take', take);

    return this.http
      .get<unknown[]>(`${this.baseUrl}/transactions`, { params })
      .pipe(map((items) => items.map((item) => this.mapTransaction(this.toRecord(item)))));
  }

  deleteTransaction(transactionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/transactions/${encodeURIComponent(transactionId)}`);
  }

  runAnalysis(request: ClientAnalysisLaunchRequest): Observable<ClientAnalysisResult> {
    return this.http
      .post<Record<string, unknown>>(`${this.baseUrl}/analysis/run`, { Symbol: request.symbol })
      .pipe(map((payload) => this.mapAnalysis(payload)));
  }

  getRecentAnalyses(limit = 8): Observable<ClientAnalysisResult[]> {
    const params = new HttpParams().set('take', limit);

    return this.http
      .get<unknown[]>(`${this.baseUrl}/analysis/recent`, { params })
      .pipe(map((items) => items.map((item) => this.mapAnalysis(this.toRecord(item)))));
  }

  runSimulation(request: ClientSimulationRequest): Observable<ClientSimulationResult> {
    return this.http
      .post<Record<string, unknown>>(`${this.baseUrl}/simulation/run`, {
        Symbol: request.symbol,
        Pattern: request.pattern,
        InvestmentAmount: request.investmentAmount,
        HorizonDays: request.horizonDays
      })
      .pipe(map((payload) => this.mapSimulation(payload)));
  }

  private mapOverview(payload: Record<string, unknown>): ClientDashboardOverview {
    return new ClientDashboardOverview({
      totalPortfolioValue: this.readNumber(payload, ['totalPortfolioValue', 'TotalPortfolioValue']) ?? 0,
      dayProfitLoss: this.readNumber(payload, ['dayProfitLoss', 'DayProfitLoss']) ?? 0,
      openPositions: this.readNumber(payload, ['openPositions', 'OpenPositions']) ?? 0,
      analysesThisWeek: this.readNumber(payload, ['analysesThisWeek', 'AnalysesThisWeek']) ?? 0,
      watchlistCount: this.readNumber(payload, ['watchlistCount', 'WatchlistCount']) ?? 0,
      recommendationWinRate: this.readNumber(payload, ['recommendationWinRate', 'RecommendationWinRate']) ?? 0,
      nextMarketOpenAt: this.readString(payload, ['nextMarketOpenAt', 'NextMarketOpenAt']) ?? '',
      totalInvested: this.readNumber(payload, ['totalInvested', 'TotalInvested']) ?? 0,
      totalOutstanding: this.readNumber(payload, ['totalOutstanding', 'TotalOutstanding']) ?? 0
    });
  }

  private mapAsset(source: unknown): MarketAssetOption {
    const payload = this.toRecord(source);

    return new MarketAssetOption({
      symbol: this.readString(payload, ['symbol', 'Symbol']) ?? '',
      companyName: this.readString(payload, ['companyName', 'CompanyName']) ?? '',
      market: this.readString(payload, ['market', 'Market']) ?? '',
      currency: this.readString(payload, ['currency', 'Currency']) ?? 'USD',
      lastPrice: this.readNumber(payload, ['lastPrice', 'LastPrice']) ?? 0,
      dayVariationPct: this.readNumber(payload, ['dayVariationPct', 'DayVariationPct']) ?? 0
    });
  }

  private mapWatchlistItem(source: Record<string, unknown>): ClientWatchlistItem {
    return new ClientWatchlistItem({
      userAssetId: this.readString(source, ['userAssetId', 'UserAssetId']) ?? '',
      symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      companyName: this.readString(source, ['companyName', 'CompanyName']) ?? '',
      market: this.readString(source, ['market', 'Market']) ?? '',
      lastPrice: this.readNumber(source, ['lastPrice', 'LastPrice']) ?? 0,
      dayVariationPct: this.readNumber(source, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      heldQuantity: this.readNumber(source, ['heldQuantity', 'HeldQuantity']) ?? 0,
      averageBuyPrice: this.readNumber(source, ['averageBuyPrice', 'AverageBuyPrice']) ?? 0,
      investedAmount: this.readNumber(source, ['investedAmount', 'InvestedAmount']) ?? 0,
      outstandingAmount: this.readNumber(source, ['outstandingAmount', 'OutstandingAmount']) ?? 0
    });
  }

  private mapQuote(source: Record<string, unknown>): ClientLiveQuote {
    return new ClientLiveQuote({
      symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      lastPrice: this.readNumber(source, ['lastPrice', 'LastPrice']) ?? 0,
      dayVariationPct: this.readNumber(source, ['dayVariationPct', 'DayVariationPct']) ?? 0,
      asOfUtc: this.readString(source, ['asOfUtc', 'AsOfUtc']) ?? ''
    });
  }

  private mapTransaction(source: Record<string, unknown>): ClientTransactionItem {
    return new ClientTransactionItem({
      id: this.readString(source, ['id', 'Id']) ?? '',
      symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      companyName: this.readString(source, ['companyName', 'CompanyName']) ?? '',
      transactionType: this.readString(source, ['transactionType', 'TransactionType']) ?? '',
      quantity: this.readNumber(source, ['quantity', 'Quantity']) ?? 0,
      unitPrice: this.readNumber(source, ['unitPrice', 'UnitPrice']) ?? 0,
      fees: this.readNumber(source, ['fees', 'Fees']) ?? 0,
      grossAmount: this.readNumber(source, ['grossAmount', 'GrossAmount']) ?? 0,
      netAmount: this.readNumber(source, ['netAmount', 'NetAmount']) ?? 0,
      timestampUtc: this.readString(source, ['timestampUtc', 'TimestampUtc']) ?? ''
    });
  }

  private mapAnalysis(source: Record<string, unknown>): ClientAnalysisResult {
    return new ClientAnalysisResult({
      id: this.readString(source, ['id', 'Id']) ?? '',
      symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      companyName: this.readString(source, ['companyName', 'CompanyName']) ?? '',
      pattern: this.readString(source, ['pattern', 'Pattern']) ?? '',
      confidence: this.readNumber(source, ['confidence', 'Confidence']) ?? 0,
      recommendation: this.readString(source, ['recommendation', 'Recommendation']) ?? '',
      reason: this.readString(source, ['reason', 'Reason']) ?? '',
      riskLevel: this.readString(source, ['riskLevel', 'RiskLevel']) ?? '',
      horizonDays: this.readNumber(source, ['horizonDays', 'HorizonDays']) ?? 0,
      predictedAt: this.readString(source, ['predictedAt', 'PredictedAt']) ?? ''
    });
  }

  private mapSimulation(source: Record<string, unknown>): ClientSimulationResult {
    return new ClientSimulationResult({
      symbol: this.readString(source, ['symbol', 'Symbol']) ?? '',
      investmentAmount: this.readNumber(source, ['investmentAmount', 'InvestmentAmount']) ?? 0,
      horizonDays: this.readNumber(source, ['horizonDays', 'HorizonDays']) ?? 0,
      estimatedReturnAmount: this.readNumber(source, ['estimatedReturnAmount', 'EstimatedReturnAmount']) ?? 0,
      estimatedReturnPct: this.readNumber(source, ['estimatedReturnPct', 'EstimatedReturnPct']) ?? 0,
      estimatedFinalAmount: this.readNumber(source, ['estimatedFinalAmount', 'EstimatedFinalAmount']) ?? 0,
      recommendation: this.readString(source, ['recommendation', 'Recommendation']) ?? '',
      assumption: this.readString(source, ['assumption', 'Assumption']) ?? ''
    });
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
