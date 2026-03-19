import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ClientAnalysisResult } from '../../../../Models/client-finance-models/client-finance-models';

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
    return this.toFrenchRecommendation(this.result?.Recommendation ?? '');
  }

  get formattedReason(): string {
    const rawReason = this.result?.Reason?.trim() ?? '';
    if (rawReason.length === 0) {
      return "L'analyse ne contient pas encore de detail supplementaire.";
    }

    const reasonMatch = /(?:^|;)Reason=(.+)$/i.exec(rawReason);
    if (reasonMatch?.[1]) {
      return reasonMatch[1].trim();
    }

    return rawReason;
  }

  get recommendationClass(): string {
    if (!this.result) {
      return 'text-bg-secondary';
    }

    const normalized = this.result.Recommendation.trim().toLowerCase();

    if (normalized === 'buy' || normalized === 'acheter') {
      return 'text-bg-success';
    }

    if (normalized === 'sell' || normalized === 'vendre') {
      return 'text-bg-danger';
    }

    return 'text-bg-secondary';
  }

  private toFrenchRecommendation(recommendation: string): string {
    const normalized = recommendation.trim().toLowerCase();

    if (normalized === 'buy' || normalized === 'acheter') {
      return 'Acheter';
    }

    if (normalized === 'sell' || normalized === 'vendre') {
      return 'Vendre';
    }

    if (normalized === 'hold' || normalized === 'conserver') {
      return 'Conserver';
    }

    return recommendation.trim() || 'Information';
  }
}
