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
    // Catalogue déjà en cache (signal peuplé) : on répond de façon synchrone via of(), sans repasser par le back.
    if (this.patternCatalog().length > 0) {
      return of(this.patternCatalog());
    }

    // Un chargement est déjà en vol (plusieurs composants appellent ensureLoaded() en parallèle au démarrage) :
    // on renvoie le même Observable partagé plutôt que de déclencher un second appel HTTP redondant.
    if (this.pendingLoad$) {
      return this.pendingLoad$;
    }

    const request$ = this.clientFinanceService.getPatternCatalog().pipe(
      // Défense contre des entrées de catalogue incomplètes côté back (Id/Label vides) qui casseraient
      // labelFor() et l'affichage — on les exclut silencieusement plutôt que de les propager à l'UI.
      map((patterns) =>
        patterns.filter((pattern) => pattern.Id.trim().length > 0 && pattern.Label.trim().length > 0)
      ),
      tap((patterns) => this.patternCatalog.set(patterns)),
      // pendingLoad$ doit être remis à null dès que la requête se termine (succès ou erreur), sinon un appel
      // en échec bloquerait indéfiniment tout ensureLoaded() ultérieur sur la branche "chargement en vol".
      finalize(() => {
        this.pendingLoad$ = null;
      }),
      // shareReplay(1) rejoue la dernière valeur à tout abonné qui arrive après coup pendant la même requête
      // en vol, garantissant qu'un seul appel HTTP dessert tous les composants qui l'attendent.
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

    // Avant chargement du catalogue (ou pattern inconnu), on affiche l'id brut plutôt qu'un texte vide :
    // c'est dégradé mais jamais silencieusement incorrect pour l'utilisateur.
    return this.patternLabels().get(normalizedPatternId) ?? normalizedPatternId;
  }
}
