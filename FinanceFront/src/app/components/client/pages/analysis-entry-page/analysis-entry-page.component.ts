import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ClientAnalysisLaunchRequest, MarketAssetOption } from '../../../../Models/client-finance-models/client-finance-models';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';
import { FinanceSymbolSelectorComponent } from '../../user-finance/finance-symbol-selector/finance-symbol-selector.component';

@Component({ selector: 'app-analysis-entry-page', standalone: true, imports: [CommonModule, RouterLink, FinanceSymbolSelectorComponent], templateUrl: './analysis-entry-page.component.html', styleUrl: './analysis-entry-page.component.scss' })
export class AnalysisEntryPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  searchResults: MarketAssetOption[] = [];
  selectedAsset: MarketAssetOption | null = null;
  searchLoading = false;
  analysisLoading = false;
  constructor() {
    const symbol = this.route.snapshot.queryParamMap.get('symbol');
    if (symbol) this.selectedAsset = new MarketAssetOption({ Symbol: symbol, CompanyName: symbol, Market: '', Currency: 'EUR', LastPrice: 0, DayVariationPct: 0 });
  }
  onSearchChanged(query: string): void {
    this.searchLoading = true;
    this.clientFinanceService.searchAssets(query).pipe(finalize(() => (this.searchLoading = false))).subscribe({ next: (results) => (this.searchResults = results), error: () => { this.searchResults = []; this.toastService.error('Recherche impossible.'); } });
  }
  onAssetSelected(asset: MarketAssetOption): void { this.selectedAsset = asset; }
  launchAnalysis(): void {
    if (!this.selectedAsset || this.analysisLoading) return;
    this.analysisLoading = true;
    this.clientFinanceService.runAnalysis(new ClientAnalysisLaunchRequest({ Symbol: this.selectedAsset.Symbol })).pipe(finalize(() => (this.analysisLoading = false))).subscribe({ next: (result) => { this.toastService.success('Analyse terminée.'); void this.router.navigate(toCommands(UserPaths.AnalysisDetail(result.Id))); }, error: () => this.toastService.error('Lancement de l’analyse impossible.') });
  }
}
