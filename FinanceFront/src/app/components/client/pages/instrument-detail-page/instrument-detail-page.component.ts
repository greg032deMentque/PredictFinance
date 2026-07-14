import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import {
  ClientInstrumentDetail,
  ClientInstrumentHistoryPage
} from '../../../../Models/client-finance-models/client-finance-models';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';

const HISTORY_PAGE_SIZE = 10;

@Component({
  selector: 'app-instrument-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, DecimalPipe, DatePipe],
  templateUrl: './instrument-detail-page.component.html',
  styleUrl: './instrument-detail-page.component.scss'
})
export class InstrumentDetailPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;

  readonly detail = signal<ClientInstrumentDetail | null>(null);
  readonly history = signal<ClientInstrumentHistoryPage | null>(null);
  readonly loading = signal(false);
  readonly historyLoading = signal(false);
  readonly historyError = signal<string | null>(null);
  readonly instrumentNotFound = signal(false);

  private symbol = '';
  private historyPage = 1;

  constructor() {
    const sym = this.route.snapshot.paramMap.get('symbol');
    if (sym) {
      this.symbol = sym;
      this.loadInstrument(sym);
      this.loadHistory();
    }
  }

  get hasPrevHistory(): boolean {
    return (this.history()?.Page ?? 1) > 1;
  }

  get hasNextHistory(): boolean {
    const h = this.history();
    if (!h) return false;
    return h.Page < Math.ceil(h.Total / HISTORY_PAGE_SIZE);
  }

  goToPrevHistory(): void {
    if (!this.hasPrevHistory) return;
    this.historyPage--;
    this.loadHistory();
  }

  goToNextHistory(): void {
    if (!this.hasNextHistory) return;
    this.historyPage++;
    this.loadHistory();
  }

  private loadInstrument(symbol: string): void {
    this.loading.set(true);
    this.clientFinanceService.getInstrumentDetail(symbol).pipe(
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (payload) => this.detail.set(payload),
      error: () => this.detail.set(null)
    });
  }

  private loadHistory(): void {
    this.historyLoading.set(true);
    this.historyError.set(null);

    this.clientFinanceService.getInstrumentHistory(this.symbol, {
      page: this.historyPage,
      pageSize: HISTORY_PAGE_SIZE,
      sortDirection: 'desc'
    }).pipe(
      finalize(() => this.historyLoading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (payload) => this.history.set(payload),
      error: (err: HttpErrorResponse) => {
        this.history.set(null);
        if (err.status === 404) {
          this.historyError.set('Cet instrument n\'est pas dans votre liste de suivi.');
        } else {
          this.historyError.set('Chargement de l\'historique instrument impossible.');
        }
      }
    });
  }
}
