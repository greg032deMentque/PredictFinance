# 12 — Plan d'implémentation V1

> **Propriétaire de** : traduction opérationnelle des specs en blocs de travail ordonnés par priorité, dépendance et risque. Ce document est la synthèse exécutable de [01](01_specification_produit.md) (règles), [06](06_ecarts_doc_code.md) (écarts et arbitrations), [07](07_flux_metier_client_admin.md) (flux), [08](08_analyse_critique_et_legal.md) (légal) et [10](10_personas.md)–[11](11_carte_navigation_et_parcours.md) (personas, parcours).
>
> **Lecture** : lire dans l'ordre A → B → C → D → E. Les blocs C sont séquentiels (un bloc ne commence pas si ses dépendances ne sont pas livrées). Les arbitrations D sont les seuls blocages qui ne dépendent pas du code.

---

## A. Principes de priorisation

| Priorité | Définition | Conséquence si non livré |
|---|---|---|
| **P0** | Bloquant : légal, décrochage J+1, moat pédagogique absent | Lancement commercial impossible ou produit sans valeur différenciée |
| **P1** | Important : rétention, pilotage, complétude admin | Churn élevé, pilotage à l'aveugle |
| **P2** | Souhaitable : profondeur, confort, contenu éducatif | Expérience incomplète mais produit fonctionnel |

### A.1 Règle de non-régression

> Toute modification d'une politique de scoring, d'un wording ou d'une règle de recommendation **doit incrémenter `policyVersion`**. Les snapshots existants ne sont jamais recalculés (RM-20). Cette règle prime sur toute urgence de livraison.

---

## B. Pré-requis légaux (conditions sine qua non avant commercialisation)

Ces actions ne dépendent pas du code — elles dépendent de décisions externes ou de rédaction. **Aucun bloc de code ne peut compenser leur absence.**

| # | Action | Référence légale | Gravité | Responsable |
|---|---|---|---|---|
| **L-01** | **Avis avocat AMF/MIF 2** : requalification CIF 40–60% de probabilité | Art. L541-1 CMF, Dir. MIF 2 2014/65/UE | 🔴 EXISTENTIEL | Externe — cabinet spécialisé |
| **L-02** | **Rédaction CGU** (post-avis L-01) | LCEN Art. 6, Code conso | 🔴 CRITIQUE | Juridique + produit |
| **L-03** | **Rédaction politique de confidentialité** | RGPD Art. 13 | 🔴 CRITIQUE | Juridique |
| **L-04** | **Rédaction mentions légales** | LCEN Art. 6 | 🔴 CRITIQUE | Juridique |
| **L-05** | **Rédaction disclaimer AMF** (contenu conditionné par L-01) | AMF — communication | 🔴 CRITIQUE | Juridique + produit |
| **L-06** | **Suppression compte RGPD Art. 17** (FLUX-A-13) | RGPD Art. 17 | 🔴 CRITIQUE | Tech — Bloc 1 |
| **L-07** | **Anonymisation logs analytics** > 13 mois | CNIL Délibération, RGPD Art. 5.1.e | 🔴 CRITIQUE | Tech — Bloc 4 (A-10) |
| **L-08** | **Export données** (portabilité RGPD Art. 20) | RGPD Art. 20 | 🟡 ÉLEVÉ | Tech — Bloc 4 |

> **Séquence** : L-01 doit être obtenu **avant** L-02 et L-05 (le contenu dépend de la qualification). L-06 doit être livré **avant toute mise en production** quelle que soit la qualification.

---

## C. Backlog ordonné par bloc

### BLOC 1 — Acquisition & onboarding (P0)

**Objectif** : qu'un visiteur puisse créer un compte et accomplir sa première analyse sans abandon.

**Dépend de** : socle auth déjà construit (JWT, guards, routes) — aucune dépendance bloquante en code.

**Dépend de (légal)** : L-02 (CGU), L-03 (politique confidentialité) — les liens doivent exister avant ouverture.

