import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe, Location } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import {
  ClientInstrumentDetail,
  ClientInstrumentHistoryPage
} from '../../../../Models/client-finance-models/client-finance-models';
import { ClientAnalysisLaunchRequest } from '../../../../Models/client-finance-models/client-analysis-launch-request.model';
import type { AnalysisDossier } from '../../../../Models/client-finance-models/client-analysis-dossier.model';
import type { TechnicalIndicators } from '../../../../Models/client-finance-models/technical-indicators.model';
import type { IInstrumentFundamentals } from '../../../../Models/client-finance-models/instrument-fundamentals.model';
import type { IFundamentalScoreResult } from '../../../../Models/client-finance-models/fundamental-score.model';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { TechnicalIndicatorsService } from '../../../../services/technical-indicators.service';
import { FundamentalsService } from '../../../../services/fundamentals.service';
import { FinanceAnalysisResultComponent } from '../../user-finance/finance-analysis-result/finance-analysis-result.component';
import { TechnicalIndicatorsPanelComponent } from '../../user-finance/technical-indicators-panel/technical-indicators-panel.component';
import { FundamentalsPanelComponent } from '../../user-finance/fundamentals-panel/fundamentals-panel.component';
import { BackButtonComponent } from '../../../shared/back-button/back-button.component';

const HISTORY_PAGE_SIZE = 10;

@Component({
  selector: 'app-instrument-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, DecimalPipe, DatePipe, BackButtonComponent, FinanceAnalysisResultComponent, TechnicalIndicatorsPanelComponent, FundamentalsPanelComponent],
  templateUrl: './instrument-detail-page.component.html',
  styleUrl: './instrument-detail-page.component.scss'
})
export class InstrumentDetailPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly technicalIndicatorsService = inject(TechnicalIndicatorsService);
  private readonly fundamentalsService = inject(FundamentalsService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly location = inject(Location);

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;

  readonly detail = signal<ClientInstrumentDetail | null>(null);
  readonly history = signal<ClientInstrumentHistoryPage | null>(null);
  readonly dossier = signal<AnalysisDossier | null>(null);
  readonly indicators = signal<TechnicalIndicators | null>(null);
  readonly fundamentals = signal<IInstrumentFundamentals | null>(null);
  readonly score = signal<IFundamentalScoreResult | null>(null);
  readonly loading = signal(false);
  readonly historyLoading = signal(false);
  readonly dossierLoading = signal(false);
  readonly indicatorsLoading = signal(false);
  readonly fundamentalsLoading = signal(false);
  readonly scoreLoading = signal(false);
  readonly historyError = signal<string | null>(null);
  readonly instrumentNotFound = signal(false);
  readonly activeInstrumentTab = signal<'analyse' | 'indicateurs' | 'fondamentaux'>('analyse');

  private indicatorsLoaded = false;
  private fundamentalsLoaded = false;
  private symbol = '';
  private historyPage = 1;

  constructor() {
    const sym = this.route.snapshot.paramMap.get('symbol');
    if (sym) {
      this.symbol = sym;
      this.loadInstrument(sym);
      this.loadHistory();
    }
  }

  get hasPrevHistory(): boolean {
    return (this.history()?.Page ?? 1) > 1;
  }

  get hasNextHistory(): boolean {
    const h = this.history();
    if (!h) return false;
    return h.Page < Math.ceil(h.Total / HISTORY_PAGE_SIZE);
  }

  goBack(): void {
    this.location.back();
  }

  switchTab(tab: 'analyse' | 'indicateurs' | 'fondamentaux'): void {
    this.activeInstrumentTab.set(tab);
    if (tab === 'indicateurs' && !this.indicatorsLoaded && this.symbol) {
      this.loadIndicators();
    }
    if (tab === 'fondamentaux' && !this.fundamentalsLoaded && this.symbol) {
      this.loadFundamentals();
    }
  }

  launchAnalysis(): void {
    if (this.dossierLoading() || !this.symbol) return;
    this.dossierLoading.set(true);
    this.clientFinanceService
      .runAnalysis(new ClientAnalysisLaunchRequest({ Symbol: this.symbol }))
      .pipe(finalize(() => this.dossierLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (d) => this.dossier.set(d), error: () => this.dossier.set(null) });
  }

  goToPrevHistory(): void {
    if (!this.hasPrevHistory) return;
    this.historyPage--;
    this.loadHistory();
  }

  goToNextHistory(): void {
    if (!this.hasNextHistory) return;
    this.historyPage++;
    this.loadHistory();
  }

  private loadFundamentals(): void {
    this.fundamentalsLoading.set(true);
    this.fundamentalsService.getFundamentals(this.symbol).pipe(
      finalize(() => this.fundamentalsLoading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (data) => {
        this.fundamentals.set(data);
        this.fundamentalsLoaded = true;
      },
      error: () => this.fundamentals.set(null)
    });

    this.loadFundamentalScore();
  }

  private loadFundamentalScore(): void {
    this.scoreLoading.set(true);
    this.fundamentalsService.getScore([this.symbol]).pipe(
      finalize(() => this.scoreLoading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response) => {
        const result = response.Results.find(r => r.Symbol === this.symbol) ?? null;
        this.score.set(result);
      },
      error: () => this.score.set(null)
    });
  }

  private loadIndicators(): void {
    this.indicatorsLoading.set(true);
    this.technicalIndicatorsService.getIndicators(this.symbol).pipe(
      finalize(() => this.indicatorsLoading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (data) => {
        this.indicators.set(data);
        this.indicatorsLoaded = true;
      },
      error: () => this.indicators.set(null)
    });
  }

  private loadInstrument(symbol: string): void {
    this.loading.set(true);
    this.clientFinanceService.getInstrumentDetail(symbol).pipe(
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (payload) => this.detail.set(payload),
      error: (err: HttpErrorResponse) => {
        this.detail.set(null);
        if (err.status === 404) {
          this.instrumentNotFound.set(true);
        }
      }
    });
  }

  private loadHistory(): void {
    this.historyLoading.set(true);
    this.historyError.set(null);

    this.clientFinanceService.getInstrumentHistory(this.symbol, {
      page: this.historyPage,
      pageSize: HISTORY_PAGE_SIZE,
      sortDirection: 'desc'
    }).pipe(
      finalize(() => this.historyLoading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (payload) => this.history.set(payload),
      error: (err: HttpErrorResponse) => {
        this.history.set(null);
        if (err.status === 404) {
          this.historyError.set('Cet instrument n\'est pas dans votre liste de suivi.');
        } else {
          this.historyError.set('Chargement de l\'historique instrument impossible.');
        }
      }
    });
  }
}
