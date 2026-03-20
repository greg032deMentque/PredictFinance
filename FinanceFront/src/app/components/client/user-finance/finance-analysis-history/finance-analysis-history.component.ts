import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  ClientAnalysisResult,
  getPatternLabel,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel,
  getRiskLevelLabel
} from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-analysis-history',
  standalone: true,
  imports: [CommonModule, DatePipe, PercentPipe],
  templateUrl: './finance-analysis-history.component.html',
  styleUrl: './finance-analysis-history.component.scss'
})
export class FinanceAnalysisHistoryComponent {
  @Input() history: ClientAnalysisResult[] = [];

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
    return getPatternLabel(pattern);
  }
}
