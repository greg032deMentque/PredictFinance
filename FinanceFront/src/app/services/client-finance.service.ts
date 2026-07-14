import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClientAnalysisDetail,
  ClientAnalysisLaunchRequest,
  ClientAnalysisResult,
  ClientDashboardOverview,
  ClientHistoryFeed,
  ClientHistoryPage,
  ClientInstrumentDetail,
  ClientInstrumentHistory,
  ClientInstrumentHistoryPage,
  ClientLiveQuote,
  ClientPatternDetail,
  ClientPatternEvaluateRequest,
  ClientPatternEvaluateResult,
  ClientPortfolio,
  ClientSimulationRequest,
  ClientSimulationResult,
  ClientTransactionCreateRequest,
  ClientTransactionItem,
  ClientWatchlistItem,
  CreateClientAlertRequest,
  HistoryQueryOptions,
  InstrumentHistoryQueryOptions,
  LearnOverview,
  MarketAssetOption,
  OnboardingGuidance,
  ParameterDetail,
  PatternCatalogItem,
  SnapshotComparison
} from '../Models/client-finance-models/client-finance-models';
import { ClientFinanceMapper } from './client-finance.mapper';

@Injectable({ providedIn: 'root' })
export class ClientFinanceService {

  private readonly http = inject(HttpClient);
  private readonly mapper = inject(ClientFinanceMapper);

