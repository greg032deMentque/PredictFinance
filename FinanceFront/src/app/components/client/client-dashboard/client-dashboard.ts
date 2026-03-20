import { CommonModule, CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  ClientAnalysisResult,
  ClientDashboardOverview,
  getPatternLabel,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel
} from '../../../Models/client-finance-models/client-finance-models';
import { UserPaths } from '../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../services/client-finance.service';

@Component({
  selector: 'app-client-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, PercentPipe, DatePipe],
  templateUrl: './client-dashboard.html',
  styleUrl: './client-dashboard.scss'
})
export class ClientDashboardComponent implements OnInit {
  readonly userPaths = UserPaths;

  overview = new ClientDashboardOverview();
  recentAnalyses: ClientAnalysisResult[] = [];
  loading = false;

  constructor(private readonly clientFinanceService: ClientFinanceService) {}

  ngOnInit(): void {
    this.loading = true;

    this.clientFinanceService
      .getDashboardOverview()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (overview) => (this.overview = overview),
        error: () => {
          this.overview = new ClientDashboardOverview();
        }
      });

    this.clientFinanceService.getRecentAnalyses(5).subscribe({
      next: (results) => (this.recentAnalyses = results),
      error: () => {
        this.recentAnalyses = [];
      }
    });
  }

  get dayProfitLossClass(): string {
    return this.overview.DayProfitLoss >= 0 ? 'text-success' : 'text-danger';
  }

  get latestInsight(): ClientAnalysisResult | null {
    if (this.recentAnalyses.length === 0) {
      return null;
    }

    return this.recentAnalyses.find((analysis) => analysis.IsActionable) ?? this.recentAnalyses[0];
  }

  get latestInsightSummary(): string {
    const insight = this.latestInsight;
    if (!insight) {
      return 'Aucune analyse recente pour le moment.';
    }

    return `${getPatternLabel(insight.Pattern)} sur ${insight.Symbol}, phase ${getPhaseLabel(insight.Phase)}, probabilite ${Math.round(insight.Probability * 100)} %.`;
  }

  getBadgeClass(action: ClientAnalysisResult['RecommendationAction']): string {
    return getRecommendationBadgeClass(action);
  }

  formatRecommendation(action: ClientAnalysisResult['RecommendationAction']): string {
    return getRecommendationLabel(action);
  }

  formatPattern(pattern: ClientAnalysisResult['Pattern']): string {
    return getPatternLabel(pattern);
  }
}
