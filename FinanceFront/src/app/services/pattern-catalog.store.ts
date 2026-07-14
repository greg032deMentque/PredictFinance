import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, finalize, map, of, shareReplay, tap } from 'rxjs';
import { type PatternCatalogItem } from '../Models/client-finance-models/client-finance-models';
import { ClientFinanceService } from './client-finance.service';

@Injectable({ providedIn: 'root' })
export class PatternCatalogStore {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly patternCatalog = signal<readonly PatternCatalogItem[]>([]);
  private readonly patternLabels = computed(
    () => new Map(this.patternCatalog().map((pattern) => [pattern.Id, pattern.Label] as const))
  );

  private pendingLoad$: Observable<readonly PatternCatalogItem[]> | null = null;

  readonly items = computed(() => this.patternCatalog());

  ensureLoaded(): Observable<readonly PatternCatalogItem[]> {
    if (this.patternCatalog().length > 0) {
      return of(this.patternCatalog());
    }

    if (this.pendingLoad$) {
      return this.pendingLoad$;
    }

    const request$ = this.clientFinanceService.getPatternCatalog().pipe(
      map((patterns) =>
        patterns.filter((pattern) => pattern.Id.trim().length > 0 && pattern.Label.trim().length > 0)
      ),
      tap((patterns) => this.patternCatalog.set(patterns)),
      finalize(() => {
        this.pendingLoad$ = null;
      }),
      shareReplay(1)
    );

    this.pendingLoad$ = request$;
    return request$;
  }

  labelFor(patternId: string): string {
    const normalizedPatternId = patternId.trim();

    if (normalizedPatternId.length === 0) {
      return 'Pattern indisponible';
    }

    return this.patternLabels().get(normalizedPatternId) ?? normalizedPatternId;
  }
}
