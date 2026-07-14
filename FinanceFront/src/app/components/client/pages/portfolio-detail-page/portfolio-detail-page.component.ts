import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import * as XLSX from 'xlsx';
import {
  ClientPortfolio,
  ClientPortfolioPosition,
  ClientTransactionCreateRequest,
  ClientTransactionItem
} from '../../../../Models/client-finance-models/client-finance-models';
import { PortfolioType, UserPortfolioViewModel, getPortfolioTypeLabel } from '../../../../Models/client-finance-models/user-portfolio.model';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { PortfolioService } from '../../../../services/portfolio.service';
import { ToastService } from '../../../../services/toastr.service';
import { ConfirmModalComponent } from '../../../shared/confirm-modal/confirm-modal.component';
import { FinanceTransactionFormComponent } from '../../user-finance/finance-transaction-form/finance-transaction-form.component';
import { PortfolioDonutChartComponent } from '../../user-finance/portfolio-donut-chart/portfolio-donut-chart.component';

@Component({
  selector: 'app-portfolio-detail-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    CurrencyPipe,
    DecimalPipe,
    DatePipe,
    ReactiveFormsModule,
    FinanceTransactionFormComponent,
    PortfolioDonutChartComponent,
    ConfirmModalComponent
  ],
  templateUrl: './portfolio-detail-page.component.html',
  styleUrl: './portfolio-detail-page.component.scss'
})
export class PortfolioDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly portfolioService = inject(PortfolioService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('importInput') importInputRef!: ElementRef<HTMLInputElement>;

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  readonly getPortfolioTypeLabel = getPortfolioTypeLabel;

  portfolioId = '';
  portfolioMeta = signal<UserPortfolioViewModel | null>(null);
  portfolio = signal<ClientPortfolio>({ Positions: [], TotalInvestedAmount: 0, TotalOutstandingAmount: 0, OpenPositionCount: 0 });
  transactions = signal<ClientTransactionItem[]>([]);
  selectedSymbol = signal('');

  loading = signal(false);
  transactionLoading = signal(false);
  formLoading = signal(false);
  importLoading = signal(false);
  portfolioError = signal<string | null>(null);
  portfolioToDeleteId = signal<string | null>(null);
  transactionToDeleteId = signal<string | null>(null);
  renamingPortfolioId = signal<string | null>(null);
  renameNameError = signal<string | null>(null);

  readonly renameForm = this.fb.nonNullable.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)])
  });

  get totalPnl(): number {
    return this.portfolio().TotalOutstandingAmount - this.portfolio().TotalInvestedAmount;
  }

  get totalPnlPct(): number {
    const invested = this.portfolio().TotalInvestedAmount;
    return invested > 0 ? ((this.portfolio().TotalOutstandingAmount / invested) - 1) * 100 : 0;
  }

  positionPnl(p: ClientPortfolioPosition): number {
    return p.OutstandingAmount - p.AverageCost * p.QuantityHeld;
  }

  positionPnlPct(p: ClientPortfolioPosition): number {
    const cost = p.AverageCost * p.QuantityHeld;
    return cost > 0 ? ((p.OutstandingAmount / cost) - 1) * 100 : 0;
  }

  get portfolioAsList(): UserPortfolioViewModel[] {
    const meta = this.portfolioMeta();
    return meta ? [meta] : [];
  }

  ngOnInit(): void {
    this.portfolioId = this.route.snapshot.paramMap.get('portfolioId') ?? '';
    if (!this.portfolioId) {
      void this.router.navigate(toCommands(UserPaths.Portfolio));
      return;
    }
    this.loadPortfolioMeta();
    this.loadPortfolio();
    this.loadTransactions();
  }

  private loadPortfolioMeta(): void {
    this.portfolioService.getPortfolios()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (portfolios) => {
          const meta = portfolios.find(p => p.Id === this.portfolioId) ?? null;
          this.portfolioMeta.set(meta);
          if (!meta) void this.router.navigate(toCommands(UserPaths.Portfolio));
        },
        error: () => void this.router.navigate(toCommands(UserPaths.Portfolio))
      });
  }

  private loadPortfolio(): void {
    this.loading.set(true);
    this.portfolioError.set(null);
    this.clientFinanceService.getPortfolio(this.portfolioId)
      .pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (payload) => this.portfolio.set(payload),
        error: (err: HttpErrorResponse) => {
          this.portfolio.set({ Positions: [], TotalInvestedAmount: 0, TotalOutstandingAmount: 0, OpenPositionCount: 0 });
          if (err.status === 404) this.portfolioError.set('Ce portefeuille est introuvable.');
        }
      });
  }

  private loadTransactions(): void {
    this.clientFinanceService.getTransactions(25, this.portfolioId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => this.transactions.set(items),
        error: () => this.transactions.set([])
      });
  }

  onOpenTransaction(symbol: string): void {
    this.selectedSymbol.set(symbol);
    setTimeout(() => document.getElementById('transaction-section')?.scrollIntoView({ behavior: 'smooth' }), 50);
  }

  onSaveTransaction(request: ClientTransactionCreateRequest): void {
    this.transactionLoading.set(true);
    this.clientFinanceService.registerTransaction(request)
      .pipe(finalize(() => this.transactionLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (transaction) => {
          this.transactions.set([transaction, ...this.transactions()].slice(0, 50));
          this.toastService.success('Transaction enregistrée.');
          this.selectedSymbol.set('');
          this.loadPortfolio();
        },
        error: () => {}
      });
  }

  requestDeleteTransaction(id: string): void { this.transactionToDeleteId.set(id); }

  confirmDeleteTransaction(): void {
    const id = this.transactionToDeleteId();
    if (!id || this.transactionLoading()) return;
    this.transactionLoading.set(true);
    this.clientFinanceService.deleteTransaction(id)
      .pipe(finalize(() => this.transactionLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.transactions.set(this.transactions().filter(t => t.Id !== id));
          this.transactionToDeleteId.set(null);
          this.toastService.success('Transaction supprimée.');
          this.loadPortfolio();
        },
        error: () => this.transactionToDeleteId.set(null)
      });
  }

  startRename(): void {
    const meta = this.portfolioMeta();
    if (!meta) return;
    this.renamingPortfolioId.set(meta.Id);
    this.renameNameError.set(null);
    this.renameForm.reset({ name: meta.Name });
  }

  cancelRename(): void {
    this.renamingPortfolioId.set(null);
    this.renameNameError.set(null);
  }

  submitRename(): void {
    this.renameNameError.set(null);
    if (this.renameForm.invalid || this.formLoading()) { this.renameForm.markAllAsTouched(); return; }
    const { name } = this.renameForm.getRawValue();
    this.formLoading.set(true);
    this.portfolioService.renamePortfolio(this.portfolioId, { Name: name })
      .pipe(finalize(() => this.formLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.portfolioMeta.set(updated);
          this.renamingPortfolioId.set(null);
          this.toastService.success('Portefeuille renommé.');
        },
        error: (err) => {
          if (err?.status === 409) this.renameNameError.set('Ce nom est déjà utilisé.');
        }
      });
  }

  requestDeletePortfolio(): void { this.portfolioToDeleteId.set(this.portfolioId); }

  confirmDeletePortfolio(): void {
    if (!this.portfolioToDeleteId() || this.formLoading()) return;
    this.formLoading.set(true);
    this.portfolioService.deletePortfolio(this.portfolioId)
      .pipe(finalize(() => this.formLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.portfolioToDeleteId.set(null);
          this.toastService.success('Portefeuille supprimé.');
          void this.router.navigate(toCommands(UserPaths.Portfolio));
        },
        error: () => this.portfolioToDeleteId.set(null)
      });
  }

  // ── Export Excel ─────────────────────────────────────────────────────────────

  exportPositionsExcel(): void {
    const meta = this.portfolioMeta();
    const rows = this.portfolio().Positions.map(p => ({
      Symbole: p.Instrument.Symbol,
      Nom: p.Instrument.DisplayName,
      Quantite: p.QuantityHeld,
      'Prix moyen': p.AverageCost,
      Encours: p.OutstandingAmount,
      'PnL (EUR)': this.positionPnl(p),
      'PnL (%)': Number(this.positionPnlPct(p).toFixed(2))
    }));
    const ws = XLSX.utils.json_to_sheet(rows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Positions');
    XLSX.writeFile(wb, `${meta?.Name ?? 'portfolio'}-positions.xlsx`);
  }

  exportTransactionsExcel(): void {
    const meta = this.portfolioMeta();
    const rows = this.transactions().map(t => ({
      Symbole: t.Symbol,
      Type: t.TransactionType,
      Quantite: t.Quantity,
      'Prix unitaire': t.UnitPrice,
      Frais: t.Fees,
      'Montant net': t.NetAmount,
      Date: new Date(t.TimestampUtc).toLocaleDateString('fr-FR')
    }));
    const ws = XLSX.utils.json_to_sheet(rows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Transactions');
    XLSX.writeFile(wb, `${meta?.Name ?? 'portfolio'}-transactions.xlsx`);
  }

  // ── Import Excel ─────────────────────────────────────────────────────────────

  triggerImport(): void {
    this.importInputRef?.nativeElement.click();
  }

  onImportFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    (event.target as HTMLInputElement).value = '';

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer);
        const wb = XLSX.read(data, { type: 'array' });
        const ws = wb.Sheets[wb.SheetNames[0]];
        const rows = XLSX.utils.sheet_to_json<Record<string, unknown>>(ws);
        this.processImportRows(rows);
      } catch {
        this.toastService.error('Fichier Excel invalide ou illisible.');
      }
    };
    reader.readAsArrayBuffer(file);
  }

  private processImportRows(rows: Record<string, unknown>[]): void {
    if (rows.length === 0) { this.toastService.error('Fichier vide.'); return; }

    const valid: ClientTransactionCreateRequest[] = [];
    const errors: string[] = [];

    rows.forEach((row, i) => {
      const symbol = String(row['Symbole'] ?? row['Symbol'] ?? '').trim();
      const type = String(row['Type'] ?? '').trim();
      const qty = Number(row['Quantite'] ?? row['Quantity'] ?? 0);
      const price = Number(row['PrixUnitaire'] ?? row['Prix unitaire'] ?? row['UnitPrice'] ?? 0);
      const fees = Number(row['Frais'] ?? row['Fees'] ?? 0);
      const dateRaw = row['Date'] ?? row['DateTransaction'] ?? '';
      const date = dateRaw ? new Date(String(dateRaw)) : new Date();

      if (!symbol) { errors.push(`Ligne ${i + 2}: symbole manquant`); return; }
      if (!['Buy', 'Sell', 'Achat', 'Vente'].includes(type)) { errors.push(`Ligne ${i + 2}: type invalide («${type}»)`); return; }
      if (qty <= 0 || price <= 0) { errors.push(`Ligne ${i + 2}: quantité/prix invalide`); return; }

      const transactionType: 'Buy' | 'Sell' = ['Buy', 'Achat'].includes(type) ? 'Buy' : 'Sell';

      valid.push(new ClientTransactionCreateRequest({
        Symbol: symbol,
        PortfolioId: this.portfolioId,
        TransactionType: transactionType,
        Quantity: qty,
        UnitPrice: price,
        Fees: fees >= 0 ? fees : 0,
        TimestampUtc: isNaN(date.getTime()) ? new Date().toISOString() : date.toISOString()
      }));
    });

    if (errors.length > 0) {
      this.toastService.error(`${errors.length} ligne(s) ignorée(s) : ${errors.slice(0, 3).join(' | ')}${errors.length > 3 ? '…' : ''}`);
    }

    if (valid.length === 0) { this.toastService.error('Aucune transaction valide à importer.'); return; }

    this.importLoading.set(true);
    this.importTransactionsSequentially(valid, 0, 0);
  }

  private importTransactionsSequentially(requests: ClientTransactionCreateRequest[], index: number, successCount: number): void {
    if (index >= requests.length) {
      this.importLoading.set(false);
      this.toastService.success(`${successCount} transaction(s) importée(s).`);
      this.loadPortfolio();
      this.loadTransactions();
      return;
    }
    this.clientFinanceService.registerTransaction(requests[index])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.importTransactionsSequentially(requests, index + 1, successCount + 1),
        error: () => this.importTransactionsSequentially(requests, index + 1, successCount)
      });
  }

  downloadImportTemplate(): void {
    const ws = XLSX.utils.json_to_sheet([{
      Symbole: 'AAPL',
      Type: 'Buy',
      Quantite: 10,
      PrixUnitaire: 175.50,
      Frais: 1.99,
      Date: '2024-01-15'
    }]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Transactions');
    XLSX.writeFile(wb, 'template-import-transactions.xlsx');
  }

  get portfolioToDeleteName(): string {
    return this.portfolioMeta()?.Name ?? '';
  }
}
