# Handoff Codex 5.4 — Stabilisation V1 PredictFinance

> Document de référence pour l'exécution. À supprimer une fois les 5 lots livrés et validés.
> Back : `FinanceBack/`. Front : `FinanceFront/` (Angular).

---

## Règles de travail (à respecter sur TOUS les lots)

- **Plan avant code** : pour chaque lot, présenter le plan (fichiers touchés + changements) et attendre validation avant de générer.
- **Un lot = un commit** cohérent et testable seul. Respecter l'ordre des lots.
- **Pas de façade pass-through** : un contrôleur n'appelle un service que si le service ajoute de la logique. Sinon, accès direct. Ne pas créer de couche de délégation vide.
- **Un type par fichier** (tolérer interface+impl, VM+Profile AutoMapper, enums groupés).
- **PascalCase obligatoire** sur tout DTO/modèle qui traverse la frontière back↔front (le back sérialise en PascalCase). Côté front, les interfaces de transport doivent matcher exactement.
- **Soft-delete via `IsDeleted`**, jamais de `DELETE` SQL.
- **Pagination** via `PagedListViewModel<T>` sur toute liste retournée.
- **Enums sérialisés en chaînes** (pas en int).
- **Frontend = rendering layer** : aucune vérité métier en dur côté front. La vérité vient du back.
- **Nommage** : anglais dans le code, français pour les textes utilisateur.

## Tests

- **Front : ne créer AUCUN test** (Angular/Jasmine). Mettre à jour uniquement le spec de conformité existant si le contrat change.
- **Back : créer un test UNIQUEMENT si** il prouve un comportement métier à risque de régression silencieuse (segmentation, FIFO, agrégats, recalcul). Pas de test de plumbing/mapping trivial. ~5 tests max sur l'ensemble.

## Validation à chaque lot

- Back : `dotnet test` (122 tests verts existants doivent le rester + nouveaux ciblés).
- Front : `npx tsc -p tsconfig.app.json --noEmit` ET `npx tsc -p tsconfig.spec.json --noEmit`.

## Glossaire

- **Drift** : désynchronisation entre le contrat back et ce que le front envoie/attend.
- **FIFO** (First In First Out) : pour les ventes, on consomme d'abord les lots d'achat les plus anciens. Sert à calculer le PRU (prix de revient unitaire) et l'encours restant par portefeuille.
- **PRU** : prix de revient unitaire moyen pondéré des lots encore détenus.
- **Encours** (`OutstandingAmount`) : montant investi encore détenu (somme des lots non vendus).
- **Agrégat global** : calcul transverse à tous les portefeuilles d'un utilisateur.

---

## LOT 1 — Catalogue patterns dynamique + suppression legacy

**Objectif** : le back devient seule source de vérité du catalogue de patterns. Le front le charge dynamiquement. Supprimer toute trace de `DOUBLE_TOP` / `RequestedPattern` côté front.

**Problème actuel** : le front force `DOUBLE_TOP` comme unique choix et parle `RequestedPattern` (singulier), alors que le back attend `RequestedPatternIds` (liste) + 4 patterns de continuation. La page simulation est cassée.

### Back (`FinanceBack`)
- `PatternIds.cs` (Patterns) : source de vérité des IDs actifs (déjà présent, ne pas dupliquer).
- **Nouveau** `PatternCatalogViewModel.cs` (ViewModels) : `{ Id, Label, Family, Description, Direction }` — PascalCase.
- **Nouveau** `PatternCatalogService.cs` (Services/Analysis) : retourne la liste des patterns supportés + métadonnées. NE PAS créer ce service s'il ne fait que recopier `PatternIds` sans logique — dans ce cas, le contrôleur lit `PatternIds` directement.
- Contrôleur : `GET api/client-finance/patterns/catalog` → `List<PatternCatalogViewModel>`.

### Front (`FinanceFront`)
- **Nouveau** `pattern-catalog.model.ts` : interface PascalCase alignée sur le DTO.
- `client-finance.service.ts` : `getPatternCatalog(): Observable<PatternCatalog[]>`.
- `finance-simulation.component.ts` : supprimer le `DOUBLE_TOP` forcé, charger le catalogue.
- `client-patterns.constants.ts` : supprimer la vérité legacy.
- `client-simulation-request.model.ts` + `client-analysis-launch-request.model.ts` : `RequestedPattern` → `RequestedPatternIds: string[]`.

**Test back** (1) : `PatternCatalogService` retourne exactement les IDs supportés ; un ID retiré ne sort plus.

**À éviter** : réintroduire une liste de patterns en dur côté front ; garder `client-patterns.constants.ts` "au cas où".

---

## LOT 2 — Segmentation portefeuille par portfolioId + FIFO

**Objectif** : la quantité et l'encours affichés pour un portefeuille doivent venir des transactions DE CE portefeuille, pas de l'agrégat user+actif.

**Problème actuel** : `HeldQuantity`/`OutstandingAmount` repartent de `userAsset.Quantity` (agrégé au niveau user+actif). Si le même titre est dans 2 portefeuilles, la vue par portefeuille est fausse.

