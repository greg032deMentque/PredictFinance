import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClientLiveQuote, ClientWatchlistItem } from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-watchlist',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe],
  templateUrl: './finance-watchlist.component.html',
  styleUrl: './finance-watchlist.component.scss'
})
export class FinanceWatchlistComponent {
  @Input() watchlist: ClientWatchlistItem[] = [];
  @Input() quote: ClientLiveQuote | null = null;
  @Input() quoteLoading = false;

  @Output() requestQuote = new EventEmitter<string>();
  @Output() removeFromWatchlist = new EventEmitter<string>();

  onSelectSymbol(symbol: string): void {
    this.requestQuote.emit(symbol);
  }

  onRemoveSymbol(symbol: string): void {
    this.removeFromWatchlist.emit(symbol);
  }
}
