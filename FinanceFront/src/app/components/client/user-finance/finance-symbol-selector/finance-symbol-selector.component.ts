import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  DestroyRef,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnInit,
  Output,
  signal,
  SimpleChanges
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { MarketAssetOption } from '../../../../Models/client-finance-models/client-finance-models';
import { translateCountry, translateSector } from '../../../../Models/client-finance-models/finance-localization.util';

const RECENT_KEY = 'pf_recent_symbols';
const RECENT_MAX = 5;

function loadRecentSymbols(): string[] {
  try {
    const raw = localStorage.getItem(RECENT_KEY);
    return raw ? (JSON.parse(raw) as string[]).slice(0, RECENT_MAX) : [];
  } catch {
    return [];
  }
}

function pushRecentSymbol(symbol: string): void {
  try {
    const existing = loadRecentSymbols().filter(s => s !== symbol);
    localStorage.setItem(RECENT_KEY, JSON.stringify([symbol, ...existing].slice(0, RECENT_MAX)));
  } catch {
    // ignore storage errors
  }
}

@Component({
  selector: 'app-finance-symbol-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule],
  templateUrl: './finance-symbol-selector.component.html',
  styleUrl: './finance-symbol-selector.component.scss'
})
export class FinanceSymbolSelectorComponent implements OnChanges, OnInit {
  @Input() selectedAsset: MarketAssetOption | null = null;

  // ── Inputs signal-backed : les computed() réagissent aux changements ────────
  private readonly _options = signal<MarketAssetOption[]>([]);
  @Input() set options(v: MarketAssetOption[]) { this._options.set(v); }
  get options(): MarketAssetOption[] { return this._options(); }

  private readonly _trendingOptions = signal<MarketAssetOption[]>([]);
  @Input() set trendingOptions(v: MarketAssetOption[]) { this._trendingOptions.set(v); }
  get trendingOptions(): MarketAssetOption[] { return this._trendingOptions(); }

  @Input() loading = false;
  @Input() peaFilterVisible = false;
  @Input() flat = false;

  @Output() searchChanged = new EventEmitter<string>();
  @Output() assetSelected = new EventEmitter<MarketAssetOption>();
  @Output() peaEligibleOnlyChanged = new EventEmitter<boolean>();

  selectedSymbol: string | null = null;
  readonly searchInput$ = new Subject<string>();

  // ── Filtres ──────────────────────────────────────────────────────────────
  readonly filterSector  = signal<string | null>(null);
  readonly filterCountry = signal<string | null>(null);
  readonly filterType    = signal<string | null>(null);
  readonly filterPea     = signal(false);

  // ── currentTerm en signal : displayedOptions() se recompute à chaque frappe
  private readonly _currentTerm = signal('');

  private readonly destroyRef = inject(DestroyRef);

  // ── Listes dédupliquées pour les dropdowns de filtres ────────────────────
  readonly availableSectors = computed(() =>
    [...new Set(this._options().map(o => o.Sector).filter((s): s is string => s !== null && s.length > 0))]
  );
  readonly availableCountries = computed(() =>
    [...new Set(this._options().map(o => o.Country).filter((c): c is string => c !== null && c.length > 0))]
  );
  readonly availableTypes = computed(() =>
    [...new Set(this._options().map(o => o.AssetType).filter((t): t is string => t !== null && t.length > 0))]
  );

  readonly hasActiveFilters = computed(() =>
    this.filterSector() !== null ||
    this.filterCountry() !== null ||
    this.filterType() !== null ||
    this.filterPea()
  );

  readonly filteredOptions = computed(() => {
    const sector  = this.filterSector();
    const country = this.filterCountry();
    const type    = this.filterType();
    const pea     = this.filterPea();
    let items = this._options();
    if (sector)  items = items.filter(o => o.Sector  === sector);
    if (country) items = items.filter(o => o.Country === country);
    if (type)    items = items.filter(o => o.AssetType === type);
    if (pea)     items = items.filter(o => o.IsPeaEligible);
    return items;
  });

  readonly recentSymbols = signal<string[]>(loadRecentSymbols());

  readonly zeroQueryItems = computed((): MarketAssetOption[] => {
    const recents = this.recentSymbols();
    const pool = [...this._options(), ...this._trendingOptions()];
    const recentItems = recents
      .map(sym => pool.find(o => o.Symbol === sym))
      .filter((o): o is MarketAssetOption => o !== undefined);

    const trending = this._trendingOptions().slice(0, RECENT_MAX);
    const recentSymSet = new Set(recentItems.map(o => o.Symbol));
    const merged = [...recentItems, ...trending.filter(o => !recentSymSet.has(o.Symbol))];
    return merged.slice(0, RECENT_MAX * 2);
  });

  readonly displayedOptions = computed((): MarketAssetOption[] => {
    if (this._currentTerm().length === 0 && this._options().length === 0) {
      return this.zeroQueryItems();
    }
    return this.filteredOptions();
  });

  ngOnInit(): void {
    this.searchInput$
      .pipe(
        debounceTime(200),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((term) => {
        const trimmed = (term ?? '').trim();
        this._currentTerm.set(trimmed);
        this.searchChanged.emit(trimmed);
        if (trimmed.length === 0) {
          this.filterSector.set(null);
          this.filterCountry.set(null);
          this.filterType.set(null);
          this.filterPea.set(false);
        }
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedAsset']) {
      this.selectedSymbol = this.selectedAsset?.Symbol ?? null;
    }
  }

  onSelectionChanged(value: MarketAssetOption | null): void {
    if (!value) {
      this.selectedSymbol = null;
      return;
    }
    // ng-select (change) émet toujours l'objet complet (x.value), pas la chaîne bindValue
    this.selectedSymbol = value.Symbol;
    pushRecentSymbol(value.Symbol);
    this.recentSymbols.set(loadRecentSymbols());
    this.assetSelected.emit(value);
  }

  onTogglePea(): void {
    this.filterPea.update(v => !v);
    this.peaEligibleOnlyChanged.emit(this.filterPea());
    this.searchChanged.emit(this._currentTerm());
  }

  onDropdownOpened(): void {
    this.searchChanged.emit(this._currentTerm());
  }

  readonly translateSector = translateSector;
  readonly translateCountry = translateCountry;

  clearFilters(): void {
    this.filterSector.set(null);
    this.filterCountry.set(null);
    this.filterType.set(null);
    this.filterPea.set(false);
    this.peaEligibleOnlyChanged.emit(false);
  }
}
