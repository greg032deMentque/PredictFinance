import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, Input, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GlossaryTermDirective } from '../../../../core/directives/glossary-term.directive';
import {
  ClientAnalysisResult,
  getModelStatusLabel,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel,
  getRiskLevelLabel
} from '../../../../Models/client-finance-models/client-finance-models';
import { PatternCatalogStore } from '../../../../services/pattern-catalog.store';

@Component({
  selector: 'app-finance-analysis-result',
  standalone: true,
  imports: [CommonModule, PercentPipe, DatePipe, GlossaryTermDirective],
  templateUrl: './finance-analysis-result.component.html',
  styleUrl: './finance-analysis-result.component.scss'
})
export class FinanceAnalysisResultComponent {
  private readonly destroyRef = inject(DestroyRef);
  private readonly patternCatalogStore = inject(PatternCatalogStore);

  @Input() loading = false;
  @Input() result: ClientAnalysisResult | null = null;

  constructor() {
    this.patternCatalogStore
      .ensureLoaded()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  get recommendationLabel(): string {
    return getRecommendationLabel(this.result?.RecommendationAction ?? '');
  }

  get recommendationClass(): string {
    return getRecommendationBadgeClass(this.result?.RecommendationAction ?? '');
  }

  get riskLevelLabel(): string {
    return getRiskLevelLabel(this.result?.RiskLevel ?? '');
  }

  get patternLabel(): string {
    return this.patternCatalogStore.labelFor(this.result?.Pattern ?? '');
  }

  get phaseLabel(): string {
    return getPhaseLabel(this.result?.Phase ?? '');
  }

  get modelStatusLabel(): string {
    return getModelStatusLabel(this.result?.ModelStatus ?? '');
  }

  get formattedReason(): string {
    const rawReason = this.result?.RecommendationReason?.trim() ?? '';
    return rawReason.length > 0
      ? rawReason
      : "Aucune justification metier supplementaire n'est disponible.";
  }
}
