import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ClientScenarioBranch } from '../../../../Models/client-finance-models/client-pattern-models';

@Component({
  selector: 'app-pattern-scenario-branches',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './pattern-scenario-branches.component.html',
  styleUrl: './pattern-scenario-branches.component.scss'
})
export class PatternScenarioBranchesComponent {
  @Input() branches: ClientScenarioBranch[] = [];

  getDirectionIcon(direction: 'Up' | 'Down'): string {
    return direction === 'Up' ? 'bi-arrow-up-circle-fill' : 'bi-arrow-down-circle-fill';
  }

  getDirectionClass(direction: 'Up' | 'Down'): string {
    return direction === 'Up' ? 'text-primary' : 'text-secondary';
  }

  getResultingStateBadgeClass(state: 'Confirmed' | 'Invalidated'): string {
    return state === 'Confirmed'
      ? 'bg-success-subtle text-success-emphasis'
      : 'bg-danger-subtle text-danger-emphasis';
  }

  getResultingStateLabel(state: 'Confirmed' | 'Invalidated'): string {
    return state === 'Confirmed' ? 'Confirme' : 'Invalide';
  }
}
