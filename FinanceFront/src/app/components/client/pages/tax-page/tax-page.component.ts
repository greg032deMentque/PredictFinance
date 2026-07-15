import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import type { TaxSummary } from '../../../../Models/client-finance-models/tax-summary.model';
import { TaxService } from '../../../../services/tax.service';

const CURRENT_YEAR = new Date().getFullYear();
const MIN_YEAR = CURRENT_YEAR - 5;
const MAX_YEAR = CURRENT_YEAR;

@Component({
  selector: 'app-tax-page',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CurrencyPipe, DatePipe],
  templateUrl: './tax-page.component.html',
  styleUrl: './tax-page.component.scss'
})
export class TaxPageComponent {
  private readonly taxService = inject(TaxService);
  private readonly destroyRef = inject(DestroyRef);

  readonly selectedYear = signal(CURRENT_YEAR);
  readonly summaries = signal<TaxSummary[]>([]);
  readonly loading = signal(false);
  readonly expandedPortfolioId = signal<string | null>(null);

  readonly minYear = MIN_YEAR;
  readonly maxYear = MAX_YEAR;

  readonly totalRealizedPnl = computed(() =>
    this.summaries().reduce((acc, s) => acc + s.TotalRealizedPnl, 0)
  );

  readonly totalEstimatedTax = computed(() =>
    this.summaries().reduce((acc, s) => acc + s.EstimatedTax, 0)
  );

  constructor() {
    this.load();
  }

  prevYear(): void {
    if (this.selectedYear() > MIN_YEAR) {
      this.selectedYear.update(y => y - 1);
      this.load();
    }
  }

  nextYear(): void {
    if (this.selectedYear() < MAX_YEAR) {
      this.selectedYear.update(y => y + 1);
      this.load();
    }
  }

  togglePortfolio(portfolioId: string): void {
    this.expandedPortfolioId.update(id => id === portfolioId ? null : portfolioId);
  }

  pnlClass(value: number): string {
    if (value > 0) return 'text-success';
    if (value < 0) return 'text-danger';
    return 'text-muted';
  }

  private load(): void {
    this.loading.set(true);
    this.summaries.set([]);
    this.expandedPortfolioId.set(null);
    this.taxService.getSummary(this.selectedYear()).pipe(
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: data => this.summaries.set(data),
      error: () => this.summaries.set([])
    });
  }
}
