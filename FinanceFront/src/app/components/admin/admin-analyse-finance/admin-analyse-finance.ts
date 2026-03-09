import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { catchError, finalize, forkJoin, map, of } from 'rxjs';
import { ClientAnalysisLaunchRequest, ClientAnalysisResult, MarketAssetOption } from '../../../Models/client-finance-models/client-finance-models';
import { ClientFinanceService } from '../../../services/client-finance.service';
import { ToastService } from '../../../services/toastr.service';
import { FinanceAnalysisResultComponent } from '../../client/user-finance/finance-analysis-result/finance-analysis-result.component';

@Component({
  selector: 'app-admin-analyse-finance',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule, FinanceAnalysisResultComponent],
  templateUrl: './admin-analyse-finance.html',
  styleUrl: './admin-analyse-finance.scss',
})
export class AdminAnalyseFinance implements OnInit {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly searchSeeds = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');

  analyzableAssets: MarketAssetOption[] = [];
  selectedAsset: MarketAssetOption | null = null;
  selectedSymbol: string | null = null;
  analysisResult: ClientAnalysisResult | null = null;

  assetsLoading = false;
  analysisLoading = false;
  assetsLoadError: string | null = null;

  ngOnInit(): void {
    this.loadAnalyzableAssets();
  }

  reloadAssets(): void {
    if (this.assetsLoading) {
      return;
    }

    this.loadAnalyzableAssets();
  }

  onSymbolChanged(symbol: string | null): void {
    this.selectedSymbol = symbol;
    this.selectedAsset = this.analyzableAssets.find((item) => item.symbol === symbol) ?? null;
  }

  runAnalysis(): void {
    if (!this.selectedSymbol || this.analysisLoading) {
      this.toastService.warning('Selectionne une valeur avant de lancer l analyse.');
      return;
    }

    this.analysisLoading = true;

    this.clientFinanceService
      .runAnalysis(new ClientAnalysisLaunchRequest({ symbol: this.selectedSymbol }))
      .pipe(finalize(() => (this.analysisLoading = false)))
      .subscribe({
        next: (result) => {
          this.analysisResult = result;
          this.toastService.success('Analyse terminee.');
        },
        error: () => {
          this.toastService.error("Echec lors de l execution de l analyse.");
        }
      });
  }

  private loadAnalyzableAssets(): void {
    this.assetsLoading = true;
    this.assetsLoadError = null;

    const requests = this.searchSeeds.map((seed) =>
      this.clientFinanceService.searchAssets(seed).pipe(catchError(() => of([])))
    );

    forkJoin(requests)
      .pipe(
        map((resultSets) => resultSets.flat()),
        map((items) => this.getUniqueAssets(items)),
        finalize(() => (this.assetsLoading = false))
      )
      .subscribe({
        next: (assets) => {
          this.analyzableAssets = assets;

          if (this.selectedSymbol && !assets.some((item) => item.symbol === this.selectedSymbol)) {
            this.selectedSymbol = null;
            this.selectedAsset = null;
            this.analysisResult = null;
          }
        },
        error: () => {
          this.analyzableAssets = [];
          this.assetsLoadError = 'Impossible de charger la liste des valeurs analysables.';
        }
      });
  }

  private getUniqueAssets(items: MarketAssetOption[]): MarketAssetOption[] {
    const uniqueBySymbol = new Map<string, MarketAssetOption>();

    for (const item of items) {
      const symbol = item.symbol.trim().toUpperCase();
      if (!symbol || uniqueBySymbol.has(symbol)) {
        continue;
      }

      uniqueBySymbol.set(symbol, new MarketAssetOption({ ...item, symbol }));
    }

    return Array.from(uniqueBySymbol.values()).sort((left, right) => left.symbol.localeCompare(right.symbol));
  }
}
