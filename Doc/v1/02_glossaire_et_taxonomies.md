# 02 — Glossaire & taxonomies canoniques

> **Propriétaire de** : tous les enums / taxonomies fermées, et le vocabulaire FR/EN.
> Toute autre page **référence** cette page au lieu de recopier un enum.
> Les valeurs ci-dessous sont **alignées sur le code réel** (`FinanceBack/BackPredictFinance.Common/enums/`). Quand le code et l'ancienne doc divergeaient, **le code fait foi** ; les divergences sont tracées en [06](06_ecarts_doc_code.md).

---

## 1. Règle de nommage (rappel)

- **Identifiant interne** = anglais, `PascalCase` en C# (ex. `CrediblePatternFound`). Stable, jamais affiché brut.
- **Texte UI** = français, lisible débutant (ex. « Pattern crédible détecté »).
- Un identifiant interne ne doit **jamais** fuiter brut dans l'interface.

> Les tables ci-dessous donnent, pour chaque membre : la valeur **code** telle qu'écrite, et le **wording FR** attendu en UI.

---

## 2. Taxonomies d'analyse technique

### AnalysisOutcome
*Issue métier de premier rang d'une analyse. Fichier : `Common/enums/AnalysisOutcome.cs`.*

| Code | Wording FR (UI) | Sens produit | Exécutable ? |
|---|---|---|---|
| `CrediblePatternFound` | Pattern crédible détecté | Un pattern fiable est retenu. | ✅ |
| `MultipleCompatiblePatterns` | Plusieurs patterns compatibles | Plusieurs patterns crédibles coexistent ; tous restitués (RM-06). | ✅ |
| `NoCrediblePattern` | Aucun pattern crédible retenu | Analyse exécutée mais aucun pattern fiable (RM-05). | ⚠️ non-exécutable de 1er rang |
| `InsufficientData` | Données insuffisantes pour analyser | Historique/données trop courts pour la fenêtre requise. | ⚠️ |
| `UnsupportedInstrument` | Instrument hors périmètre V1 | L'instrument n'est pas une action FR active prise en charge (RM-24, §4.4 de [01]). | ⚠️ |
| `UnsupportedContext` | Contexte non pris en charge | Le contexte de requête n'est pas exécutable en V1. | ⚠️ |

> ⚠️ **Divergence résolue.** L'ancien `contract_freeze.md` listait `ANALYSIS_COMPLETED / INSTRUMENT_NOT_ELIGIBLE / INSUFFICIENT_MARKET_DATA / NO_ENABLED_PATTERN / NO_CREDIBLE_PATTERN` — **périmé**. La table ci-dessus reflète le code réel. Voir [06](06_ecarts_doc_code.md).

### PatternStatus
*État d'avancement d'un pattern. Fichier : `Common/enums/PatternStatus.cs`.*

| Code | Wording FR (UI) | Sens produit |
|---|---|---|
| `Forming` | En formation | Des éléments précurseurs existent, la configuration commence à se structurer. |
| `Monitoring` | À surveiller | Pattern crédible à suivre, pas encore confirmé. |
| `Confirmed` | Confirmé | Les critères de validation du pattern sont atteints. |
| `Invalidated` | Invalidé | Les conditions contredisent la lecture du pattern. |
| `Completed` | Terminé | Le scénario pédagogique a atteint sa conclusion / phase terminale. |

**État dérivé `Absent`** (« Absent ») : existe uniquement dans le **read model** (`PatternProgressStatusEnum`, fichier `UserCoreReadModelEnums.cs`) pour signifier qu'un pattern attendu n'est pas présent dans une liste. Ce n'est **pas** un `PatternStatus` de domaine.

> Règle de réconciliation : « pattern envisagé », « proche de validation » sont des formulations pédagogiques **dans les explications**, jamais des statuts canoniques. Tout écran/snapshot/API expose l'un des 5 codes ci-dessus.

