import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  ClientAnalysisResult,
  ClientDashboardOverview,
  ClientWatchlistItem,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel
} from '../../../Models/client-finance-models/client-finance-models';
import { UserPaths, toCommands } from '../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../services/client-finance.service';
import { PatternCatalogStore } from '../../../services/pattern-catalog.store';

@Component({
  selector: 'app-client-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, PercentPipe, DatePipe, DecimalPipe],
  templateUrl: './client-dashboard.html',
  styleUrl: './client-dashboard.scss'
})
export class ClientDashboardComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly patternCatalogStore = inject(PatternCatalogStore);
  private readonly clientFinanceService = inject(ClientFinanceService);

  private readonly LAST_VISIT_KEY = 'pf_last_dashboard_visit';
  private lastVisitAt: string | null = null;

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;

  overview = new ClientDashboardOverview();
  recentAnalyses: ClientAnalysisResult[] = [];
  watchlist: ClientWatchlistItem[] = [];
  loading = false;

  ngOnInit(): void {
    this.loading = true;

    this.lastVisitAt = localStorage.getItem(this.LAST_VISIT_KEY);
    localStorage.setItem(this.LAST_VISIT_KEY, new Date().toISOString());

    this.patternCatalogStore
      .ensureLoaded()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();

    this.clientFinanceService
      .getDashboardOverview()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => (this.loading = false))
      )
      .subscribe({
        next: (overview) => (this.overview = overview),
        error: () => {
          this.overview = new ClientDashboardOverview();
        }
      });

    this.clientFinanceService
      .getRecentAnalyses(10)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (results) => (this.recentAnalyses = results),
        error: () => {
          this.recentAnalyses = [];
        }
      });

    this.clientFinanceService
      .getWatchlist()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => (this.watchlist = items),
        error: () => {
          this.watchlist = [];
        }
      });
  }

  get topMovers(): ClientWatchlistItem[] {
    return [...this.watchlist]
      .filter((item) => item.DayVariationPct !== 0)
      .sort((a, b) => Math.abs(b.DayVariationPct) - Math.abs(a.DayVariationPct))
      .slice(0, 5);
  }

  get activeSignals(): ClientAnalysisResult[] {
    return this.recentAnalyses.filter((a) => a.IsActionable).slice(0, 5);
  }

  get newSinceLastVisit(): ClientAnalysisResult[] {
    if (!this.lastVisitAt) return [];
    const threshold = new Date(this.lastVisitAt);
    return this.recentAnalyses.filter((a) => new Date(a.PredictedAt) > threshold);
  }

  isNew(analysis: ClientAnalysisResult): boolean {
    if (!this.lastVisitAt) return false;
    return new Date(analysis.PredictedAt) > new Date(this.lastVisitAt);
  }

  get latestInsight(): ClientAnalysisResult | null {
    if (this.recentAnalyses.length === 0) return null;
    return this.recentAnalyses.find((a) => a.IsActionable) ?? this.recentAnalyses[0];
  }

  get latestInsightSummary(): string {
    const insight = this.latestInsight;
    if (!insight) return 'Aucune analyse récente.';
    return `${this.patternCatalogStore.labelFor(insight.Pattern)} sur ${insight.Symbol}, phase ${getPhaseLabel(insight.Phase)}, probabilité ${Math.round(insight.Probability * 100)} %.`;
  }

  getBadgeClass(action: ClientAnalysisResult['RecommendationAction']): string {
    return getRecommendationBadgeClass(action);
  }

  formatRecommendation(action: ClientAnalysisResult['RecommendationAction']): string {
    return getRecommendationLabel(action);
  }

  formatPattern(pattern: ClientAnalysisResult['Pattern']): string {
    return this.patternCatalogStore.labelFor(pattern);
  }

  formatPhase(phase: string): string {
    return getPhaseLabel(phase);
  }

  variationSign(pct: number): string {
    return pct > 0 ? '+' : '';
  }
}
