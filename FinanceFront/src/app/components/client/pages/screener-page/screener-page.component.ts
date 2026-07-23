import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { AssetTypeOptions, ScreenerItem, ScreenerMeta, ScreenerPage, ScreenerPreset } from '../../../../Models/client-finance-models/screener.model';
import type { IFundamentalScoreResult } from '../../../../Models/client-finance-models/fundamental-score.model';
import { UserPaths } from '../../../../Routes/app.routes.constants';
import { ScreenerService } from '../../../../services/screener.service';
import { FundamentalsService } from '../../../../services/fundamentals.service';
import { translateCountry } from '../../../../Models/client-finance-models/finance-localization.util';

const PAGE_SIZE = 20;

const SECTOR_LABELS: Record<string, string> = {
  'Technology': 'Technologie',
  'Healthcare': 'Santé',
  'Financial Services': 'Services financiers',
  'Consumer Cyclical': 'Consommation cyclique',
  'Energy': 'Énergie',
  'Industrials': 'Industrie',
  'Basic Materials': 'Matériaux de base',
  'Real Estate': 'Immobilier',
  'Utilities': 'Services publics',
  'Communication Services': 'Communication',
  'Consumer Defensive': 'Consommation défensive',
  'Information Technology': 'Technologies de l\'information',
};

