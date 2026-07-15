import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';
import {
  AnalysisConcept,
  ClientPatternDetail,
  ClientPatternEvaluateResult,
  ClientSupportResistanceZone,
  CreateClientAlertRequest,
  MarketAssetOption,
  PatternCatalogItem,
  PatternExplorerRow,
  PatternStatisticsItem,
  PatternStatisticsResult
} from '../../../../Models/client-finance-models/client-finance-models';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';
import { PatternCandidatesBoardComponent } from '../../patterns/pattern-candidates-board/pattern-candidates-board.component';
import { PatternLifecycleFriezeComponent } from '../../patterns/pattern-lifecycle-frieze/pattern-lifecycle-frieze.component';
import { PatternScenarioBranchesComponent } from '../../patterns/pattern-scenario-branches/pattern-scenario-branches.component';
import { PatternTriggerLevelsComponent } from '../../patterns/pattern-trigger-levels/pattern-trigger-levels.component';
import { FinanceSymbolSelectorComponent } from '../../user-finance/finance-symbol-selector/finance-symbol-selector.component';

type ExplorerState = 'empty' | 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-pattern-explorer-page',
  standalone: true,
  imports: [
    CommonModule,
    FinanceSymbolSelectorComponent,
    PatternCandidatesBoardComponent,
    PatternLifecycleFriezeComponent,
    PatternScenarioBranchesComponent,
    PatternTriggerLevelsComponent
  ],
  templateUrl: './pattern-explorer-page.component.html',
  styleUrl: './pattern-explorer-page.component.scss'
})
export class PatternExplorerPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly state = signal<ExplorerState>('empty');
  readonly errorMessage = signal<string | null>(null);
  readonly catalog = signal<PatternCatalogItem[]>([]);
  readonly concepts = signal<AnalysisConcept[]>([]);
  readonly patternStatistics = signal<PatternStatisticsResult | null>(null);

  searchResults: MarketAssetOption[] = [];
  selectedAsset: MarketAssetOption | null = null;
  searchLoading = false;

  evaluateResult: ClientPatternEvaluateResult | null = null;
  selectedPatternId: string | null = null;
  patternDetail: ClientPatternDetail | null = null;
  detailLoading = false;

  constructor() {
    this.clientFinanceService
      .getPatternCatalog()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (catalog) => this.catalog.set(catalog), error: () => { /* catalogue optionnel */ } });

    this.clientFinanceService
      .getAnalysisConcepts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (concepts) => this.concepts.set(concepts), error: () => { /* glossaire optionnel */ } });

    this.clientFinanceService
      .getPatternStatistics()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (result) => this.patternStatistics.set(result), error: () => { /* statistiques observées optionnelles */ } });

    const symbol = this.route.snapshot.queryParamMap.get('symbol');
    if (symbol) {
      this.selectedAsset = new MarketAssetOption({
        Symbol: symbol,
        CompanyName: symbol,
        Market: '',
        Currency: 'EUR',
        LastPrice: 0,
        DayVariationPct: 0
      });
      this.launchEvaluate(symbol);
    }
  }

  onSearchChanged(query: string): void {
    this.searchLoading = true;
    this.clientFinanceService
      .searchAssets(query)
      .pipe(finalize(() => (this.searchLoading = false)))
      .subscribe({
        next: (results) => (this.searchResults = results),
        error: () => { this.searchResults = []; }
      });
  }

  onAssetSelected(asset: MarketAssetOption): void {
    this.selectedAsset = asset;
    this.evaluateResult = null;
    this.selectedPatternId = null;
    this.patternDetail = null;
    this.errorMessage.set(null);
    this.launchEvaluate(asset.Symbol);
  }

  onPatternSelected(patternId: string): void {
    if (!this.evaluateResult) return;
    this.selectedPatternId = patternId;
    this.patternDetail = null;
    this.loadPatternDetail(this.evaluateResult.AnalysisId, patternId);
  }

  onCreateAlert(request: CreateClientAlertRequest): void {
    this.clientFinanceService
      .createAlert(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.toastService.success('Alerte créée.'),
        error: () => { /* toast géré par ApiErrorInterceptor */ }
      });
  }

  /** Fusion catalogue (les 8 patterns) + détection live : détectés en tête, puis par fiabilité. */
  get patternRows(): PatternExplorerRow[] {
    const candidates = this.evaluateResult?.Candidates ?? [];
    const candidateById = new Map(candidates.map((candidate) => [candidate.PatternId.toUpperCase(), candidate]));

    return this.catalog()
      .map((item) => ({
        catalog: item,
        candidate: candidateById.get(item.Id.toUpperCase()) ?? null
      }))
      .sort((a, b) => {
        const detectedDelta = (b.candidate ? 1 : 0) - (a.candidate ? 1 : 0);
        if (detectedDelta !== 0) return detectedDelta;
        return b.catalog.Reliability - a.catalog.Reliability;
      });
  }

  statsFor(patternId: string): PatternStatisticsItem[] {
    return (this.patternStatistics()?.PatternStats ?? [])
      .filter((stat) => stat.PatternId.toUpperCase() === patternId.toUpperCase());
  }

  get hasObservedStatistics(): boolean {
    return (this.patternStatistics()?.PatternStats?.length ?? 0) > 0;
  }

  get srZones(): ClientSupportResistanceZone[] {
    return this.evaluateResult?.SupportResistanceZones ?? [];
  }

  /** Concepts pédagogiques affichés sous le tableau des niveaux clés. */
  get srConcepts(): AnalysisConcept[] {
    const codes = ['support', 'resistance', 'touches', 'strength', 'double_zone'];
    return codes
      .map((code) => this.concepts().find((concept) => concept.Code === code))
      .filter((concept): concept is AnalysisConcept => concept !== undefined);
  }

  private launchEvaluate(symbol: string): void {
    this.state.set('loading');
    this.clientFinanceService
      .evaluatePatterns({ Symbol: symbol, HoldingContext: null })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          if (this.state() === 'loading') this.state.set('ready');
        })
      )
      .subscribe({
        next: (result) => {
          this.evaluateResult = result;
          // On affiche toujours le catalogue complet des patterns : l'état « ready » est systématique,
          // la détection live ne fait qu'enrichir le sous-ensemble réellement repéré.
          this.state.set('ready');
          if (result.Candidates.length > 0) {
            const primary = result.Candidates.find(c => c.IsPrimary) ?? result.Candidates[0];
            this.onPatternSelected(primary.PatternId);
          }
        },
        error: (err: unknown) => {
          this.state.set('error');
          this.errorMessage.set(this.extractErrorMessage(err));
        }
      });
  }

  private extractErrorMessage(err: unknown): string {
    const fallback = 'L\'évaluation des patterns est temporairement indisponible.';
    if (err instanceof HttpErrorResponse) {
      const body = err.error as { message?: string } | string | null;
      if (typeof body === 'object' && body?.message?.trim()) {
        return body.message.trim();
      }
    }
    return fallback;
  }

  private loadPatternDetail(analysisId: string, patternId: string): void {
    this.detailLoading = true;
    this.clientFinanceService
      .getPatternDetail(analysisId, patternId, false)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.detailLoading = false))
      )
      .subscribe({
        next: (detail) => (this.patternDetail = detail),
        error: () => { this.patternDetail = null; }
      });
  }
}