### Back (`FinanceBack`)
- `ClientFinanceWatchlistPortfolioService.cs` (lignes ~71, 217, 278) : recalculer quantité/encours depuis `AssetTransactions` filtrées par `portfolioId`.
- **Nouveau** `PortfolioHoldingCalculator.cs` : logique de recalcul.
  - Trier les transactions du portefeuille par date.
  - **Si** chaque lot a date + prix unitaire exploitables → **FIFO** : consommer les lots anciens en premier pour les ventes ; calculer PRU + encours restant du portefeuille.
  - **Sinon** (prix/date manquant sur un lot) → **fallback** : quantité nette + encours brut simple, SANS planter. Logguer le fallback.
- `UserAsset.cs` / `AssetTransaction.cs` : lecture seule. Ne pas modifier le modèle — dériver la vérité des transactions.

**Tests back** (2) :
1. Même actif sur 2 portefeuilles → chaque portefeuille montre sa quantité propre.
2. FIFO : achat 10@100 puis 10@120, vente 5 → PRU/encours restant corrects ; cas fallback (prix manquant) ne jette pas.

**À éviter** : matérialiser une nouvelle table de positions (non demandé) ; toucher au modèle EF. La vérité reste dérivée des transactions.

---

## LOT 3 — Archivage de portefeuille

**Objectif** : remplacer la suppression ambiguë par un statut **Archivé** — exclu des agrégats globaux par défaut, mais toujours consultable.

**Problème actuel** : le soft-delete masque le portefeuille mais ses transactions pèsent encore dans les agrégats globaux → incohérence.

### Back (`FinanceBack`)
- `Portfolio.cs` : ajouter `Status` (enum `Active`/`Archived`, sérialisé en chaîne) OU `ArchivedAt` nullable.
- Migration EF pour la nouvelle colonne.
- `PortfolioService.cs` (ligne ~106) : `ArchivePortfolioAsync` (au lieu d'un delete ambigu).
- `ClientFinanceWatchlistPortfolioService.cs` (lignes ~60, 213) : **TOUS** les agrégats globaux excluent les portefeuilles `Archived` par défaut. Vérifier qu'aucun calcul global n'oublie le filtre.
- Endpoint : `PUT api/client-finance/portfolios/{id}/archive` (+ `/restore` optionnel).

### Front (`FinanceFront`)
- `portfolio-page.component.ts` : action "Archiver" ; filtre actifs / archivés.
- Modèle portfolio front : ajouter `Status` (PascalCase).

**Test back** (1) : un portefeuille archivé ne pèse plus dans les agrégats globaux mais reste lisible en consultation directe.

**À éviter** : oublier un seul site d'agrégation (c'est le piège — chercher tous les calculs globaux et filtrer partout).

---

## LOT 4 — Édition de transaction (PUT)

**Objectif** : vrai endpoint d'édition + flow UI, avec recalcul de position après édition.

**Problème actuel** : ajout et suppression existent, modification non (ni endpoint, ni UI).

### Back (`FinanceBack`)
- `ClientFinancePortfolioController.cs` (ligne ~49) : `PUT api/client-finance/portfolios/{portfolioId}/transactions/{id}`.
- Service transaction : `UpdateTransactionAsync` → valider, mettre à jour, **recalculer** la position via `PortfolioHoldingCalculator` (réutiliser le LOT 2).
- **Nouveau** `UpdateTransactionRequestViewModel` (PascalCase).

### Front (`FinanceFront`)
- `portfolio-detail-page.component.ts` (ligne ~151) : flow d'édition (form pré-rempli → PUT → refresh).
- Service front transaction : `updateTransaction(...)`.

**Test back** (1) : éditer la quantité d'une transaction recalcule correctement la position (cohérent LOT 2).

**À éviter** : dupliquer la logique de recalcul — réutiliser `PortfolioHoldingCalculator`.

---

## LOT 5 — Sonar : code mort, duplication, PascalCase

**Objectif** : revue Sonar propre une fois la logique stable.

### Front (`FinanceFront`)
- `UserFinancePageComponent` + flux watchlist legacy : **supprimer** (hors table de routes active `app.routes.user.ts`).
- `analysis-entry` / `simulation` / `pattern-explorer` : mutualiser le **search flow** dupliqué (service/composant partagé).
- `faq.model.ts`, `legal-card.model.ts`, `learn-topic-admin.model.ts`, `glossary-term.model.ts` : passer en **PascalCase** alignées sur les DTO back.
- `faq.service.ts`, `legal.service.ts`, `learn-topics.service.ts`, `glossary-terms.service.ts` : consommer PascalCase (mapper si besoin).
- `client-finance-compliance.spec.ts` : mettre à jour le contrat patterns.

**À éviter** : supprimer un composant encore référencé (vérifier les routes + imports avant) ; introduire une abstraction de search trop générique qui complique au lieu de simplifier.

---

## Ordre & dépendances

```
LOT 1 → LOT 2 → LOT 3 → LOT 4 → LOT 5
```
- LOT 4 réutilise `PortfolioHoldingCalculator` du LOT 2 → 2 avant 4.
- LOT 5 en dernier : nettoie ce que les lots précédents rendent obsolète (legacy patterns).
