import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ClientPortfolioPosition } from '../../../../Models/client-finance-models/read-models/client-portfolio.model';

@Component({
  selector: 'app-position-pnl-modal',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DecimalPipe],
  templateUrl: './position-pnl-modal.component.html'
})
export class PositionPnlModalComponent {
  @Input() position: ClientPortfolioPosition | null = null;

  get investedAmount(): number {
    if (!this.position) return 0;
    return this.position.AverageCost * this.position.QuantityHeld;
  }

  get currentPriceEur(): number {
    if (!this.position) return 0;
    return this.position.CurrentPriceNative * this.position.ForexRateUsed;
  }

  get pnl(): number {
    return this.position ? this.position.OutstandingAmount - this.investedAmount : 0;
  }

  get pnlPct(): number {
    return this.investedAmount > 0 ? (this.pnl / this.investedAmount) * 100 : 0;
  }

  get hasConversion(): boolean {
    return !!this.position && this.position.Currency !== 'EUR' && this.position.ForexRateUsed !== 1;
  }
}
