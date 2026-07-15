import { CommonModule, PercentPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PatternExplorerRow } from '../../../../Models/client-finance-models/client-pattern-models';

@Component({
  selector: 'app-pattern-candidates-board',
  standalone: true,
  imports: [CommonModule, PercentPipe],
  templateUrl: './pattern-candidates-board.component.html',
  styleUrl: './pattern-candidates-board.component.scss'
})
export class PatternCandidatesBoardComponent {
  @Input() rows: PatternExplorerRow[] = [];
  @Input() selectedPatternId: string | null = null;

  @Output() selectPattern = new EventEmitter<string>();

  get detectedRows(): PatternExplorerRow[] {
    return this.rows.filter((row) => row.candidate !== null);
  }

  get otherRows(): PatternExplorerRow[] {
    return this.rows.filter((row) => row.candidate === null);
  }

  onSelect(row: PatternExplorerRow): void {
    if (row.candidate) {
      this.selectPattern.emit(row.catalog.Id);
    }
  }

  getReliabilityBadgeClass(label: string): string {
    const l = label.toLowerCase();
    if (l.includes('fiable')) return 'bg-success-subtle text-success-emphasis';
    if (l.includes('modér')) return 'bg-warning-subtle text-warning-emphasis';
    return 'bg-secondary-subtle text-secondary-emphasis';
  }

  getConfidenceBadgeClass(label: string): string {
    const l = label.toLowerCase();
    if (l.includes('fort') || l.includes('high') || l.includes('élevé')) return 'bg-success-subtle text-success-emphasis';
    if (l.includes('partiel') || l.includes('moyen') || l.includes('medium')) return 'bg-warning-subtle text-warning-emphasis';
    return 'bg-secondary-subtle text-secondary-emphasis';
  }
}
