import { CommonModule, DecimalPipe, PercentPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClientPatternCandidate } from '../../../../Models/client-finance-models/client-pattern-models';

@Component({
  selector: 'app-pattern-candidates-board',
  standalone: true,
  imports: [CommonModule, DecimalPipe, PercentPipe],
  templateUrl: './pattern-candidates-board.component.html',
  styleUrl: './pattern-candidates-board.component.scss'
})
export class PatternCandidatesBoardComponent {
  @Input() candidates: ClientPatternCandidate[] = [];
  @Input() selectedPatternId: string | null = null;

  @Output() selectPattern = new EventEmitter<string>();

  get sortedCandidates(): ClientPatternCandidate[] {
    return [...this.candidates].sort((a, b) => {
      if (a.IsPrimary !== b.IsPrimary) return a.IsPrimary ? -1 : 1;
      return b.Confidence - a.Confidence;
    });
  }

  onSelect(patternId: string): void {
    this.selectPattern.emit(patternId);
  }

  getConfidenceBadgeClass(label: string): string {
    const l = label.toLowerCase();
    if (l.includes('fort') || l.includes('high') || l.includes('élevé')) return 'bg-success-subtle text-success-emphasis';
    if (l.includes('partiel') || l.includes('moyen') || l.includes('medium')) return 'bg-warning-subtle text-warning-emphasis';
    return 'bg-secondary-subtle text-secondary-emphasis';
  }
}
