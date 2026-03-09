import { CommonModule, CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ClientSimulationRequest, ClientSimulationResult } from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-simulation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, PercentPipe],
  templateUrl: './finance-simulation.component.html',
  styleUrl: './finance-simulation.component.scss'
})
export class FinanceSimulationComponent {
  private readonly fb = inject(FormBuilder);
  readonly availablePatterns = ['DOUBLE_TOP'] as const;

  @Input() selectedSymbol = '';
  @Input() loading = false;
  @Input() result: ClientSimulationResult | null = null;

  @Output() launch = new EventEmitter<ClientSimulationRequest>();

  readonly form = this.fb.nonNullable.group({
    pattern: this.fb.nonNullable.control<(typeof this.availablePatterns)[number]>('DOUBLE_TOP', [Validators.required]),
    investmentAmount: this.fb.nonNullable.control(1000, [Validators.required, Validators.min(1)]),
    horizonDays: this.fb.nonNullable.control(30, [Validators.required, Validators.min(1), Validators.max(365)])
  });

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
