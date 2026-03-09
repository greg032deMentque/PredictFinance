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
}
