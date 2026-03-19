import { CommonModule, CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  CLIENT_DEFAULT_PATTERN,
  CLIENT_SUPPORTED_PATTERNS,
  type ClientSimulationResult,
  ClientSimulationRequest,
  type ClientSupportedPattern
} from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-simulation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, PercentPipe],
  templateUrl: './finance-simulation.component.html',
  styleUrl: './finance-simulation.component.scss'
})
export class FinanceSimulationComponent {
  private readonly fb = inject(FormBuilder);
  readonly availablePatterns = CLIENT_SUPPORTED_PATTERNS;

  @Input() selectedSymbol = '';
  @Input() loading = false;
  @Input() result: ClientSimulationResult | null = null;

  @Output() launch = new EventEmitter<ClientSimulationRequest>();

  readonly form = this.fb.nonNullable.group({
    pattern: this.fb.nonNullable.control<ClientSupportedPattern>(CLIENT_DEFAULT_PATTERN, [Validators.required]),
    investmentAmount: this.fb.nonNullable.control(1000, [Validators.required, Validators.min(1)]),
    horizonDays: this.fb.nonNullable.control(30, [Validators.required, Validators.min(1), Validators.max(365)])
  });

  get recommendationLabel(): string {
    const normalized = this.result?.Recommendation?.trim().toLowerCase() ?? '';

    if (normalized === 'buy' || normalized === 'acheter') {
      return 'Acheter';
    }

    if (normalized === 'sell' || normalized === 'vendre') {
      return 'Vendre';
    }

    if (normalized === 'hold' || normalized === 'conserver') {
      return 'Conserver';
    }

    return this.result?.Recommendation?.trim() || 'Conserver';
  }

  get recommendationClass(): string {
    const normalized = this.result?.Recommendation?.trim().toLowerCase() ?? '';

    if (normalized === 'buy' || normalized === 'acheter') {
      return 'text-bg-success';
    }

    if (normalized === 'sell' || normalized === 'vendre') {
      return 'text-bg-danger';
    }

    return 'text-bg-secondary';
  }

  get actionableSummary(): string {
    if (!this.result) {
      return '';
    }

    if (this.result.IsActionable) {
      return `Le signal parait exploitable pour le moment. La posture suggeree est ${this.recommendationLabel.toLowerCase()}.`;
    }

    return "Le signal reste indicatif pour l'instant. Il vaut mieux attendre une confirmation supplementaire avant d'agir.";
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
