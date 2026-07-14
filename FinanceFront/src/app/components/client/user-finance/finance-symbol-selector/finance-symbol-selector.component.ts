import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { MarketAssetOption } from '../../../../Models/client-finance-models/client-finance-models';

@Component({
  selector: 'app-finance-symbol-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule],
  templateUrl: './finance-symbol-selector.component.html',
  styleUrl: './finance-symbol-selector.component.scss'
})
export class FinanceSymbolSelectorComponent implements OnChanges, OnInit {
  @Input() selectedAsset: MarketAssetOption | null = null;
  @Input() options: MarketAssetOption[] = [];
  @Input() loading = false;
  @Input() peaFilterVisible = false;
  @Input() flat = false;

  @Output() searchChanged = new EventEmitter<string>();
  @Output() assetSelected = new EventEmitter<MarketAssetOption>();
  @Output() peaEligibleOnlyChanged = new EventEmitter<boolean>();

  selectedSymbol: string | null = null;
  peaEligibleOnly = false;

  /** Recherche serveur pure : ng-select pousse le terme saisi ici via [typeahead],
   *  ce qui desactive son filtrage client (sinon il masque les resultats dont le
   *  Symbol ne matche pas le terme, ex. "Microsoft" -> MSFT). */
  readonly searchInput$ = new Subject<string>();
  private readonly destroyRef = inject(DestroyRef);
  private currentTerm = '';

  ngOnInit(): void {
    this.searchInput$
      .pipe(
        debounceTime(200),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((term) => {
        this.currentTerm = (term ?? '').trim();
        this.searchChanged.emit(this.currentTerm);
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedAsset']) {
      this.selectedSymbol = this.selectedAsset?.Symbol ?? null;
    }
  }

  onSelectionChanged(item: MarketAssetOption | null): void {
    if (!item) {
      this.selectedSymbol = null;
      return;
    }
    this.assetSelected.emit(item);
  }

  onTogglePea(): void {
    this.peaEligibleOnly = !this.peaEligibleOnly;
    this.peaEligibleOnlyChanged.emit(this.peaEligibleOnly);
    this.searchChanged.emit(this.currentTerm);
  }

  onDropdownOpened(): void {
    this.searchChanged.emit(this.currentTerm);
  }
}
