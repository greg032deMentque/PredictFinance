import { CommonModule, CurrencyPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ClientPortfolio } from '../../../../Models/client-finance-models/read-models/client-portfolio.model';

@Component({
  selector: 'app-portfolio-pnl-modal',
  standalone: true,
  imports: [CommonModule, CurrencyPipe],
  templateUrl: './portfolio-pnl-modal.component.html'
})
export class PortfolioPnlModalComponent {
  @Input({ required: true }) portfolio!: ClientPortfolio;

  get pnl(): number {
    return this.portfolio.TotalOutstandingAmount - this.portfolio.TotalInvestedAmount;
  }

  get pnlPct(): number {
    const invested = this.portfolio.TotalInvestedAmount;
    return invested > 0 ? (this.pnl / invested) * 100 : 0;
  }
}