| ID | Livrable | Spec | Flux | PERSONA | Effort |
|---|---|---|---|---|---|
| **B1-01** | Écran `/register` — `RegisterComponent` | 03 §B.4 | FLUX-C-01 | PERSONA-ANON | M |
| **B1-02** | Endpoint backend `POST /auth/register` (validation, unicité, hash, email confirmation) | 05 §API auth | FLUX-C-01 étapes 1–4 | PERSONA-ANON | M |
| **B1-03** | Variante `/register-confirm` (post-soumission, renvoi email) | 03 §B.4 variante | FLUX-C-01 étape 4 | PERSONA-ANON | S |
| **B1-04** | Validation token de confirmation email (`GET /auth/confirm?token=...`) | 05 §API auth | FLUX-C-01 étapes 5–6 | PERSONA-ANON | S |
| **B1-05** | Écran `C.14` — `OnboardingEmptyComponent` (route `/client/onboarding`) | 03 §C.14 | FLUX-C-04 | PERSONA-U01 | M |
| **B1-06** | Détection état vide dans `ClientDashboardComponent` → redirect C.14 | 03 §C.1 état vide | FLUX-C-04 | PERSONA-U01 | S |
| **B1-07** | FLUX-A-13 — Suppression de compte (auto-service C.15 + admin D.2) | 07 FLUX-A-13 | FLUX-A-13 | L-06 | M |

**Critère de validation** :
- Un visiteur peut créer un compte, confirmer son email, et se connecter.
- Un investisseur sans données arrive sur l'écran d'onboarding, pas sur une home vide.
- Un utilisateur peut supprimer son compte depuis C.15 (RGPD Art. 17).

---

### BLOC 2 — Différenciateurs pédagogiques (P0)

**Objectif** : livrer les deux moats pédagogiques qui transforment PredictFinance de "screener" en outil d'apprentissage.

**Dépend de** : moteur de patterns (construit), `AnalysisSnapshot` (construit), `PatternAssessment` avec sous-objets `detection`/`validation`/`invalidation` (construit).

