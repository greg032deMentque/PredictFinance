import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ClientTransactionCreateRequest } from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-transaction-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './finance-transaction-form.component.html',
  styleUrl: './finance-transaction-form.component.scss'
})
export class FinanceTransactionFormComponent {
  private readonly fb = inject(FormBuilder);

  @Input() submitting = false;
  @Input() selectedSymbol = '';

  @Output() save = new EventEmitter<ClientTransactionCreateRequest>();

  readonly form = this.fb.nonNullable.group({
    transactionType: this.fb.nonNullable.control<'Buy' | 'Sell'>('Buy', [Validators.required]),
    quantity: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0.0001)]),
    unitPrice: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0.0001)]),
    fees: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0)]),
    timestampUtc: this.fb.nonNullable.control(new Date().toISOString().slice(0, 16), [Validators.required])
  });

  submit(): void {
    if (this.form.invalid || this.submitting || this.selectedSymbol.trim().length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();

    this.save.emit(
      new ClientTransactionCreateRequest({
        Symbol: this.selectedSymbol,
        TransactionType: payload.transactionType,
        Quantity: payload.quantity,
        UnitPrice: payload.unitPrice,
        Fees: payload.fees,
        TimestampUtc: new Date(payload.timestampUtc).toISOString()
      })
    );
  }
}