### ValidationState
*Fichier : `Common/AnalysisV1/…` (objet `PatternAssessment.validation`).*

| Code | Sens |
|---|---|
| `NOT_VALIDATED` | Critères de validation non atteints. |
| `VALIDATED` | Critères atteints. |
| `NOT_APPLICABLE` | La validation ne s'applique pas à ce stade. |

### InvalidationState

| Code | Sens |
|---|---|
| `ACTIVE` | Le scénario reste valide (pas d'invalidation déclenchée). |
| `INVALIDATED` | Une condition d'invalidation est déclenchée. |
| `NOT_APPLICABLE` | L'invalidation ne s'applique pas. |

### ConfidenceLabel

| Code | Wording FR | Sens |
|---|---|---|
| `LOW` | Confiance faible | Crédibilité basse. |
| `MEDIUM` | Confiance moyenne | Crédibilité intermédiaire. |
| `HIGH` | Confiance élevée | Crédibilité haute. |

> **Confiance expliquée (RM-27)** : le `ConfidenceLabel` n'est jamais affiché seul ; il est accompagné d'une décomposition en critères dérivés des sous-objets `detection` / `validation` / `invalidation` de `PatternAssessment` ([05](05_contrats_donnees_api.md#25-patternassessment)). Chaque critère a un état pédagogique : ✅ rempli · ⚠️ partiel/non confirmé · ❌ absent. Cette décomposition explique le niveau, elle ne le recalcule pas.

### SignalOutcome (issue réalisée ex post)
*Résultat **a posteriori** d'un signal persisté, calculé par le job d'évaluation ex post (RM-29). Cible — pas un enum encore présent dans le code, voir [06](06_ecarts_doc_code.md). Stockage à arbitrer (colonne `PatternAssessment` vs table `SignalOutcome`).*

| Code | Wording FR (admin) | Règle |
|---|---|---|
| `TARGET_HIT` | Cible atteinte | Le prix a atteint `TargetPrice` **avant** `InvalidationPrice` dans la fenêtre d'évaluation. |
| `INVALIDATION_HIT` | Invalidation touchée | Le prix a atteint `InvalidationPrice` **avant** `TargetPrice`. |
| `STILL_OPEN` | En cours | Ni cible ni invalidation atteinte dans la fenêtre. |
| `NOT_EVALUABLE` | Non évaluable | Données de prix postérieures insuffisantes. |

> La fenêtre d'évaluation (basée sur `reviewHorizonDays` ou un horizon fixe) est **à arbitrer** ([06](06_ecarts_doc_code.md#4-d%C3%A9cisions-%C3%A0-arbitrer)). `SignalOutcome` alimente les alertes (RM-25) et les KPI de qualité des signaux ([03 §D.10](03_specification_ecrans.md#d10--pilotage-kpi)).

---

## 3. Patterns pris en charge (V1)

*Fichier : `BackPredictFinance.Patterns/PatternIds.cs`. Famille V1 : patterns de **continuation**.*

| Identifiant canonique | Label domaine | Biais | Lecture |
|---|---|---|---|
| `RECTANGLE_CONTINUATION` | RectangleContinuation | Continuation | Consolidation horizontale entre support et résistance, reprise dans le sens du mouvement préalable. |
| `SYMMETRICAL_TRIANGLE_CONTINUATION` | SymmetricalTriangleContinuation | Continuation | Compression en triangle symétrique, sortie dans le sens de la tendance préalable. |
| `BULL_FLAG_CONTINUATION` | BullFlagContinuation | Haussier après confirmation | Impulsion haussière (hampe) + consolidation courte et orderly, cassure au-dessus de la résistance du drapeau. |
| `BEAR_FLAG_CONTINUATION` | BearFlagContinuation | Baissier après confirmation | Impulsion baissière + rebond consolidant court, cassure sous le support du drapeau. |

> **`DOUBLE_TOP` est retiré.** Il n'est plus un pattern actif ; le code rejette explicitement `DOUBLE_TOP` comme non pris en charge (test dédié dans `BackPredictFinance.Tests`). Aucune surface V1 ne doit le présenter comme supporté.

Le détail de lecture / validation / invalidation de chaque pattern est documenté dans `Doc/_legacy/product/pattern_reference_pack/` (conservé comme référence métier).

---

## 4. Recommandation

### HoldingContext
*Contexte de détention au moment de la recommandation.*

| Code | Wording FR | Sens |
|---|---|---|
| `NOT_HELD` | Non détenue | L'utilisateur ne détient aucune quantité de l'instrument. |
| `HELD` | Détenue | L'utilisateur détient au moins une ligne ouverte. |

> ⚠️ **Dette technique** : dans le code actuel, `HoldingContext` est stocké comme **string**, pas comme enum. La cible est un enum. Voir [06](06_ecarts_doc_code.md).

### Recommandation — verbes
*Fichier : `Common/enums/RecommendationKind.cs`. Le verbe autorisé dépend du `HoldingContext` (RM-10).*

| Code (`RecommendationKind`) | Wording FR | Autorisé si **non détenue** | Autorisé si **détenue** |
|---|---|:---:|:---:|
| `Monitor` | Surveiller | ✅ | ❌ (jamais reco finale d'une position détenue) |
| `Wait` | Attendre | ✅ | ✅ |
| `Buy` | Acheter | ✅ | ❌ |
| `Hold` | Conserver | ❌ | ✅ |
| `Reinforce` | Renforcer | ❌ | ✅ |
| `Lighten` | Alléger | ❌ | ✅ |
| `Sell` | Vendre | ❌ | ✅ |

> **Enum secondaire `RecommendationActionEnum`** (`Buy / Sell / Hold / NonActionable`, fichier `TradingPatternEnum.cs`) appartient à l'ancien périmètre *trading* retiré. Ne pas l'utiliser pour la recommandation pédagogique V1. Voir [06](06_ecarts_doc_code.md).

---

## 5. Lecture support (fondamentale + PEA)

### PeaEligibilityStatus
*Fichier : `Common/enums/PeaEligibilityEnums.cs`.*

| Code | Wording FR | Sens |
|---|---|---|
| `ConfirmedEligible` | Éligibilité PEA confirmée | Éligibilité avérée via registre gouverné. |
| `ConfirmedIneligible` | Non éligible PEA confirmée | Inéligibilité avérée. |
| `Unknown` | Éligibilité PEA non confirmée | Vérité non établie. **Jamais traité comme éligible** (RM-15). Ne doit **jamais** paraître implicitement positif. |

### PeaEligibilitySourceType
*Origine de la vérité PEA. Même fichier.*

| Code | Sens |
|---|---|
| `Unknown` | Source indéterminée. |
| `ManualRegistry` | Registre interne saisi manuellement. |
| `BrokerConfirmation` | Confirmation courtier. |
| `IssuerReference` | Référence émetteur. |
| `ExchangeReference` | Référence place de marché. |

### Disponibilité du score composite
*États de disponibilité de la lecture fondamentale (RM-14).*

| Code | Wording FR |
|---|---|
| `AVAILABLE` | Score composite disponible |
| `INSUFFICIENT_COVERAGE` | Score composite indisponible : couverture de données insuffisante |
| `PEA_UNKNOWN_BLOCKING` | Score composite indisponible : éligibilité PEA non confirmée |
| `CONFIRMED_INELIGIBLE_IN_UNIVERSE` | Score composite indisponible : instrument confirmé non éligible PEA dans cet univers |
| `UNSUPPORTED_UNIVERSE` | Score composite indisponible : univers demandé non pris en charge |
| `PROVIDER_DATA_INCOMPLETE` | Score composite indisponible : données fournisseur incomplètes ou indisponibles |

### Catégories fondamentales
*Le composite est la moyenne des catégories valides (min. 3 requises).*

`Profitability` (Rentabilité) · `Valuation` (Valorisation) · `FinancialStrength` (Solidité financière) · `Growth` (Croissance) · `Income` (Rendement).

### Disponibilité de la lecture support

| Code | Wording FR |
|---|---|
| `FULL` | Lecture support complète |
| `PARTIAL` | Lecture support partielle |
| `UNAVAILABLE` | Lecture support indisponible |

---

## 6. Instrument

### AssetType
*Fichier : `Common/enums/AssetTypeEnum.cs`.*

| Code | Wording FR | Statut V1 |
|---|---|---|
| `Stock` | Action | ✅ **Seul actif actif en V1** (actions FR). |
| `Etf` | ETF | ⛔ Présent dans l'enum mais **hors runtime V1** (réservé V2). |
| `Crypto` | Crypto | ⛔ Présent dans l'enum mais **hors périmètre** (roadmap non datée). |

> ⚠️ L'enum est plus large que le périmètre runtime. Un instrument `Etf`/`Crypto` doit produire `UnsupportedInstrument`, jamais un élargissement silencieux (RM-24). Voir [06](06_ecarts_doc_code.md).

### Marqueurs de fraîcheur des données

| Code | Wording FR |
|---|---|
| `FRESH` | Données à jour |
| `AGING` | Données à surveiller |
| `STALE` | Données obsolètes |
| `MISSING` | Données indisponibles |

---

## 7. Comptes & utilisateurs

### UserRole
*Fichier : `Common/enums/UserEnums.cs` (`UserRoleEnum`).*

| Code | Wording FR | Espace après login |
|---|---|---|
| `User` | Utilisateur | Espace `User`. |
| `Admin` | Administrateur | Espace `Admin`. |

> Le périmètre V1 retient **deux rôles** : `User` et `Admin`. `Admin` est l'unique rôle d'administration actif.

### UserStatus

| Code | Wording FR | Sens |
|---|---|---|
| `ACTIVE` | Actif | Compte utilisable. |
| `PENDING` | En attente | Compte créé non encore activé. |
| `DISABLED` | Désactivé | Accès suspendu. |

---

## 8. Notifications

### NotificationCategory
*Fichier : `Common/enums/NotificationEnums.cs`.*

| Code | Wording FR |
|---|---|
| `Watchlist` | Watchlist |
| `Analysis` | Analyse |
| `Learning` | Apprentissage |
| `Account` | Compte |

### NotificationStatus

| Code | Wording FR |
|---|---|
| `Unread` | Non lue |
| `Read` | Lue |

### NotificationTargetScreen
*Écran cible d'ouverture d'une notification.*

| Code | Écran cible |
|---|---|
| `InstrumentDetail` | Détail instrument |
| `AnalysisResult` | Résultat d'analyse |
| `HelpCenter` | Centre d'aide |
| `Account` | Compte |

### Déclencheurs d'alerte
*Origine métier d'une notification générée **proactivement** par la boucle de feedback (RM-25). Le `NotificationItem` enrichi porte ce déclencheur ([05](05_contrats_donnees_api.md#29-notificationitem--notificationpreferenceset)).*

| Code (`AlertTrigger`) | Wording FR | Condition | Catégorie | Écran cible |
|---|---|---|---|---|
| `PATTERN_STATE_CHANGE` | Changement d'état de pattern | `PatternStatus` passe `Monitoring`→`Confirmed`, ou `*`→`Invalidated` | `Analysis` | `AnalysisResult` |
| `LEVEL_CROSSED` | Niveau franchi | Le prix franchit `InvalidationPrice` ou `TargetPrice` d'un signal suivi (issu de l'évaluation ex post, RM-29) | `Analysis` | `InstrumentDetail` |
| `DATA_STALE` | Données obsolètes | Une valeur suivie bascule en fraîcheur `STALE` | `Watchlist` | `InstrumentDetail` |

> Garde-fous (RM-25b) : respect des préférences de notification ([NotificationPreferenceSet](05_contrats_donnees_api.md#29-notificationitem--notificationpreferenceset)) ; dédoublonnage par (instrument × déclencheur × jour) ; une alerte **route**, n'explique pas, et **n'est pas une prédiction**.

---

## 9. Aide (Help center)

### HelpCategory

`ANALYSIS_RESULT` · `SNAPSHOTS` · `ACCOUNT` · `GENERAL`.

### HelpTargetScreen

`ANALYSIS_RESULT` · `SNAPSHOT_COMPARISON` · `HISTORY` · `ACCOUNT`.

> Le help center est **déterministe et contextuel** : pas un chat, pas un ticketing, pas un fil de notifications (RM-23).

---

## 10. Vocabulaire produit transverse

| Terme | Définition |
|---|---|
| **Lecture marché** | Conclusion de la détection technique (patterns) sur le graphe. |
| **Lecture support** | Conclusion fondamentale relative + statut PEA. |
| **Lecture situation personnelle** | Effet du contexte de détention (FIFO) sur l'interprétation. |
| **Lecture paramètre** | Interprétation pédagogique d'un indicateur précis (4 couches, RM-16). |
| **Pattern principal** | Pattern retenu pour l'affichage prioritaire ; n'efface pas les alternatifs (RM-06). |
| **Pattern alternatif compatible** | Autre pattern crédible coexistant, restitué intégralement. |
| **Snapshot** | Photo persistée et versionnée d'une analyse complète (RM-19). |
| **PRU** | Prix de revient unitaire, **dérivé** des lignes ouvertes FIFO (jamais stocké comme vérité, RM-08). |
| **Univers** | Ensemble explicite d'instruments servant de base au classement relatif fondamental (V1 : actions FR éligibles PEA). |
| **Couverture** | Proportion de catégories fondamentales valides pour un instrument. |
| **Issue métier non exécutable** | État de premier rang ≠ erreur technique (RM-24). |
| **Évaluation ex post** | Rejeu a posteriori d'un signal persisté pour produire son `SignalOutcome` (RM-29). |
| **Alerte proactive** | Notification générée sans action utilisateur, sur un déclencheur (`AlertTrigger`, RM-25). |
| **Plan d'action** | Bloc « Vos prochaines étapes » déterministe reformulant les vérités d'une analyse (RM-26). |
| **Confiance expliquée** | Décomposition en critères du `ConfidenceLabel` (RM-27). |
| **Glossaire inline** | Info-bulles tirées du dictionnaire gouverné, au moment du doute (RM-28). |
| **KPI** | Indicateur de pilotage admin, formule explicite, source traçable, n'invente aucune vérité (RM-29). |
| **Funnel d'activation** | Suite d'étapes inscription → 1ère watchlist → 1ère analyse → 1ère transaction. |
| **Cohorte** | Groupe d'utilisateurs partageant une date d'inscription, suivi en rétention. |

### Disponibilité d'un KPI
*État métier d'un KPI qui ne peut être calculé (cohérent avec les familles d'états, [03 §A.2](03_specification_ecrans.md#a2-familles-détats-obligatoires)).*

| Code | Wording FR (admin) |
|---|---|
| `KPI_AVAILABLE` | Indicateur disponible |
| `KPI_INSUFFICIENT_DATA` | Données insuffisantes sur la période |
| `KPI_WINDOW_TOO_YOUNG` | Fenêtre trop récente pour ce calcul (ex. rétention J+30 d'une cohorte récente) |

---

## 11. Cohérence d'usage

Si une même vérité backend apparaît sur plusieurs écrans (liste, détail, rail de synthèse, notification, historique, comparaison, admin), elle doit utiliser **la même famille de wording français**. Aucun écran n'invente un libellé plus court qui change le sens.
