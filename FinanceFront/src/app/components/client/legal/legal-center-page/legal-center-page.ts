import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { LegalCardItem } from '../../../../Models/client-finance-models/legal-card.model';
import { LegalService } from '../../../../services/legal.service';

@Component({
  selector: 'app-legal-center-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './legal-center-page.html',
  styleUrl: './legal-center-page.scss'
})
export class LegalCenterPageComponent implements OnInit {
  private readonly legalService = inject(LegalService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly legalCards = signal<LegalCardItem[]>([]);

  ngOnInit(): void {
    this.load();
  }

  protected retry(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.legalService
      .getList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.legalCards.set(data ?? []),
        error: () => this.error.set('Impossible de charger le centre légal.')
      });
  }
}
