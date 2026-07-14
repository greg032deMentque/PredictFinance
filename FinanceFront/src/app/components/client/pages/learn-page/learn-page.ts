import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { LearnOverview } from '../../../../Models/client-finance-models/learn-parameter-models';
import { toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-learn-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './learn-page.html',
  styleUrl: './learn-page.scss'
})
export class LearnPageComponent implements OnInit {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly toCommands = toCommands;
  protected readonly overview = signal<LearnOverview | null>(null);
  protected readonly loading = signal(false);

  ngOnInit(): void {
    this.loadOverview();
  }

  protected retry(): void {
    this.loadOverview();
  }

  private loadOverview(): void {
    this.loading.set(true);
    this.overview.set(null);
    this.clientFinanceService
      .getLearnOverview()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.overview.set(data),
        error: () => this.toastService.error('Impossible de charger le contenu pédagogique.')
      });
  }
}
