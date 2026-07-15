import { CommonModule, Location } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ClientMultiSimulationRequest, MarketAssetOption } from '../../../../Models/client-finance-models/client-finance-models';
import type { MultiSimulationDossier } from '../../../../Models/client-finance-models/client-simulation-dossier.model';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';
import { FinanceSimulationComponent } from '../../user-finance/finance-simulation/finance-simulation.component';
import { FinanceSymbolSelectorComponent } from '../../user-finance/finance-symbol-selector/finance-symbol-selector.component';
import { BackButtonComponent } from '../../../shared/back-button/back-button.component';

@Component({ selector: 'app-simulation-page', standalone: true, imports: [CommonModule, RouterLink, BackButtonComponent, FinanceSimulationComponent, FinanceSymbolSelectorComponent], templateUrl: './simulation-page.component.html', styleUrl: './simulation-page.component.scss' })
export class SimulationPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly location = inject(Location);
  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  searchResults: MarketAssetOption[] = [];
  selectedAsset: MarketAssetOption | null = null;
  result: MultiSimulationDossier | null = null;
  searchLoading = false;
  loading = false;
  constructor() { const symbol = this.route.snapshot.queryParamMap.get('symbol'); if (symbol) this.selectedAsset = new MarketAssetOption({ Symbol: symbol, CompanyName: symbol, Market: '', Currency: 'EUR', LastPrice: 0, DayVariationPct: 0 }); }
  onSearchChanged(query: string): void {
    this.searchLoading = true;
    this.clientFinanceService.searchAssets(query).pipe(finalize(() => (this.searchLoading = false))).subscribe({ next: (results) => (this.searchResults = results), error: () => { this.searchResults = []; this.toastService.error('Recherche impossible.'); } });
  }
  goBack(): void { this.location.back(); }
  onAssetSelected(asset: MarketAssetOption): void { this.selectedAsset = asset; }
  launchSimulation(request: ClientMultiSimulationRequest): void {
    this.loading = true;
    this.clientFinanceService.runMultiSimulation(request).pipe(finalize(() => (this.loading = false))).subscribe({ next: (payload) => { this.result = payload; this.toastService.success('Simulation terminée.'); }, error: () => this.toastService.error('Simulation impossible.') });
  }
}
