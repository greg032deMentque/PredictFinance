import { Component, DestroyRef, OnInit, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { FaqItem } from '../../../../Models/client-finance-models/faq.model';
import { FaqService } from '../../../../services/faq.service';

interface FaqSection {
  category: string;
  items: FaqItem[];
}

@Component({
  selector: 'app-help-center-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './help-center-page.html',
  styleUrl: './help-center-page.scss'
})
export class HelpCenterPageComponent implements OnInit {
  private readonly faqService = inject(FaqService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  private readonly items = signal<FaqItem[]>([]);

  protected readonly faqSections = computed<FaqSection[]>(() => {
    const map = new Map<string, FaqItem[]>();
    for (const item of this.items()) {
      const existing = map.get(item.Category) ?? [];
      map.set(item.Category, [...existing, item]);
    }
    return Array.from(map.entries()).map(([category, faqItems]) => ({
      category,
      items: faqItems.sort((a, b) => a.DisplayOrder - b.DisplayOrder)
    }));
  });

  ngOnInit(): void {
    this.load();
  }

  protected retry(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.faqService
      .getList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.items.set(data ?? []),
        error: () => this.error.set('Impossible de charger le centre d\'aide.')
      });
  }
}
