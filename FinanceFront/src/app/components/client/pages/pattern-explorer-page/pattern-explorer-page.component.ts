import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';
import {
  ClientPatternCandidate,
  ClientPatternDetail,
  ClientPatternEvaluateResult,
  CreateClientAlertRequest,
  MarketAssetOption
} from '../../../../Models/client-finance-models/client-finance-models';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';
import { PatternCandidatesBoardComponent } from '../../patterns/pattern-candidates-board/pattern-candidates-board.component';
import { PatternLifecycleFriezeComponent } from '../../patterns/pattern-lifecycle-frieze/pattern-lifecycle-frieze.component';
import { PatternScenarioBranchesComponent } from '../../patterns/pattern-scenario-branches/pattern-scenario-branches.component';
import { PatternTriggerLevelsComponent } from '../../patterns/pattern-trigger-levels/pattern-trigger-levels.component';
import { FinanceSymbolSelectorComponent } from '../../user-finance/finance-symbol-selector/finance-symbol-selector.component';

type ExplorerState = 'empty' | 'loading' | 'error' | 'no-pattern' | 'ready';

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

  searchResults: MarketAssetOption[] = [];
  selectedAsset: MarketAssetOption | null = null;
  searchLoading = false;

  evaluateResult: ClientPatternEvaluateResult | null = null;
  selectedPatternId: string | null = null;
  patternDetail: ClientPatternDetail | null = null;
  detailLoading = false;

  constructor() {
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

  get candidates(): ClientPatternCandidate[] {
    return this.evaluateResult?.Candidates ?? [];
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
          if (result.Candidates.length === 0) {
            this.state.set('no-pattern');
          } else {
            this.state.set('ready');
            const primary = result.Candidates.find(c => c.IsPrimary) ?? result.Candidates[0];
            this.onPatternSelected(primary.PatternId);
          }
        },
        error: () => {
          this.state.set('error');
          this.errorMessage.set('L\'évaluation des patterns est temporairement indisponible.');
        }
      });
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