@Component({
  selector: 'app-screener-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './screener-page.component.html',
  styleUrl: './screener-page.component.scss'
})
export class ScreenerPageComponent {
  private readonly screenerService = inject(ScreenerService);
  private readonly fundamentalsService = inject(FundamentalsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  readonly assetTypeOptions = AssetTypeOptions;

  readonly screener = signal<ScreenerPage>({ Items: [], Total: 0, Page: 1, PageSize: PAGE_SIZE });
  readonly meta = signal<ScreenerMeta>({ Sectors: [], Countries: [] });
  readonly scoreBySymbol = signal<Record<string, IFundamentalScoreResult>>({});
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly exporting = signal(false);
  readonly sortBy = signal<string>('Symbol');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly presets = signal<ScreenerPreset[]>([]);
  readonly savingPreset = signal(false);
  readonly selectedPresetId = signal<string>('');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.screener().Total / PAGE_SIZE)));
  readonly hasPrev = computed(() => this.screener().Page > 1);
  readonly hasNext = computed(() => this.screener().Page < this.totalPages());

  readonly filters = this.fb.nonNullable.group({
    sectors: this.fb.nonNullable.control<string[]>([]),
    countries: this.fb.nonNullable.control<string[]>([]),
    peaOnly: this.fb.nonNullable.control(false),
    assetType: this.fb.nonNullable.control<number | null>(null),
    minPE: this.fb.nonNullable.control<number | null>(null),
    maxPE: this.fb.nonNullable.control<number | null>(null),
    minDividendYield: this.fb.nonNullable.control<number | null>(null),
    minMarketCap: this.fb.nonNullable.control<number | null>(null),
    minScore: this.fb.nonNullable.control<number | null>(null)
  });

  readonly search = this.fb.nonNullable.control('');

  private currentPage = 1;

  constructor() {
    this.loadMeta();
    this.loadScreener();
    this.loadPresets();

    this.filters.valueChanges.pipe(
      distinctUntilChanged((prev, curr) => this.serializeServerFilters(prev) === this.serializeServerFilters(curr)),
      takeUntilDestroyed()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadScreener();
    });

    this.search.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadScreener();
    });
  }

  sort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDirection.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDirection.set('asc');
    }
    this.currentPage = 1;
    this.loadScreener();
  }

  navigateToInstrument(item: ScreenerItem): void {
    void this.router.navigate(['/client/instruments', item.Symbol]);
  }

  resetFilters(): void {
    this.filters.reset({
      sectors: [], countries: [], peaOnly: false, assetType: null,
      minPE: null, maxPE: null, minDividendYield: null, minMarketCap: null, minScore: null
    });
    this.search.reset('');
    this.selectedPresetId.set('');
    this.currentPage = 1;
    this.loadScreener();
  }

  exportCsv(): void {
    if (this.exporting()) return;
    this.exporting.set(true);
    this.error.set(null);

    const { sectors, countries, peaOnly, assetType, minPE, maxPE, minDividendYield, minMarketCap, minScore } = this.filters.getRawValue();

    this.screenerService.exportCsv({
      SortBy: this.sortBy(),
      SortDirection: this.sortDirection(),
      Sectors: sectors.length ? sectors : undefined,
      Countries: countries.length ? countries : undefined,
      PeaOnly: peaOnly || undefined,
      AssetType: assetType,
      Search: this.search.value.trim() || undefined,
      MinPE: minPE,
      MaxPE: maxPE,
      MinDividendYield: minDividendYield,
      MinMarketCap: minMarketCap,
      MinScore: minScore
    }).pipe(
      finalize(() => this.exporting.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (blob) => this.downloadBlob(blob, 'screener-export.csv'),
      error: () => this.error.set('Export du screener impossible.')
    });
  }

  private serializeServerFilters(value: Record<string, unknown>): string {
    return JSON.stringify(Object.entries(value));
  }

  formatMarketCap(value: number | null): string {
    if (value === null) return '—';
    if (value >= 1_000_000_000) return (value / 1_000_000_000).toFixed(1) + ' Md';
    if (value >= 1_000_000) return (value / 1_000_000).toFixed(1) + ' M';
    return new Intl.NumberFormat('fr-FR').format(value);
  }

  private loadPresets(): void {
    this.screenerService.getPresets().pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (data) => this.presets.set(data),
      error: () => this.error.set('Chargement des presets impossible.')
    });
  }

  applyPreset(presetId: string): void {
    this.selectedPresetId.set(presetId);
    if (!presetId) return;

    const preset = this.presets().find(p => p.Id === presetId);
    if (!preset) return;

    const q = preset.Query;
    this.filters.setValue({
      sectors: q.Sectors ?? [],
      countries: q.Countries ?? [],
      peaOnly: q.PeaOnly ?? false,
      assetType: q.AssetType ?? null,
      minPE: q.MinPE ?? null,
      maxPE: q.MaxPE ?? null,
      minDividendYield: q.MinDividendYield ?? null,
      minMarketCap: q.MinMarketCap ?? null,
      minScore: q.MinScore ?? null
    });
    this.search.setValue(q.Search ?? '');
    if (q.SortBy) this.sortBy.set(q.SortBy);
    if (q.SortDirection) this.sortDirection.set(q.SortDirection);

    this.currentPage = 1;
    this.loadScreener();
  }

  saveCurrentFiltersAsPreset(): void {
    const name = window.prompt('Nom du preset à sauvegarder ?');
    if (!name?.trim()) return;

    this.savingPreset.set(true);
    const { sectors, countries, peaOnly, assetType, minPE, maxPE, minDividendYield, minMarketCap, minScore } = this.filters.getRawValue();

    this.screenerService.savePreset({
      Name: name.trim(),
      Query: {
        SortBy: this.sortBy(),
        SortDirection: this.sortDirection(),
        Sectors: sectors.length ? sectors : undefined,
        Countries: countries.length ? countries : undefined,
        PeaOnly: peaOnly || undefined,
        AssetType: assetType,
        Search: this.search.value.trim() || undefined,
        MinPE: minPE,
        MaxPE: maxPE,
        MinDividendYield: minDividendYield,
        MinMarketCap: minMarketCap,
        MinScore: minScore
      }
    }).pipe(
      finalize(() => this.savingPreset.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => this.loadPresets(),
      error: () => this.error.set('Sauvegarde du preset impossible.')
    });
  }

  deletePreset(presetId: string): void {
    if (!presetId) return;

    this.screenerService.deletePreset(presetId).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => {
        if (this.selectedPresetId() === presetId) {
          this.selectedPresetId.set('');
        }
        this.loadPresets();
      },
      error: () => this.error.set('Suppression du preset impossible.')
    });
  }

  getVariationClass(value: number | null): string {
    if (value === null || value === 0) return 'text-muted';
    return value > 0 ? 'text-success' : 'text-danger';
  }

  formatVariation(value: number): string {
    const formatted = new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
    return (value > 0 ? '+' : '') + formatted + ' %';
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  goToPrev(): void {
    if (!this.hasPrev()) return;
    this.currentPage--;
    this.loadScreener();
  }

  goToNext(): void {
    if (!this.hasNext()) return;
    this.currentPage++;
    this.loadScreener();
  }

  getAssetTypeLabel(value: number): string {
    return this.assetTypeOptions.find(o => o.value === value)?.label ?? '—';
  }

  getSectorLabel(sector: string): string {
    return SECTOR_LABELS[sector] ?? sector;
  }

  getCountryLabel(country: string): string {
    return translateCountry(country);
  }

  private loadMeta(): void {
    this.screenerService.getMeta().pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (data) => this.meta.set(data),
      error: () => this.error.set('Chargement des filtres du screener impossible.')
    });
  }

  private loadScreener(): void {
    this.loading.set(true);
    this.error.set(null);

    const { sectors, countries, peaOnly, assetType, minPE, maxPE, minDividendYield, minMarketCap, minScore } = this.filters.getRawValue();

    this.screenerService.getScreener({
      Page: this.currentPage,
      PageSize: PAGE_SIZE,
      SortBy: this.sortBy(),
      SortDirection: this.sortDirection(),
      Sectors: sectors.length ? sectors : undefined,
      Countries: countries.length ? countries : undefined,
      PeaOnly: peaOnly || undefined,
      AssetType: assetType,
      Search: this.search.value.trim() || undefined,
      MinPE: minPE,
      MaxPE: maxPE,
      MinDividendYield: minDividendYield,
      MinMarketCap: minMarketCap,
      MinScore: minScore
    }).pipe(
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (data) => {
        this.screener.set(data);
        this.loadFundamentalScores(data.Items.map(item => item.Symbol));
      },
      error: () => {
        this.screener.set({ Items: [], Total: 0, Page: 1, PageSize: PAGE_SIZE });
        this.scoreBySymbol.set({});
        this.error.set('Chargement du screener impossible.');
      }
    });
  }

  private loadFundamentalScores(symbols: string[]): void {
    if (!symbols.length) {
      this.scoreBySymbol.set({});
      return;
    }

    this.fundamentalsService.getScore(symbols).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response) => {
        const map: Record<string, IFundamentalScoreResult> = {};
        for (const result of response.Results) {
          map[result.Symbol] = result;
        }
        this.scoreBySymbol.set(map);
      },
      error: () => this.scoreBySymbol.set({})
    });
  }

  getScoreDisplay(symbol: string): string {
    const s = this.scoreBySymbol()[symbol];
    if (!s || !s.UsableScore || s.TotalScore === null) return '—';
    return s.TotalScore.toFixed(0);
  }

  protected readonly UserPaths = UserPaths;
}
