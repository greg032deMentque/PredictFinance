import { CommonModule, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { ClientHistoryPage } from '../../../../Models/client-finance-models/client-finance-models';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';

const RECOMMENDATION_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'Toutes les recommandations' },
  { value: 'Monitor', label: 'Surveiller' },
  { value: 'Buy', label: 'Acheter' },
  { value: 'Wait', label: 'Attendre' },
  { value: 'Hold', label: 'Conserver' },
  { value: 'Reinforce', label: 'Renforcer' },
  { value: 'Lighten', label: 'Alléger' },
  { value: 'Sell', label: 'Vendre' }
];

const PAGE_SIZE = 20;

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, ReactiveFormsModule],
  templateUrl: './history-page.component.html',
  styleUrl: './history-page.component.scss'
})
export class HistoryPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  readonly recommendationOptions = RECOMMENDATION_OPTIONS;

  readonly history = signal<ClientHistoryPage>({ Items: [], Total: 0, Page: 1, PageSize: PAGE_SIZE });
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.history().Total / PAGE_SIZE)));
  readonly hasPrev = computed(() => this.history().Page > 1);
  readonly hasNext = computed(() => this.history().Page < this.totalPages());

  readonly filters = this.fb.nonNullable.group({
    symbol: this.fb.nonNullable.control(''),
    recommendation: this.fb.nonNullable.control('')
  });

  private currentPage = 1;

  constructor() {
    this.loadHistory();

    this.filters.controls.symbol.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadHistory();
    });

    this.filters.controls.recommendation.valueChanges.pipe(
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadHistory();
    });
  }

  toggleSort(): void {
    this.sortDirection.update(d => d === 'desc' ? 'asc' : 'desc');
    this.currentPage = 1;
    this.loadHistory();
  }

  goToPrev(): void {
    if (!this.hasPrev()) return;
    this.currentPage--;
    this.loadHistory();
  }

  goToNext(): void {
    if (!this.hasNext()) return;
    this.currentPage++;
    this.loadHistory();
  }

  private loadHistory(): void {
    this.loading.set(true);
    this.error.set(null);

    const { symbol, recommendation } = this.filters.getRawValue();

    this.clientFinanceService.getHistory({
      page: this.currentPage,
      pageSize: PAGE_SIZE,
      symbol: symbol || undefined,
      recommendation: recommendation || undefined,
      sortDirection: this.sortDirection()
    }).pipe(
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (payload) => this.history.set(payload),
      error: () => {
        this.history.set({ Items: [], Total: 0, Page: 1, PageSize: PAGE_SIZE });
        this.error.set('Chargement de l\'historique impossible.');
      }
    });
  }
}
