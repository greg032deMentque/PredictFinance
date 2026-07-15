import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, Subscription, debounceTime, distinctUntilChanged, finalize, interval, switchMap } from 'rxjs';
import {
  ClientAnalysisLaunchRequest,
  ClientAnalysisResult,
  ClientDashboardOverview,
  ClientLiveQuote,
  ClientMultiSimulationRequest,
  ClientTransactionCreateRequest,
  ClientTransactionItem,
  ClientWatchlistItem,
  MarketAssetOption
} from '../../../../Models/client-finance-models/client-finance-models';
// Note : ClientSimulationResult (ancien modèle plat) non importé ici — MultiSimulationDossier le remplace.
import type { AnalysisDossier } from '../../../../Models/client-finance-models/client-analysis-dossier.model';
import type { MultiSimulationDossier } from '../../../../Models/client-finance-models/client-simulation-dossier.model';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';
import { FinanceAnalysisHistoryComponent } from '../finance-analysis-history/finance-analysis-history.component';
import { FinanceAnalysisResultComponent } from '../finance-analysis-result/finance-analysis-result.component';
import { FinanceSimulationComponent } from '../finance-simulation/finance-simulation.component';
import { FinanceSymbolSelectorComponent } from '../finance-symbol-selector/finance-symbol-selector.component';
import { FinanceTransactionFormComponent } from '../finance-transaction-form/finance-transaction-form.component';
import { FinanceWatchlistComponent } from '../finance-watchlist/finance-watchlist.component';

@Component({
  selector: 'app-user-finance-page',
  standalone: true,
  imports: [
    CommonModule,
    FinanceSymbolSelectorComponent,
    FinanceWatchlistComponent,
    FinanceTransactionFormComponent,
    FinanceSimulationComponent,
    FinanceAnalysisResultComponent,
    FinanceAnalysisHistoryComponent
  ],
  templateUrl: './user-finance-page.component.html',
  styleUrl: './user-finance-page.component.scss'
})
export class UserFinancePageComponent implements OnInit, OnDestroy {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toastService = inject(ToastService);
  private readonly searchTerm$ = new Subject<string>();
  private quoteRefreshSubscription: Subscription | null = null;

  selectedAsset: MarketAssetOption | null = null;
  selectedSymbol = '';
  selectedQuote: ClientLiveQuote | null = null;

  searchResults: MarketAssetOption[] = [];
  readonly watchlist = signal<ClientWatchlistItem[]>([]);
  transactions: ClientTransactionItem[] = [];

  readonly trendingOptions = computed(() =>
    this.watchlist().slice(0, 5).map(
      (item) => new MarketAssetOption({
        Symbol: item.Symbol,
        CompanyName: item.CompanyName,
        Market: item.Market,
        LastPrice: item.LastPrice,
        DayVariationPct: item.DayVariationPct
      })
    )
  );

  lastResult: AnalysisDossier | null = null;
  history: ClientAnalysisResult[] = [];
  simulationResult: MultiSimulationDossier | null = null;
  overview = new ClientDashboardOverview();

  searchLoading = false;
  watchlistLoading = false;
  quoteLoading = false;
  analysisLoading = false;
  transactionLoading = false;
  simulationLoading = false;
  overviewLoading = false;

  ngOnInit(): void {
    this.initializeSearchStream();
    this.loadOverview();
    this.loadWatchlist();
    this.loadTransactions();
    this.loadHistory();
  }

  ngOnDestroy(): void {
    this.stopQuoteRefresh();
  }

  onSearchChanged(term: string): void {
    this.searchTerm$.next(term);
  }

  onAssetSelected(asset: MarketAssetOption): void {
    this.selectedAsset = asset;
    this.selectedSymbol = asset.Symbol;
    this.searchResults = [];
  }

