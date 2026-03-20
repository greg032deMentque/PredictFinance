import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  ClientAnalysisResult,
  getModelStatusLabel,
  getPatternLabel,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel,
  getRiskLevelLabel
} from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-analysis-result',
  standalone: true,
  imports: [CommonModule, PercentPipe, DatePipe],
  templateUrl: './finance-analysis-result.component.html',
  styleUrl: './finance-analysis-result.component.scss'
})
export class FinanceAnalysisResultComponent {
  @Input() loading = false;
  @Input() result: ClientAnalysisResult | null = null;

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
    return getPatternLabel(this.result?.Pattern ?? '');
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
