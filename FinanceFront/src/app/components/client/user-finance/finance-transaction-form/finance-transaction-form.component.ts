import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnChanges, Output, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ClientTransactionCreateRequest, MarketAssetOption } from '../../../../Models/client-finance-models/client-finance-models';
import { UserPortfolioViewModel } from '../../../../Models/client-finance-models/user-portfolio.model';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { FinanceSymbolSelectorComponent } from '../finance-symbol-selector/finance-symbol-selector.component';

@Component({
  selector: 'app-finance-transaction-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FinanceSymbolSelectorComponent],
  templateUrl: './finance-transaction-form.component.html',
  styleUrl: './finance-transaction-form.component.scss'
})
export class FinanceTransactionFormComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly destroyRef = inject(DestroyRef);

  @Input() submitting = false;
  @Input() selectedSymbol = '';
  @Input() portfolios: UserPortfolioViewModel[] = [];
  @Input() hidePortfolioSelector = false;
  @Input() trendingOptions: MarketAssetOption[] = [];

  @Output() save = new EventEmitter<ClientTransactionCreateRequest>();

  readonly searchResults = signal<MarketAssetOption[]>([]);
  readonly searchLoading = signal(false);
  readonly internalAsset = signal<MarketAssetOption | null>(null);

  get effectiveSymbol(): string {
    return this.internalAsset()?.Symbol ?? this.selectedSymbol;
  }

  readonly form = this.fb.nonNullable.group({
    portfolioId: this.fb.nonNullable.control('', [Validators.required]),
    transactionType: this.fb.nonNullable.control<'Buy' | 'Sell'>('Buy', [Validators.required]),
    quantity: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0.0001)]),
    unitPrice: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0.0001)]),
    fees: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0)]),
    timestampUtc: this.fb.nonNullable.control(new Date().toISOString().slice(0, 16), [Validators.required])
  });

  ngOnChanges(): void {
    if (this.portfolios.length === 1 && !this.form.controls.portfolioId.value) {
      this.form.controls.portfolioId.setValue(this.portfolios[0].Id);
    }
    if (this.selectedSymbol) {
      this.internalAsset.set(null);
      this.searchResults.set([]);
    }
  }

  onSearchChanged(query: string): void {
    this.searchLoading.set(true);
    this.clientFinanceService.searchAssets(query)
      .pipe(finalize(() => this.searchLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (results) => this.searchResults.set(results),
        error: () => this.searchResults.set([])
      });
  }

  onAssetSelected(asset: MarketAssetOption): void {
    this.internalAsset.set(asset);
  }

  clearSymbol(): void {
    this.internalAsset.set(null);
    this.searchResults.set([]);
  }

  resetForm(): void {
    const portfolioId = this.form.controls.portfolioId.value;
    this.form.reset({
      portfolioId,
      transactionType: 'Buy',
      quantity: 0,
      unitPrice: 0,
      fees: 0,
      timestampUtc: new Date().toISOString().slice(0, 16)
    });
    this.internalAsset.set(null);
    this.searchResults.set([]);
  }

  submit(): void {
    const symbol = this.effectiveSymbol;
    if (this.form.invalid || this.submitting || !symbol) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();

    this.save.emit(
      new ClientTransactionCreateRequest({
        Symbol: symbol,
        PortfolioId: payload.portfolioId,
        TransactionType: payload.transactionType,
        Quantity: payload.quantity,
        UnitPrice: payload.unitPrice,
        Fees: payload.fees,
        TimestampUtc: new Date(payload.timestampUtc).toISOString()
      })
    );
  }
}
