import { CommonModule, CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  type ClientSimulationResult,
  ClientSimulationRequest,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel,
  getRiskLevelLabel
} from '../../../../Models/client-finance-models/client-finance-models';
import { PatternCatalogStore } from '../../../../services/pattern-catalog.store';

@Component({
  selector: 'app-finance-simulation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, PercentPipe],
  templateUrl: './finance-simulation.component.html',
  styleUrl: './finance-simulation.component.scss'
})
export class FinanceSimulationComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly patternCatalogStore = inject(PatternCatalogStore);

  readonly availablePatterns = this.patternCatalogStore.items;

  @Input() selectedSymbol = '';
  @Input() loading = false;
  @Input() result: ClientSimulationResult | null = null;

  @Output() launch = new EventEmitter<ClientSimulationRequest>();

  readonly form = this.fb.nonNullable.group({
    pattern: this.fb.nonNullable.control('', [Validators.required]),
    investmentAmount: this.fb.nonNullable.control(1000, [Validators.required, Validators.min(1)]),
    horizonDays: this.fb.nonNullable.control(30, [Validators.required, Validators.min(1), Validators.max(365)])
  });

  ngOnInit(): void {
    this.patternCatalogStore
      .ensureLoaded()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((patterns) => {
        const selectedPattern = this.form.controls.pattern.value;
        const hasSelectedPattern = patterns.some((pattern) => pattern.Id === selectedPattern);

        if (!hasSelectedPattern) {
          this.form.controls.pattern.setValue(patterns[0]?.Id ?? '');
        }
      });
  }

  get recommendationLabel(): string {
    return getRecommendationLabel(this.result?.RecommendationAction ?? '');
  }

  get recommendationClass(): string {
    return getRecommendationBadgeClass(this.result?.RecommendationAction ?? '');
  }

  get riskLevelLabel(): string {
    return getRiskLevelLabel(this.result?.RiskLevel ?? '');
  }

  get phaseLabel(): string {
    return getPhaseLabel(this.result?.Phase ?? '');
  }

  get actionableSummary(): string {
    if (!this.result) {
      return '';
    }

    if (this.result.IsActionable) {
      return `Le conseil produit retient actuellement une posture ${this.recommendationLabel.toLowerCase()}.`;
    }

    return "Le scenario reste informatif pour l'instant. Aucune action immediate n'est retenue.";
  }

  get performanceToneClass(): string {
    if (!this.result) {
      return '';
    }

    if (this.result.EstimatedReturnAmount > 0) {
      return 'text-success';
    }

    if (this.result.EstimatedReturnAmount < 0) {
      return 'text-danger';
    }

    return 'text-body';
  }

  submit(): void {
    if (this.form.invalid || this.loading || this.selectedSymbol.trim().length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();

    this.launch.emit(
      new ClientSimulationRequest({
        Symbol: this.selectedSymbol,
        Pattern: payload.pattern,
        InvestmentAmount: payload.investmentAmount,
        HorizonDays: payload.horizonDays
      })
    );
  }
}
