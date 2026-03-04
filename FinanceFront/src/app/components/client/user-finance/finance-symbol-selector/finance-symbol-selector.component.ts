import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { MarketAssetOption } from '../../../../Models/client-finance';

@Component({
  selector: 'app-finance-symbol-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule],
  templateUrl: './finance-symbol-selector.component.html',
  styleUrl: './finance-symbol-selector.component.scss'
})
export class FinanceSymbolSelectorComponent implements OnChanges {
  @Input() selectedAsset: MarketAssetOption | null = null;
  @Input() options: MarketAssetOption[] = [];
  @Input() loading = false;

  @Output() searchChanged = new EventEmitter<string>();
  @Output() assetSelected = new EventEmitter<MarketAssetOption>();

  selectedSymbol: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedAsset']) {
      this.selectedSymbol = this.selectedAsset?.symbol ?? null;
    }
  }

  onSearch(event: { term: string }): void {
    this.searchChanged.emit((event?.term ?? '').trim());
  }

  onSelectionChanged(symbol: string | null): void {
    if (!symbol) {
      this.selectedSymbol = null;
      return;
    }

    const asset = this.options.find((item) => item.symbol === symbol);
    if (!asset) return;

    this.assetSelected.emit(asset);
  }
}
