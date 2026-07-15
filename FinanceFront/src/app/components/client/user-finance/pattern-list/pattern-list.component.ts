import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { AnalysisPattern } from '../../../../Models/client-finance-models/client-analysis-dossier.model';

@Component({
  selector: 'app-pattern-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pattern-list.component.html',
  styleUrl: './pattern-list.component.scss'
})
export class PatternListComponent {
  @Input() patterns: AnalysisPattern[] = [];
  @Input() selectedPatternId: string | null = null;

  @Output() patternSelected = new EventEmitter<AnalysisPattern>();

  get sortedPatterns(): AnalysisPattern[] {
    return [...this.patterns].sort((a, b) => b.ConfidenceScore - a.ConfidenceScore);
  }

  selectPattern(pattern: AnalysisPattern): void {
    this.patternSelected.emit(pattern);
  }

  isSelected(pattern: AnalysisPattern): boolean {
    return this.selectedPatternId === pattern.PatternId;
  }

  getRiskBadgeClass(riskLevel: string): string {
    switch (riskLevel.toLowerCase()) {
      case 'low':
        return 'chip chip-success';
      case 'moderate':
        return 'chip chip-warning';
      case 'high':
        return 'chip chip-danger';
      default:
        return 'chip chip-navy';
    }
  }

  getRiskLabel(riskLevel: string): string {
    switch (riskLevel.toLowerCase()) {
      case 'low':
        return 'Faible';
      case 'moderate':
        return 'Modéré';
      case 'high':
        return 'Élevé';
      default:
        return riskLevel || 'Information';
    }
  }

  getRiskIcon(riskLevel: string): string {
    switch (riskLevel.toLowerCase()) {
      case 'low':
        return 'bi-shield-check';
      case 'moderate':
        return 'bi-shield-exclamation';
      case 'high':
        return 'bi-shield-x';
      default:
        return 'bi-shield';
    }
  }

  getScoreBarWidth(score: number): number {
    return Math.max(0, Math.min(100, Math.round(score * 100)));
  }
}
