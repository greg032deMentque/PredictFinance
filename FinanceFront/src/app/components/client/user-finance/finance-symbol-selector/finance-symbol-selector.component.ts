import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAssetOption } from '../../../../Models/client-finance';

@Component({
  selector: 'app-finance-symbol-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './finance-symbol-selector.component.html',
  styleUrl: './finance-symbol-selector.component.scss'
})
export class FinanceSymbolSelectorComponent {
  @Input() selectedAsset: MarketAssetOption | null = null;
  @Input() options: MarketAssetOption[] = [];
  @Input() loading = false;

  @Output() searchChanged = new EventEmitter<string>();
  @Output() assetSelected = new EventEmitter<MarketAssetOption>();

  searchTerm = '';

  onSearchChanged(value: string): void {
    this.searchTerm = value;
    this.searchChanged.emit(value);
  }

  selectAsset(asset: MarketAssetOption): void {
    this.searchTerm = `${asset.symbol} - ${asset.companyName}`;
    this.assetSelected.emit(asset);
  }
}
