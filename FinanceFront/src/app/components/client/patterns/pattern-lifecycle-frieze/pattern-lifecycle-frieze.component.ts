import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ClientPatternConfidenceCriterion } from '../../../../Models/client-finance-models/client-pattern-models';

interface LifecycleStep {
  code: string;
  label: string;
}

const LIFECYCLE_STEPS: LifecycleStep[] = [
  { code: 'Forming', label: 'Formation' },
  { code: 'Monitoring', label: 'Surveillance' },
  { code: 'Confirmed', label: 'Confirmé' },
  { code: 'Completed', label: 'Complété' },
  { code: 'Invalidated', label: 'Invalidé' }
];

const TERMINAL_STEPS = new Set(['Completed', 'Invalidated']);

@Component({
  selector: 'app-pattern-lifecycle-frieze',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pattern-lifecycle-frieze.component.html',
  styleUrl: './pattern-lifecycle-frieze.component.scss'
})
export class PatternLifecycleFriezeComponent {
  @Input() lifecyclePhaseCode = '';
  @Input() detectionStatus = '';
  @Input() validationState = '';
  @Input() invalidationState = '';
  @Input() criteria: ClientPatternConfidenceCriterion[] = [];

  readonly lifecycleSteps = LIFECYCLE_STEPS;

  get mainSteps(): LifecycleStep[] {
    return LIFECYCLE_STEPS.filter(s => s.code !== 'Invalidated');
  }

  // L'étape courante se lit sur le statut de détection (Forming/Monitoring/Confirmed/Completed/Invalidated) ;
  // lifecyclePhaseCode est un code de phase domaine (ex. bull_flag_forming) qui ne matche pas la frise.
  private get currentStepCode(): string {
    if (LIFECYCLE_STEPS.some(s => s.code === this.detectionStatus)) {
      return this.detectionStatus;
    }
    return this.lifecyclePhaseCode;
  }

  isCurrentStep(code: string): boolean {
    return this.currentStepCode === code;
  }

  isPastStep(code: string): boolean {
    const currentIdx = LIFECYCLE_STEPS.findIndex(s => s.code === this.currentStepCode);
    const stepIdx = LIFECYCLE_STEPS.findIndex(s => s.code === code);
    return stepIdx < currentIdx && !TERMINAL_STEPS.has(code);
  }

  getStepClass(code: string): string {
    if (this.isCurrentStep(code)) {
      if (code === 'Invalidated') return 'step-invalidated step-current';
      if (code === 'Completed') return 'step-completed step-current';
      return 'step-current';
    }
    if (this.isPastStep(code)) return 'step-past';
    return 'step-future';
  }

  getCriterionIcon(state: string): string {
    switch (state) {
      case 'met': return '✅';
      case 'partial': return '⚠️';
      case 'absent': return '❌';
      default: return state;
    }
  }

  getCriterionSourceLabel(source: string): string {
    switch (source) {
      case 'DETECTION': return 'Détection';
      case 'VALIDATION': return 'Validation';
      case 'INVALIDATION': return 'Invalidation';
      default: return source;
    }
  }

  get pendingCriteria(): ClientPatternConfidenceCriterion[] {
    return this.criteria.filter(c => c.State !== 'met');
  }
}
