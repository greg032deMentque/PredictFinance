import { Component, DestroyRef, OnInit, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { EducationArticleSummary } from '../../../../Models/client-finance-models/education-article.model';
import { educationProductTypeLabel } from '../../../../Models/client-finance-models/education-product-type.util';
import { EducationService } from '../../../../services/education.service';
import { AppRoutes } from '../../../../Routes/app.routes.constants';

interface EducationGroup {
  label: string;
  productType: string;
  articles: EducationArticleSummary[];
}

@Component({
  selector: 'app-education-list-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './education-list-page.component.html',
  styleUrl: './education-list-page.component.scss'
})
export class EducationListPageComponent implements OnInit {
  private readonly educationService = inject(EducationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly articles = signal<EducationArticleSummary[]>([]);

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly searchValue = toSignal(this.search.valueChanges, { initialValue: '' });

  protected readonly filteredGroups = computed<EducationGroup[]>(() => {
    const map = new Map<string, EducationArticleSummary[]>();
    for (const article of this.articles()) {
      const existing = map.get(article.ProductType) ?? [];
      map.set(article.ProductType, [...existing, article]);
    }
    const allGroups = Array.from(map.entries()).map(([productType, items]) => ({
      label: educationProductTypeLabel(productType),
      productType,
      articles: items.sort((a, b) => a.DisplayOrder - b.DisplayOrder)
    }));
    const q = this.searchValue().toLowerCase().trim();
    if (!q) return allGroups;
    return allGroups
      .map(g => ({
        ...g,
        articles: g.articles.filter(a =>
          a.Title.toLowerCase().includes(q) || a.Summary?.toLowerCase().includes(q)
        )
      }))
      .filter(g => g.articles.length > 0);
  });

  protected readonly educationDetailPath = (slug: string) =>
    ['/', AppRoutes.ClientRoot, AppRoutes.Education, slug];

  ngOnInit(): void {
    this.load();
  }

  protected retry(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.educationService
      .getList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.articles.set(data),
        error: () => this.error.set('Impossible de charger les articles.')
      });
  }
}
