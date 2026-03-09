import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ClientAnalysisResult } from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-analysis-history',
  standalone: true,
  imports: [CommonModule, DatePipe, PercentPipe],
  templateUrl: './finance-analysis-history.component.html',
  styleUrl: './finance-analysis-history.component.scss'
})
export class FinanceAnalysisHistoryComponent {
  @Input() history: ClientAnalysisResult[] = [];

  getBadgeClass(recommendation: string): string {
    const normalized = recommendation.trim().toLowerCase();

    if (normalized === 'buy' || normalized === 'acheter') {
      return 'text-bg-success';
    }

    if (normalized === 'sell' || normalized === 'vendre') {
      return 'text-bg-danger';
    }

    return 'text-bg-secondary';
  }
}