  addSelectedAssetToWatchlist(): void {
    if (!this.selectedAsset) {
      this.toastService.warning('Selectionne une valeur avant de l ajouter a la watchlist.');
      return;
    }

    this.watchlistLoading = true;

    this.clientFinanceService
      .addToWatchlist(this.selectedAsset.Symbol, this.selectedAsset.CompanyName, this.selectedAsset.Market)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.watchlistLoading = false))
      )
      .subscribe({
        next: (item) => {
          if (!this.watchlist().some((x) => x.UserAssetId === item.UserAssetId)) {
            this.watchlist.set([item, ...this.watchlist()]);
          }

          this.selectedSymbol = item.Symbol;
          this.toastService.success('Valeur ajoutee a la watchlist.');
          this.loadOverview();
        },
        error: () => {
          this.toastService.error('Impossible d ajouter la valeur a la watchlist.');
        }
      });
  }

  onRequestQuote(symbol: string): void {
    this.selectedSymbol = symbol;
    this.fetchQuote(symbol);
    this.startQuoteRefresh(symbol);
  }

  onAnalyzeFromWatchlist(symbol: string): void {
    if (!symbol || this.analysisLoading) {
      return;
    }

    this.selectedSymbol = symbol;
    this.toastService.success(`Analyse lancee pour ${symbol}.`);
    this.launchAnalysis();
  }

  onRemoveFromWatchlist(symbol: string): void {
    if (!symbol || this.watchlistLoading) {
      return;
    }

    const shouldRemove = window.confirm(`Retirer ${symbol} de la watchlist ?`);
    if (!shouldRemove) {
      return;
    }

    this.watchlistLoading = true;
    this.clientFinanceService
      .removeFromWatchlist(symbol)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.watchlistLoading = false))
      )
      .subscribe({
        next: () => {
          this.watchlist.set(this.watchlist().filter((item) => item.Symbol !== symbol));
          if (this.selectedSymbol === symbol) {
            this.selectedSymbol = '';
            this.selectedQuote = null;
            this.stopQuoteRefresh();
          }
          this.toastService.success('Valeur retiree de la watchlist.');
          this.loadOverview();
        },
        error: () => {
          this.toastService.error("Impossible de retirer la valeur (position ouverte possible).");
        }
      });
  }

  onSaveTransaction(request: ClientTransactionCreateRequest): void {
    this.transactionLoading = true;

    this.clientFinanceService
      .registerTransaction(request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.transactionLoading = false))
      )
      .subscribe({
        next: (transaction) => {
          this.transactions = [transaction, ...this.transactions].slice(0, 50);
          this.toastService.success("Transaction enregistrée.");
          this.loadWatchlist();
          this.loadOverview();
          this.fetchQuote(request.Symbol);
        },
        error: () => {
          this.toastService.error('Echec de l enregistrement de la transaction.');
        }
      });
  }

  deleteTransaction(transactionId: string): void {
    if (!transactionId || this.transactionLoading) {
      return;
    }

    const shouldDelete = window.confirm('Supprimer cette transaction ?');
    if (!shouldDelete) {
      return;
    }

    this.transactionLoading = true;
    this.clientFinanceService
      .deleteTransaction(transactionId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.transactionLoading = false))
      )
      .subscribe({
        next: () => {
          this.transactions = this.transactions.filter((item) => item.Id !== transactionId);
          this.toastService.success("Transaction supprimée.");
          this.loadWatchlist();
          this.loadOverview();
          if (this.selectedSymbol) {
            this.fetchQuote(this.selectedSymbol);
          }
        },
        error: () => {
          this.toastService.error('Suppression de transaction impossible.');
        }
      });
  }

  launchAnalysis(): void {
    if (!this.selectedSymbol || this.analysisLoading) {
      this.toastService.warning('Selectionne une valeur avant de lancer l analyse.');
      return;
    }

    this.analysisLoading = true;

    this.clientFinanceService
      .runAnalysis(new ClientAnalysisLaunchRequest({ Symbol: this.selectedSymbol }))
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.analysisLoading = false))
      )
      .subscribe({
        next: (dossier) => {
          this.lastResult = dossier;
          // Alimenter l'historique (modèle plat) depuis le dossier
          const mainPattern = dossier.MainPattern;
          const historyEntry = new ClientAnalysisResult({
            Id: dossier.Id,
            Symbol: dossier.Symbol,
            CompanyName: dossier.CompanyName,
            Pattern: mainPattern?.PatternId ?? '',
            Phase: mainPattern?.PhaseCode ?? '',
            Probability: mainPattern?.ConfidenceScore ?? 0,
            RecommendationAction: (mainPattern?.RecommendationAction ?? '') as ClientAnalysisResult['RecommendationAction'],
            RecommendationReason: mainPattern?.RecommendationReason ?? '',
            RiskLevel: (mainPattern?.RiskLevel ?? '') as ClientAnalysisResult['RiskLevel'],
            RecommendationHorizonDays: mainPattern?.RecommendationHorizonDays ?? 0,
            PredictedAt: dossier.PredictedAt,
            IsActionable: mainPattern?.IsActionable ?? false,
            ModelStatus: '' as ClientAnalysisResult['ModelStatus'],
            ModelMessage: dossier.ModelMessage,
            CurrentPrice: mainPattern?.CurrentPrice ?? 0,
            NecklinePrice: null,
            TargetPrice: mainPattern?.SuggestedTakeProfit ?? null,
            InvalidationPrice: mainPattern?.InvalidationLevel ?? null
          });
          this.history = [historyEntry, ...this.history].slice(0, 20);
          this.toastService.success("Analyse terminée.");
          this.loadOverview();
        },
        error: () => {
          this.toastService.error('Echec du lancement de l analyse.');
        }
      });
  }

  launchSimulation(request: ClientMultiSimulationRequest): void {
    this.simulationLoading = true;

    this.clientFinanceService
      .runMultiSimulation(request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.simulationLoading = false))
      )
      .subscribe({
        next: (dossier) => {
          this.simulationResult = dossier;
          this.toastService.success('Simulation terminee.');
        },
        error: () => {
          this.toastService.error('Echec de la simulation.');
        }
      });
  }

  private initializeSearchStream(): void {
    this.searchTerm$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((term) => {
          this.searchLoading = true;
          return this.clientFinanceService.searchAssets(term).pipe(finalize(() => (this.searchLoading = false)));
        })
      )
      .subscribe({
        next: (assets) => (this.searchResults = assets),
        error: () => {
          this.searchResults = [];
        }
      });
  }

  private loadOverview(): void {
    this.overviewLoading = true;

    this.clientFinanceService
      .getDashboardOverview()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.overviewLoading = false))
      )
      .subscribe({
        next: (overview) => (this.overview = overview),
        error: () => {
          this.overview = new ClientDashboardOverview();
        }
      });
  }

  private loadWatchlist(): void {
    this.watchlistLoading = true;

    this.clientFinanceService
      .getWatchlist()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.watchlistLoading = false))
      )
      .subscribe({
        next: (items) => this.watchlist.set(items),
        error: () => this.watchlist.set([])
      });
  }

  private loadTransactions(): void {
    this.clientFinanceService
      .getTransactions(50)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => (this.transactions = items),
        error: () => {
          this.transactions = [];
        }
      });
  }

  private loadHistory(): void {
    this.clientFinanceService
      .getRecentAnalyses(20)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => (this.history = items),
        error: () => {
          this.history = [];
        }
      });
  }

  private fetchQuote(symbol: string): void {
    this.quoteLoading = true;

    this.clientFinanceService
      .getLiveQuote(symbol)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.quoteLoading = false))
      )
      .subscribe({
        next: (quote) => (this.selectedQuote = quote),
        error: () => {
          this.selectedQuote = null;
        }
      });
  }

  private startQuoteRefresh(symbol: string): void {
    this.stopQuoteRefresh();

    this.quoteRefreshSubscription = interval(15000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.fetchQuote(symbol);
      });
  }

  private stopQuoteRefresh(): void {
    if (!this.quoteRefreshSubscription) {
      return;
    }

    this.quoteRefreshSubscription.unsubscribe();
    this.quoteRefreshSubscription = null;
  }
}