  getDashboardOverview(): Observable<ClientDashboardOverview> {
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/dashboard`)
      .pipe(map((payload) => this.mapper.mapOverview(payload)));
  }

  searchAssets(query: string, peaEligibleOnly = false): Observable<MarketAssetOption[]> {
    const normalizedQuery = query.trim();

    const params = new HttpParams()
      .set('query', normalizedQuery)
      .set('peaEligibleOnly', String(peaEligibleOnly));

    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/assets/search`, { params })
      .pipe(map((items) => items.map((item) => this.mapper.mapAsset(item))));
  }

  getWatchlist(): Observable<ClientWatchlistItem[]> {
    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/watchlist`)
      .pipe(map((items) => items.map((item) => this.mapper.mapWatchlistItem(item))));
  }

  addToWatchlist(symbol: string, companyName: string, market: string): Observable<ClientWatchlistItem> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/watchlist`, {
        Symbol: symbol,
        CompanyName: companyName,
        Market: market
      })
      .pipe(map((item) => this.mapper.mapWatchlistItem(item)));
  }

  removeFromWatchlist(symbol: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}ClientFinance/watchlist/${encodeURIComponent(symbol)}`);
  }

  getLiveQuote(symbol: string): Observable<ClientLiveQuote> {
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/quote/${encodeURIComponent(symbol)}`)
      .pipe(map((payload) => this.mapper.mapQuote(payload)));
  }

  getPortfolio(portfolioId?: string): Observable<ClientPortfolio> {
    const params = portfolioId
      ? new HttpParams().set('portfolioId', portfolioId)
      : new HttpParams();
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/portfolio`, { params })
      .pipe(map((payload) => this.mapper.mapPortfolio(payload)));
  }

  getHistory(options: HistoryQueryOptions = {}): Observable<ClientHistoryPage> {
    let params = new HttpParams();
    if (options.page != null) params = params.set('page', options.page);
    if (options.pageSize != null) params = params.set('pageSize', options.pageSize);
    if (options.symbol) params = params.set('symbol', options.symbol);
    if (options.recommendation) params = params.set('recommendation', options.recommendation);
    if (options.sortDirection) params = params.set('sortDirection', options.sortDirection);

    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/history`, { params })
      .pipe(map((payload) => this.mapper.mapHistoryPage(payload)));
  }

  getAnalysisDetail(analysisId: string): Observable<ClientAnalysisDetail> {
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/analysis/${encodeURIComponent(analysisId)}`)
      .pipe(map((payload) => this.mapper.mapAnalysisDetail(payload)));
  }

  getPatternCatalog(): Observable<PatternCatalogItem[]> {
    return this.http.get<PatternCatalogItem[]>(`${environment.apiUrl}ClientFinance/patterns/catalog`);
  }

  sendContactMessage(subject: string, message: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}ClientFinance/contact`, {
      Subject: subject,
      Message: message
    });
  }

  getInstrumentDetail(symbol: string): Observable<ClientInstrumentDetail> {
    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/instruments/${encodeURIComponent(symbol)}`)
      .pipe(map((payload) => this.mapper.mapInstrumentDetail(payload)));
  }

  getInstrumentHistory(symbol: string, options: InstrumentHistoryQueryOptions = {}): Observable<ClientInstrumentHistoryPage> {
    let params = new HttpParams();
    if (options.page != null) params = params.set('page', options.page);
    if (options.pageSize != null) params = params.set('pageSize', options.pageSize);
    if (options.sortDirection) params = params.set('sortDirection', options.sortDirection);

    return this.http
      .get<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/instruments/${encodeURIComponent(symbol)}/analysis-history`, { params })
      .pipe(map((payload) => this.mapper.mapInstrumentHistoryPage(payload)));
  }

  registerTransaction(request: ClientTransactionCreateRequest): Observable<ClientTransactionItem> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/transactions`, {
        Symbol: request.Symbol,
        TransactionType: request.TransactionType,
        Quantity: request.Quantity,
        UnitPrice: request.UnitPrice,
        Fees: request.Fees,
        TimestampUtc: request.TimestampUtc,
        PortfolioId: request.PortfolioId
      })
      .pipe(map((payload) => this.mapper.mapTransaction(payload)));
  }

  getTransactions(take = 100, portfolioId?: string): Observable<ClientTransactionItem[]> {
    let params = new HttpParams().set('take', take);
    if (portfolioId) params = params.set('portfolioId', portfolioId);

    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/transactions`, { params })
      .pipe(map((items) => items.map((item) => this.mapper.mapTransaction(item))));
  }

  deleteTransaction(transactionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}ClientFinance/transactions/${encodeURIComponent(transactionId)}`);
  }

  runAnalysis(request: ClientAnalysisLaunchRequest): Observable<ClientAnalysisResult> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/analysis/run`, {
        Symbol: request.Symbol,
        RequestedPatternIds: request.RequestedPatternIds
      })
      .pipe(map((payload) => this.mapper.mapAnalysis(payload)));
  }

  getRecentAnalyses(limit = 8): Observable<ClientAnalysisResult[]> {
    const params = new HttpParams().set('take', limit);

    return this.http
      .get<unknown[]>(`${environment.apiUrl}ClientFinance/analysis/recent`, { params })
      .pipe(map((items) => items.map((item) => this.mapper.mapAnalysis(item))));
  }

  getLearnOverview(): Observable<LearnOverview> {
    return this.http.get<LearnOverview>(`${environment.apiUrl}ClientFinance/learn`);
  }

  getOnboarding(): Observable<OnboardingGuidance> {
    return this.http.get<OnboardingGuidance>(`${environment.apiUrl}ClientFinance/onboarding`);
  }

  getParameterDetail(analysisId: string, parameterId: string): Observable<ParameterDetail> {
    return this.http.get<ParameterDetail>(
      `${environment.apiUrl}ClientFinance/parameters/${encodeURIComponent(analysisId)}/${encodeURIComponent(parameterId)}`
    );
  }

  compareSnapshots(leftSnapshotId: string, rightSnapshotId: string): Observable<SnapshotComparison> {
    return this.http.post<SnapshotComparison>(`${environment.apiUrl}ClientFinance/snapshots/compare`, {
      LeftSnapshotId: leftSnapshotId,
      RightSnapshotId: rightSnapshotId
    });
  }

  runSimulation(request: ClientSimulationRequest): Observable<ClientSimulationResult> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/simulation/run`, {
        Symbol: request.Symbol,
        Pattern: request.Pattern,
        InvestmentAmount: request.InvestmentAmount,
        HorizonDays: request.HorizonDays
      })
      .pipe(map((payload) => this.mapper.mapSimulation(payload)));
  }

  evaluatePatterns(request: ClientPatternEvaluateRequest): Observable<ClientPatternEvaluateResult> {
    return this.http.post<ClientPatternEvaluateResult>(
      `${environment.apiUrl}ClientFinance/patterns/evaluate`,
      { Symbol: request.Symbol, HoldingContext: request.HoldingContext }
    );
  }

  getPatternDetail(analysisId: string, patternId: string, holds: boolean): Observable<ClientPatternDetail> {
    const params = new HttpParams().set('holds', holds);
    return this.http.get<ClientPatternDetail>(
      `${environment.apiUrl}ClientFinance/patterns/${encodeURIComponent(analysisId)}/${encodeURIComponent(patternId)}`,
      { params }
    );
  }

  createAlert(request: CreateClientAlertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}ClientFinance/alerts`, {
      Symbol: request.Symbol,
      Trigger: request.Trigger,
      LevelValue: request.LevelValue,
      PatternId: request.PatternId
    });
  }
}
