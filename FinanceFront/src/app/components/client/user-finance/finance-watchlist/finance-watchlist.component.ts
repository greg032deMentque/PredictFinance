import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, EventEmitter, Input, OnChanges, Output, signal, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClientLiveQuote, ClientWatchlistItem } from '../../../../Models/client-finance-models/client-finance-models';
import { SearchBarComponent } from '../../../shared/search-bar/search-bar.component';

type SortKey = 'Symbol' | 'DayVariationPct' | 'HeldQuantity' | 'OutstandingAmount' | 'Recommendation';
type SortDir = 'asc' | 'desc';
type RecoFilter = 'all' | 'held' | string;
type Density = 'compact' | 'regular';

interface WatchlistViewState {
  sortKey: SortKey;
  sortDir: SortDir;
  recoFilter: RecoFilter;
  searchText: string;
  density: Density;
}

const VIEW_STATE_KEY = 'predictfinance_watchlist_view_state';

const DEFAULTS: WatchlistViewState = {
  sortKey: 'Symbol',
  sortDir: 'asc',
  recoFilter: 'all',
  searchText: '',
  density: 'regular'
};

const RECO_BADGE: Record<string, { cssClass: string; label: string }> = {
  Acheter:   { cssClass: 'badge-buy',  label: 'Acheter' },
  Buy:       { cssClass: 'badge-buy',  label: 'Acheter' },
  Vendre:    { cssClass: 'badge-sell', label: 'Vendre'  },
  Sell:      { cssClass: 'badge-sell', label: 'Vendre'  },
  Conserver: { cssClass: 'badge-hold', label: 'Conserver' },
  Hold:      { cssClass: 'badge-hold', label: 'Conserver' }
};

@Component({
  selector: 'app-finance-watchlist',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe, DecimalPipe, SearchBarComponent],
  templateUrl: './finance-watchlist.component.html',
  styleUrl: './finance-watchlist.component.scss'
})
export class FinanceWatchlistComponent implements OnChanges {
  @Input() watchlist: ClientWatchlistItem[] = [];
  @Input() quote: ClientLiveQuote | null = null;
  @Input() quoteLoading = false;

  @Output() requestQuote    = new EventEmitter<string>();
  @Output() requestAnalysis = new EventEmitter<string>();
  @Output() removeFromWatchlist = new EventEmitter<string>();

  // ── View state (signals) ────────────────────────────────────────────────
  private readonly _state = signal<WatchlistViewState>(this.loadState());

  readonly sortKey   = computed(() => this._state().sortKey);
  readonly sortDir   = computed(() => this._state().sortDir);
  readonly recoFilter = computed(() => this._state().recoFilter);
  readonly searchText = computed(() => this._state().searchText);
  readonly density    = computed(() => this._state().density);

  // ── Unique reco labels for filter dropdown ──────────────────────────────
  readonly recoLabels = signal<string[]>([]);

  // ── Processed list ──────────────────────────────────────────────────────
  readonly filteredList = computed(() => {
    const state = this._state();
    let items = [...this.watchlist];

    // Search filter
    const search = state.searchText.trim().toLowerCase();
    if (search) {
      items = items.filter(i =>
        i.Symbol.toLowerCase().includes(search) ||
        i.CompanyName.toLowerCase().includes(search)
      );
    }

    // Held filter
    if (state.recoFilter === 'held') {
      items = items.filter(i => i.HeldQuantity > 0);
    } else if (state.recoFilter !== 'all') {
      items = items.filter(i =>
        i.HasPersistedAnalysis &&
        this.normalizeRecoLabel(i.Recommendation.DisplayLabel) === state.recoFilter
      );
    }

    // Sort
    items.sort((a, b) => {
      let cmp = 0;
      switch (state.sortKey) {
        case 'Symbol':           cmp = a.Symbol.localeCompare(b.Symbol); break;
        case 'DayVariationPct':  cmp = a.DayVariationPct - b.DayVariationPct; break;
        case 'HeldQuantity':     cmp = a.HeldQuantity - b.HeldQuantity; break;
        case 'OutstandingAmount': cmp = a.OutstandingAmount - b.OutstandingAmount; break;
        case 'Recommendation':   cmp = a.Recommendation.DisplayLabel.localeCompare(b.Recommendation.DisplayLabel); break;
      }
      return state.sortDir === 'asc' ? cmp : -cmp;
    });

    return items;
  });

  // ── Lifecycle ───────────────────────────────────────────────────────────
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['watchlist']) {
      this.updateRecoLabels();
    }
  }

  // ── Sorting ─────────────────────────────────────────────────────────────
  toggleSort(key: SortKey): void {
    this._state.update(s => {
      const dir = s.sortKey === key && s.sortDir === 'asc' ? 'desc' : 'asc';
      return { ...s, sortKey: key, sortDir: dir };
    });
    this.persistState();
  }

  sortIcon(key: SortKey): string {
    const s = this._state();
    if (s.sortKey !== key) return 'bi-arrow-down-up text-muted opacity-50';
    return s.sortDir === 'asc' ? 'bi-sort-down-alt' : 'bi-sort-up-alt';
  }

  // ── Filters ─────────────────────────────────────────────────────────────
  setRecoFilter(value: RecoFilter): void {
    this._state.update(s => ({ ...s, recoFilter: value }));
    this.persistState();
  }

  setSearchText(value: string): void {
    this._state.update(s => ({ ...s, searchText: value }));
    this.persistState();
  }

  toggleDensity(): void {
    this._state.update(s => ({ ...s, density: s.density === 'compact' ? 'regular' : 'compact' }));
    this.persistState();
  }

  // ── Badge helpers ────────────────────────────────────────────────────────
  recoBadge(label: string): { cssClass: string; label: string } {
    return RECO_BADGE[label] ?? { cssClass: 'badge-neutral', label };
  }

  /** True si l'analyse doit déclencher une pastille d'alerte de fraîcheur. */
  isStaleAlert(item: ClientWatchlistItem): boolean {
    return item.HasPersistedAnalysis && item.Freshness.Status === 'Stale';
  }

  isAgingAlert(item: ClientWatchlistItem): boolean {
    return item.HasPersistedAnalysis && item.Freshness.Status === 'Aging';
  }

  // ── Actions ──────────────────────────────────────────────────────────────
  onSelectSymbol(symbol: string): void  { this.requestQuote.emit(symbol); }
  onRemoveSymbol(symbol: string): void  { this.removeFromWatchlist.emit(symbol); }
  onAnalyzeSymbol(symbol: string): void { this.requestAnalysis.emit(symbol); }

  // ── Private ───────────────────────────────────────────────────────────────
  private updateRecoLabels(): void {
    const labels = [...new Set(
      this.watchlist
        .filter(i => i.HasPersistedAnalysis && i.Recommendation.DisplayLabel)
        .map(i => this.normalizeRecoLabel(i.Recommendation.DisplayLabel))
    )];
    this.recoLabels.set(labels);
  }

  private normalizeRecoLabel(label: string): string {
    return RECO_BADGE[label]?.label ?? label;
  }

  private loadState(): WatchlistViewState {
    try {
      const raw = localStorage.getItem(VIEW_STATE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as Partial<WatchlistViewState>;
        return { ...DEFAULTS, ...parsed };
      }
    } catch {
      // ignore malformed storage
    }
    return { ...DEFAULTS };
  }

  private persistState(): void {
    try {
      localStorage.setItem(VIEW_STATE_KEY, JSON.stringify(this._state()));
    } catch {
      // ignore storage errors (ex. private mode)
    }
  }
}
