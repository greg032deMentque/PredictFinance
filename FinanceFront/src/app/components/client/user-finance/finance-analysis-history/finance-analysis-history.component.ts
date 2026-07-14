import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, Input, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ClientAnalysisResult,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel,
  getRiskLevelLabel
} from '../../../../Models/client-finance-models/client-finance-models';
import { PatternCatalogStore } from '../../../../services/pattern-catalog.store';

@Component({
  selector: 'app-finance-analysis-history',
  standalone: true,
  imports: [CommonModule, DatePipe, PercentPipe],
  templateUrl: './finance-analysis-history.component.html',
  styleUrl: './finance-analysis-history.component.scss'
})
export class FinanceAnalysisHistoryComponent {
  private readonly destroyRef = inject(DestroyRef);
  private readonly patternCatalogStore = inject(PatternCatalogStore);

  @Input() history: ClientAnalysisResult[] = [];

  constructor() {
    this.patternCatalogStore
      .ensureLoaded()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  getBadgeClass(action: ClientAnalysisResult['RecommendationAction']): string {
    return getRecommendationBadgeClass(action);
  }

  getRecommendationLabel(action: ClientAnalysisResult['RecommendationAction']): string {
    return getRecommendationLabel(action);
  }

  getPhaseLabel(phase: string): string {
    return getPhaseLabel(phase);
  }

  getRiskLevelLabel(riskLevel: ClientAnalysisResult['RiskLevel']): string {
    return getRiskLevelLabel(riskLevel);
  }

  getPatternLabel(pattern: ClientAnalysisResult['Pattern']): string {
    return this.patternCatalogStore.labelFor(pattern);
  }
}
