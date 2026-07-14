import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ClientLiveQuote, ClientWatchlistItem, MarketAssetOption } from '../../../../Models/client-finance-models/client-finance-models';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';
import { FinanceSymbolSelectorComponent } from '../../user-finance/finance-symbol-selector/finance-symbol-selector.component';
import { FinanceWatchlistComponent } from '../../user-finance/finance-watchlist/finance-watchlist.component';

@Component({ selector: 'app-watchlist-page', standalone: true, imports: [CommonModule, RouterLink, FinanceSymbolSelectorComponent, FinanceWatchlistComponent], templateUrl: './watchlist-page.component.html', styleUrl: './watchlist-page.component.scss' })
export class WatchlistPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  watchlist: ClientWatchlistItem[] = [];
  searchResults: MarketAssetOption[] = [];
  selectedAsset: MarketAssetOption | null = null;
  selectedQuote: ClientLiveQuote | null = null;
  watchlistLoading = false;
  searchLoading = false;
  quoteLoading = false;
  peaEligibleOnly = false;
  private lastQuery = '';

  constructor() { this.loadWatchlist(); }

  onSearchChanged(query: string): void {
    this.lastQuery = query;
    this.searchLoading = true;
    this.clientFinanceService.searchAssets(query, this.peaEligibleOnly).pipe(finalize(() => (this.searchLoading = false))).subscribe({ next: (results) => (this.searchResults = results), error: () => { this.searchResults = []; this.toastService.error('Recherche impossible.'); } });
  }
  onPeaFilterChanged(peaOnly: boolean): void { this.peaEligibleOnly = peaOnly; }
  onAssetSelected(asset: MarketAssetOption): void { this.selectedAsset = asset; this.fetchQuote(asset.Symbol); }
  addSelectedAssetToWatchlist(): void {
    if (!this.selectedAsset || this.watchlistLoading) return;
    this.watchlistLoading = true;
    this.clientFinanceService.addToWatchlist(this.selectedAsset.Symbol, this.selectedAsset.CompanyName, this.selectedAsset.Market).pipe(finalize(() => (this.watchlistLoading = false))).subscribe({ next: () => { this.toastService.success('Valeur ajoutée à la watchlist.'); this.loadWatchlist(); }, error: () => this.toastService.error('Ajout à la watchlist impossible.') });
  }
  fetchQuote(symbol: string): void {
    if (!symbol) return;
    this.quoteLoading = true;
    this.clientFinanceService.getLiveQuote(symbol).pipe(finalize(() => (this.quoteLoading = false))).subscribe({ next: (quote) => (this.selectedQuote = quote), error: () => { this.selectedQuote = null; this.toastService.error('Lecture du cours live impossible.'); } });
  }
  onAnalyze(symbol: string): void { void this.router.navigate(toCommands(UserPaths.AnalysisEntry), { queryParams: { symbol } }); }
  removeFromWatchlist(symbol: string): void {
    if (!symbol || this.watchlistLoading) return;
    this.watchlistLoading = true;
    this.clientFinanceService.removeFromWatchlist(symbol).pipe(finalize(() => (this.watchlistLoading = false))).subscribe({ next: () => { this.toastService.success('Valeur retirée de la watchlist.'); this.watchlist = this.watchlist.filter((item) => item.Symbol !== symbol); }, error: () => this.toastService.error('Suppression impossible.') });
  }
  private loadWatchlist(): void {
    this.watchlistLoading = true;
    this.clientFinanceService.getWatchlist().pipe(finalize(() => (this.watchlistLoading = false))).subscribe({ next: (items) => (this.watchlist = items), error: () => { this.watchlist = []; this.toastService.error('Chargement de la watchlist impossible.'); } });
  }
}