| ID | Livrable | Spec | Flux | RM | Effort |
|---|---|---|---|---|---|
| **B2-01** | Backend — `ActionPlan` generator : 5 types de steps (`NOTE_LEVEL`, `REVIEW_AT`, `SET_ALERT`, `HOLDING_REMINDER`, `WAIT_FOR_DATA`), toute valeur tracée vers `sourceField` | 05 §ActionPlan | FLUX-C-07 | RM-26 | L |
| **B2-02** | Frontend C.5 — Bloc « Vos prochaines étapes » (plan d'action UI, liste steps, lien sourceField) | 03 §C.5 | FLUX-C-08 | RM-26 | M |
| **B2-03** | Backend — `ConfidenceBreakdown` : décomposition détection/validation/invalidation en critères avec état MET/PARTIAL/ABSENT, libellés gouvernés | 05 §ConfidenceBreakdown | FLUX-C-07 | RM-27 | M |
| **B2-04** | Frontend C.5 — Section « Pourquoi ce niveau de confiance » (ConfidenceBreakdown UI, grille critères ✅/⚠️/❌) | 03 §C.5 | FLUX-C-08 | RM-27 | M |
| **B2-05** | Persistance de `ConfidenceBreakdown` et `ActionPlan` dans `AnalysisSnapshot` (versionné) | 05 §AnalysisSnapshot | RM-19 | RM-19 | S |

> **Règle critique** : `ActionPlan` **ne doit jamais introduire de nouvelle vérité** — chaque valeur doit être tracée vers un champ existant du snapshot (`sourceField`). Si une valeur ne peut pas être tracée, elle ne doit pas apparaître (RM-26).

**Critère de validation** :
- Un résultat d'analyse avec `CrediblePatternFound` affiche le plan d'action avec 2–3 étapes concrètes.
- Chaque étape du plan affiche sa `sourceField` (vérifiable).
- La confiance est décomposée en critères lisibles par un débutant.

---

### BLOC 3 — Boucle ex post et alertes proactives (P0 rétention)

**Objectif** : fermer la boucle de feedback — prouver au fil du temps que le moteur a eu raison ou tort, et alerter l'utilisateur quand son contexte change.

**Dépend de** : `AnalysisSnapshot` avec `TargetPrice`/`InvalidationPrice` persistés (vérifier dette T-05) ; arbitrages **A-07** (fenêtre évaluation) et **A-08** (stockage `SignalOutcome`) fermés.

| ID | Livrable | Spec | Flux | RM | Effort |
|---|---|---|---|---|---|
| **B3-01** | Arbitrage A-07 : définir `evaluationWindowDays` (lié à `reviewHorizonDays` ou horizon fixe) | 06 §A-07 | — | RM-29 | — (décision) |
| **B3-02** | Arbitrage A-08 : stocker `SignalOutcome` (colonne snapshot vs table dédiée) | 06 §A-08 | — | RM-29 | — (décision) |
| **B3-03** | Backend — `SignalOutcome` job batch : rejoue `PriceHistory` vs `TargetPrice`/`InvalidationPrice` → produit `TARGET_HIT`, `INVALIDATION_HIT`, `STILL_OPEN`, `NOT_EVALUABLE` | 05 §SignalOutcome | FLUX-C-11 | RM-29 | L |
| **B3-04** | Backend — Alertes proactives : job de veille `PATTERN_STATE_CHANGE`, `LEVEL_CROSSED`, `DATA_STALE` → génère `NotificationItem` avec déduplication `(instrumentId, AlertTrigger, jour)` | 07 FLUX-C-11 | FLUX-C-12 | RM-25, RM-25b | L |
| **B3-05** | Frontend C.12 — Affichage alertes proactives avec `AlertTrigger` et routage vers C.5 ou C.6 | 11 §H | FLUX-C-12 | RM-23, RM-25 | M |
| **B3-06** | Frontend C.5 — Bloc évaluation ex post (état `SignalOutcome` + date résolution, sous l'analyse) | 03 §C.5 | — | RM-29 | M |

> **Prérequis sur T-05** : vérifier avant B3-03 que `AnalysisSnapshot` persiste bien `TargetPrice`, `InvalidationPrice` et `reviewHorizonDays` (dette T-05). Si non : corriger la profondeur de snapshot en premier.

**Critère de validation** :
- Un snapshot avec `CrediblePatternFound` reçoit une issue ex post dans la fenêtre définie.
- Un franchissement de niveau déclenche une notification dans C.12 routée vers C.5 ou C.6.
- L'utilisateur peut désactiver chaque type d'alerte depuis C.15.

---

### BLOC 4 — Admin complet (P1)

**Objectif** : compléter l'espace admin avec les flux manquants (A-02 à A-13) et les tableaux KPI.

**Dépend de** : Bloc 3 (pour KPI famille A — qualité signaux) ; arbitrages **A-09** (définition DAU/WAU/MAU), **A-10** (anonymisation CNIL) fermés.

| ID | Livrable | Flux | Priorité | Effort |
|---|---|---|---|---|
| **B4-01** | Suppression compte (L-06, si non livré en Bloc 1) | FLUX-A-13 | P0 (bloquant légal) | M |
| **B4-02** | KPI tableau qualité signaux `/admin/kpi/signal-quality` (famille A) | FLUX-A-10 | P1 (dépend Bloc 3) | M |
| **B4-03** | Arbitrage A-09 : définir DAU/WAU/MAU (1 définition stable) | 06 §A-09 | Bloquant B4-04 | — |
| **B4-04** | KPI tableau engagement `/admin/kpi/engagement` (familles B+C+D) | FLUX-A-11 | P1 | L |
| **B4-05** | Job anonymisation logs > 13 mois (obligation CNIL A-10) | 06 §A-10 | P0 légal | M |
| **B4-06** | Endpoint export données utilisateur `GET /account/data-export` (RGPD Art. 20) | 08 §2.2 | P1 | M |
| **B4-07** | Liens KPI dans `/admin/dashboard` (D.1 → D.10a, D.10b) | 11 §F | P1 | S |

**Critère de validation** :
- L'admin accède aux 4 familles de KPI avec des formules traçables.
- L'anonymisation des logs tourne mensuellement.
- L'export de données est disponible depuis C.15.

---

### BLOC 5 — Profondeur client (P2)

**Objectif** : enrichir l'expérience des PERSONA-U02 avancés avec les surfaces de profondeur.

**Dépend de** : Bloc 2 (plan d'action, confiance expliquée) ; snapshots persistés avec profondeur suffisante (T-05 résolu).

| ID | Livrable | Spec | Flux | Effort |
|---|---|---|---|---|
| **B5-01** | Écran C.7 — Paramètre detail user (`/client/instruments/:symbol/parameter/:id`) : 4 couches pédagogiques depuis dictionnaire gouverné | 03 §C.7 | — | M |
| **B5-02** | Écran C.10 — Comparaison snapshots user (`/client/analysis/compare`) : diff 2 snapshots persistés | 03 §C.10 | FLUX-C-09 | M |
| **B5-03** | Écran C.13 — Centre d'aide (`/client/help`) : aide contextuelle déterministe + routage | 03 §C.13 | FLUX-C-14 | L |
| **B5-04** | Écran C.11 — Learn (`/client/learn`) : contenu éducatif conceptuel long | 03 §C.11 | FLUX-C-14 | L |
| **B5-05** | Glossaire inline — info-bulles sur termes techniques dans C.5 et C.6, depuis dictionnaire gouverné | 03 §C.6–C.5 | — | M |

**Critère de validation** :
- L'investisseur peut ouvrir le détail d'un paramètre depuis C.5 et lire les 4 couches.
- L'investisseur peut comparer deux snapshots sur le même instrument.

---

## D. Arbitrations à lever (conditions avant chaque bloc)

Ces décisions ne dépendent pas du code — elles dépendent d'un arbitrage produit/légal/architecture. **Chaque bloc qui attend une arbitration est bloqué tant qu'elle n'est pas fermée.**

| ID | Question | Impact | Bloc bloqué | Recommandation |
|---|---|---|---|---|
| **A-07** | Fenêtre d'évaluation ex post : `reviewHorizonDays` du signal (variable) ou horizon fixe (ex. 30j) ? | `SignalOutcome` job, KPI famille A | Bloc 3 (B3-01/B3-03) | Recommandé : utiliser `reviewHorizonDays` persisté dans le snapshot. Fixer un horizon par défaut (30j) si absent. |
| **A-08** | Stockage `SignalOutcome` : colonne dans `AnalysisSnapshot` ou table dédiée `SignalOutcomes` ? | Architecture persistance, évolutivité multi-horizons | Bloc 3 (B3-02/B3-03) | Recommandé : table dédiée `SignalOutcomes (snapshotId, horizon, outcome, evaluatedAt)` — anticipates multi-horizons V2. |
| **A-09** | Définition canonique de « utilisateur actif » pour DAU/WAU/MAU | Toutes métriques engagement | Bloc 4 (B4-05/B4-06) | Recommandé : ≥ 1 requête authentifiée (hors refresh token) dans la fenêtre. Documenter dans 02 (glossaire). |
| **A-10** | Implémentation anonymisation logs > 13 mois (CNIL — **non optionnel**) | Obligation légale, bloquerait le lancement | Bloc 4 (B4-07) | Planifier dès le début de Bloc 4 : job mensuel, flag `anonymizedAt`, suppression des `userId` nominatifs. |
| **T-05** | Profondeur snapshot lecture support : `TargetPrice`, `InvalidationPrice`, `reviewHorizonDays`, score composite, PEA persistés ? | Boucle ex post, comparaison | Bloc 3 (B3-03) | Auditer `AnalysisSnapshot` réel avant Bloc 3. Compléter si nécessaire — sans cela, l'ex post est impossible. |

---

## E. Matrice risques V1 (top 7, filtrés P0)

| # | Risque | Probabilité | Impact | Mitigation |
|---|---|---|---|---|
| **R-01** | **Requalification CIF/AMF** : l'application est considérée comme prestation de conseil en investissement | Prob. 40–60% | Existentiel (arrêt activité ou amende AMF) | L-01 : avis avocat avant lancement. Renforcer les disclaimers, désactiver les verbes actifs si requalification confirmée. |
| **R-02** | **Absence de suppression compte** (RGPD Art. 17) en production | Prob. 100% si non livré | Amende CNIL jusqu'à 4% CA mondial | B1-07 dans Bloc 1 — livrer avant tout accès utilisateur en production. |
| **R-03** | **Abandon J+1** : C.14 absent, home vide non guidée | Prob. très élevée sans C.14 | Churn > 70% J+1 | B1-05/B1-06 dans Bloc 1. |
| **R-04** | **Churn M+6** : aucun signal ex post reçu, moteur non prouvé | Prob. élevée sans Bloc 3 | Churn > 8%/mois, perte de confiance | Bloc 3 livré dans les 3 mois post-lancement. |
| **R-05** | **Plan d'action absent** : produit perçu comme screener | Prob. 100% si B2-01/B2-02 non livrés | Différenciation nulle | Bloc 2 livré avant tout lancement public. |
| **R-06** | **Logs non anonymisés** > 13 mois en production | Prob. élevée si ignoré | Mise en demeure CNIL, amende | B4-07 planifié dès l'ouverture de Bloc 4, job mensuel automatique. |
| **R-07** | **T-05 non résolu** : `AnalysisSnapshot` ne persiste pas `TargetPrice`/`InvalidationPrice` | À auditer | Bloc 3 impossible, ex post bloqué | Auditer en pré-Bloc 3. Corriger avec migration si nécessaire. |

---

## F. Séquence visuelle des blocs

```
Maintenant
    │
    ├─ PRÉ-REQUIS LÉGAUX (parallèle, externe)
    │   L-01 Avis AMF/CIF ──────────────────────────────────────► L-02/L-03/L-04/L-05 (rédaction docs légaux)
    │
    ├─ BLOC 1 — Acquisition & onboarding (P0)
    │   B1-01..B1-07 (inscription, email confirm, onboarding, suppression compte)
    │   ◄────────────────────────────────────────────────────────► ~3–4 semaines
    │
    ├─ BLOC 2 — Différenciateurs pédagogiques (P0)
    │   B2-01..B2-05 (plan d'action, confiance expliquée, persistance)
    │   ◄────────────────────────────────────────────────────────► ~3–4 semaines
    │
    ├─ [Audit T-05 : vérifier profondeur snapshot avant Bloc 3]
    │
    ├─ BLOC 3 — Boucle ex post + alertes (P0 rétention)
    │   B3-01/B3-02 (arbitrages A-07/A-08) + B3-03..B3-06
    │   ◄────────────────────────────────────────────────────────► ~4–5 semaines
    │
    ├─ BLOC 4 — Admin complet (P1) — après arbitrages A-09/A-10
    │   B4-01..B4-09 (KPI, export, anonymisation)
    │   ◄────────────────────────────────────────────────────────► ~4–5 semaines
    │
    └─ BLOC 5 — Profondeur client (P2)
        B5-01..B5-05 (paramètre detail, comparaison, help, learn, glossaire)
        ◄────────────────────────────────────────────────────────► ~3–4 semaines

→ Horizon V1 complet estimé : 17–22 semaines (hors temps légal)
→ MVP lançable (blocs 1+2 + légal) : ~8–10 semaines
```

---

## G. Index des références croisées

| Notion | Document autoritaire |
|---|---|
| Règles métier RM-xx | [01 §7](01_specification_produit.md#7-r%C3%A8gles-m%C3%A9tier-structurantes) |
| Enums canoniques | [02](02_glossaire_et_taxonomies.md) |
| Spec écrans B.x / C.x / D.x | [03](03_specification_ecrans.md) |
| User stories | [04](04_user_stories.md) |
| Contrats données / API | [05](05_contrats_donnees_api.md) |
| Écarts code / arbitrations A-xx / dettes T-xx | [06](06_ecarts_doc_code.md) |
| Flux métier FLUX-C-xx / FLUX-A-xx | [07](07_flux_metier_client_admin.md) |
| Cadre légal détaillé | [08](08_analyse_critique_et_legal.md) |
| Diagnostic stratégique, valorisation | [09](09_diagnostic_strategique.md) |
| Personas enrichis | [10](10_personas.md) |
| Carte navigation, parcours | [11](11_carte_navigation_et_parcours.md) |
