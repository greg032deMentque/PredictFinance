# 05 — Contrats de données & surface API (V1)

> **Propriétaire de** : structures de données canoniques + surface API écran par écran.
> Les enums référencés ici sont définis **uniquement** dans [02](02_glossaire_et_taxonomies.md). Les écrans dans [03](03_specification_ecrans.md). L'écart entre cette cible et le code réel dans [06](06_ecarts_doc_code.md).

---

## 1. Portée

Ce document gèle les contrats nécessaires pour : le cœur d'analyse de marché, l'expérience utilisateur authentifiée, les surfaces self-service et la gouvernance admin. Il **ne gèle pas** les détails d'implémentation, la composition UI, ni la techno de transport.

**Convention** : `Mandatory` = obligatoire ; `Optional` = optionnel. Les types `enum` renvoient à [02](02_glossaire_et_taxonomies.md).

---

## 2. Structures de données canoniques

### 2.1 Instrument
*Instrument financier analysable, indépendant de tout payload fournisseur. Propriétaire : domaine + persistance API.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `instrumentId` | Mandatory | string | Identifiant interne stable. |
| `symbol` | Mandatory | string | Symbole normalisé côté domaine. |
| `providerSymbol` | Mandatory | string | Symbole du fournisseur de données actif. |
| `displayName` | Mandatory | string | Nom lisible débutant. |
| `marketCode` | Mandatory | string | Code marché/place normalisé. |
| `countryCode` | Mandatory | string | Pays de cotation ; contraint le périmètre France-first. |
| `currencyCode` | Mandatory | string | Devise ISO de cotation. |
| `assetType` | Mandatory | enum `AssetType` | Valeur active V1 : `Stock` (cf. [02](02_glossaire_et_taxonomies.md#assettype)). |
| `isActive` | Mandatory | bool | Éligible aux nouvelles actions watchlist/portefeuille/analyse. |
| `lastProfileSyncUtc` | Optional | datetime | Marqueur de fraîcheur des métadonnées. |
| `summary` | Optional | string | Courte description pédagogique (pas du texte brut fournisseur). |

**Invariants** : `symbol/providerSymbol/displayName/marketCode/currencyCode/assetType` jamais vides ; unicité par `symbol + marketCode + assetType` ; `isActive=false` ⇒ pas de nouvelle analyse mais snapshots historiques restent valides ; **runtime V1 = actions FR actives uniquement** (RM-24).

### 2.2 PortfolioLine
*Lot de détention ouvert pour un utilisateur et un instrument.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `portfolioLineId` | Mandatory | string | Identifiant interne stable. |
| `userId` | Mandatory | string | Utilisateur propriétaire. |
| `instrumentId` | Mandatory | string | Instrument détenu. |
| `quantity` | Mandatory | decimal | Quantité restante sur cette ligne. |
| `unitBuyPrice` | Mandatory | decimal | Prix d'achat unitaire de la ligne. |
| `buyDate` | Mandatory | date | Date d'achat. |
| `feesAmount` | Mandatory | decimal | Frais imputés à la ligne. |
| `currencyCode` | Mandatory | string | Devise de valorisation. |
| `sourceReference` | Optional | string | Lien vers transaction/import d'origine. |
| `note` | Optional | string | Mémo utilisateur non analytique. |

**Invariants** : `quantity > 0` ; `unitBuyPrice > 0` ; `feesAmount >= 0` ; `buyDate ≤ asOfDate` ; plusieurs lignes possibles par instrument ; **PRU dérivé** des lignes ouvertes (jamais stocké comme vérité, RM-08).

### 2.3 PortfolioContext
*Contexte de détention fourni à la recommandation/explication, sans polluer la détection.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `userId` | Mandatory | string | Utilisateur. |
| `instrumentId` | Mandatory | string | Instrument analysé. |
| `holdsInstrument` | Mandatory | bool | Détient au moins une quantité. |
| `openLineCount` | Mandatory | int | Nombre de lignes ouvertes. |
| `totalQuantityHeld` | Mandatory | decimal | Somme des quantités ouvertes. |
| `averageUnitCost` | Mandatory si `holdsInstrument=true` | decimal? | PRU dérivé des lignes ouvertes. |
| `currencyCode` | Mandatory | string | Devise du contexte. |
| `openLines` | Mandatory | array | Résumés des lignes ouvertes (`quantity`, `unitBuyPrice`, `buyDate`, `feesAmount`, `currencyCode`). |
| `oldestOpenBuyDate` | Optional | date | Plus ancienne ligne ouverte. |
| `latestOpenBuyDate` | Optional | date | Plus récente ligne ouverte. |

**Invariants** : si `holdsInstrument=false` ⇒ `openLineCount=0`, `totalQuantityHeld=0`, `averageUnitCost=null`, `openLines=[]` ; `openLines` = quantités restantes **après FIFO strict** (RM-08) ; la détection peut lire `instrumentId`/`asOfDate` mais **jamais** les champs de détention (RM-07).

### 2.4 AnalysisRequest
*Requête d'une analyse à la demande, instrument unique, bougies journalières.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `instrumentId` | Mandatory (API) | string | Instrument canonique à analyser. |
| `requestedPatternIds` | Optional (API) | string[] | Sous-ensemble de patterns activés ; vide/null = jeu activé par défaut. |
| `asOfDate` | Optional (API) | date | Date d'analyse ; défaut = dernière clôture journalière. |
| `userId` | Mandatory (résolu) | string | Issu du contexte d'auth, jamais du client. |
| `instrument` | Mandatory (résolu) | Instrument | Contexte instrument chargé par l'API. |
| `portfolioContext` | Mandatory (résolu) | PortfolioContext | Contexte de détention chargé par l'API. |
| `candleInterval` | Mandatory (résolu) | enum | Fixé à `ONE_DAY` en V1. |
| `analysisMode` | Mandatory (résolu) | enum | Fixé à `ON_DEMAND` en V1. |
| `resolvedPatternIds` | Mandatory (résolu) | string[] | Jeu exécutable final après politique/activation. |
| `historyStartDate` | Mandatory (résolu) | date | Calculée serveur depuis la fenêtre la plus profonde requise. |
| `historyEndDate` | Mandatory (résolu) | date | Égale `asOfDate` après résolution. |

**Invariants** : le client ne contrôle pas `startDate`/`endDate` bruts ; la plage est calculée serveur ; `requestedPatternIds` ne contient que des patterns activés ; requête mono-instrument journalière uniquement.

### 2.5 PatternAssessment
*Sortie cœur par pattern, sans mélanger détection / validation / invalidation / scoring / risque / explication.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `assessmentId` | Mandatory (persist.) | string | Id d'évaluation stable. |
| `patternId` | Mandatory | string | Identifiant canonique de pattern. |
| `displayName` | Mandatory | string | Nom lisible débutant. |
| `pedagogicalDescription` | Mandatory | string | Description pédagogique courte. |
| `analysisWindow` | Mandatory | object | `interval`, `startDate`, `endDate`, `requiredCandles`, `actualCandles`. |
| `detection` | Mandatory | object | Faits de détection **uniquement**. |
| `validation` | Mandatory | object | `ValidationState` **uniquement**. |
| `invalidation` | Mandatory | object | `InvalidationState` **uniquement**. |
| `scoring` | Mandatory | object | Confiance / score **uniquement** (`ConfidenceLabel`). |
| `riskHints` | Mandatory | object | Indices de risque **uniquement**. |
| `explanation` | Mandatory | object | Métadonnées d'explication **uniquement**. |
| `trace` | Mandatory | object | Traçabilité versions pattern/règle **uniquement**. |

Enums associés : `PatternStatus`, `ValidationState`, `InvalidationState`, `ConfidenceLabel` ([02](02_glossaire_et_taxonomies.md)).

**Règle de texte déterministe (RM-02)** : tous les textes explicatifs obligatoires sont déterministes, générés par règles et versionnables — au minimum `detection.statusReason`, `validation.reason`, `invalidation.reason`, `explanation.whyListed`, `explanation.pedagogicalSummary`, `recommendation.rationale`.

### 2.6 Recommendation
*Guidance utilisateur après évaluation des patterns et contexte de détention.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `recommendationId` | Mandatory (persist.) | string | Identifiant stable. |
| `kind` | Mandatory | enum `RecommendationKind` | Verbe d'action gelé. |
| `holdingContext` | Mandatory | enum `HoldingContext` | `NOT_HELD` / `HELD`. |
| `rationale` | Mandatory | string | Justification pédagogique déterministe. |
| `basedOnPatternIds` | Mandatory | string[] | Patterns ayant informé la reco. |
| `reviewHorizonDays` | Optional | int | Horizon de revue **seulement** si une règle déterministe le justifie. |
| `policyVersion` | Mandatory | string | Version des règles de politique. |
| `warningText` | Optional | string | Note de prudence pour scénarios ambigus. |

**Invariants** (RM-10, RM-11) : `NOT_HELD` ⇒ `kind ∈ {Monitor, Wait, Buy}` ; `HELD` ⇒ `kind ∈ {Hold, Reinforce, Lighten, Sell, Wait}` ; jamais de stop loss / take profit / niveau d'invalidation / ratio R/R (ceux-ci restent dans `riskHints`) ; ne contient pas de faits de détection (référence par `basedOnPatternIds`).

### 2.6.1 ConfidenceBreakdown
*Décomposition lisible du niveau de confiance d'un pattern (RM-27). Dérivée des sous-objets de `PatternAssessment`, jamais d'un recalcul frontend. Cible — à exposer au payload, voir [06](06_ecarts_doc_code.md).*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `label` | Mandatory | enum `ConfidenceLabel` | Niveau global (`LOW`/`MEDIUM`/`HIGH`). |
| `criteria` | Mandatory | array | Liste de critères. |
| `criteria[].code` | Mandatory | string | Identifiant gouverné du critère (ex. `IMPULSE_CLEAR`). |
| `criteria[].label` | Mandatory | string | Libellé FR gouverné (dictionnaire/wording). |
| `criteria[].state` | Mandatory | enum | `MET` ✅ / `PARTIAL` ⚠️ / `ABSENT` ❌. |
| `criteria[].source` | Mandatory | enum | `DETECTION` / `VALIDATION` / `INVALIDATION` (origine du critère). |

**Invariants** : explique le `label`, **ne le modifie pas** ; chaque critère trace son origine ; libellés gouvernés (RM-17), aucun texte frontend libre.

### 2.6.2 ActionPlan
*Plan d'action déterministe « Vos prochaines étapes » (RM-26). Reformule des vérités déjà calculées, sans en introduire de nouvelles. Cible.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `steps` | Mandatory | array | 2-3 étapes ordonnées. |
| `steps[].kind` | Mandatory | enum | `NOTE_LEVEL` / `REVIEW_AT` / `SET_ALERT` / `HOLDING_REMINDER` / `WAIT_FOR_DATA`. |
| `steps[].label` | Mandatory | string | Libellé FR gouverné. |
| `steps[].sourceField` | Mandatory | string | Champ d'analyse dont l'étape découle (ex. `riskHints.invalidationPrice`, `recommendation.reviewHorizonDays`). |
| `steps[].value` | Optional | string | Valeur reprise telle quelle de la source (jamais recalculée). |
| `steps[].alertTrigger` | Optional | enum `AlertTrigger` | Renseigné si `kind = SET_ALERT`. |
| `policyVersion` | Mandatory | string | Version des gabarits de plan d'action. |

**Invariants** (RM-26) : **aucun nouveau chiffre** — toute `value` provient d'un `sourceField` ; chaque étape est traçable ; respecte le contexte détenue/non détenue (RM-10) ; en issue non-exécutable, seules les étapes `WAIT_FOR_DATA` / `HOLDING_REMINDER` sont admissibles.

### 2.7 AnalysisSnapshot
*Modèle d'historique canonique persisté — un événement d'analyse complet.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `snapshotId` | Mandatory | string | Identifiant racine stable. |
| `userId` | Mandatory | string | Propriétaire. |
| `instrumentId` | Mandatory | string | Instrument analysé. |
| `requestedPatternIds` | Mandatory | string[] | Patterns demandés. |
| `executedPatternIds` | Mandatory | string[] | Patterns réellement exécutés. |
| `outcome` | Mandatory | enum `AnalysisOutcome` | Issue métier finale. |
| `requestedAtUtc` | Mandatory | datetime | Heure de requête. |
| `completedAtUtc` | Mandatory | datetime | Heure de complétion. |
| `engineVersion` | Mandatory | string | Version du moteur. |
| `portfolioContext` | Mandatory | PortfolioContext | Contexte utilisé pour reco/explication. |
| `recommendation` | Optional | Recommendation | Présente si résultat exécutable. |
| `actionPlan` | Optional | ActionPlan | Plan d'action (§2.6.2), présent si résultat exécutable. |
| `patternAssessments` | Mandatory | PatternAssessment[] | Lignes par pattern (chacune porte sa `ConfidenceBreakdown`, §2.6.1). |

> Doit persister assez de **lecture support** ET de **niveaux de risque** (`TargetPrice` / `InvalidationPrice`) pour permettre l'audit, la comparaison et **l'évaluation ex post** (§2.7.1) sans reconstruction (RM-20). Ce point est un **écart** avec le code actuel à valider — voir [06](06_ecarts_doc_code.md).

### 2.7.1 SignalOutcome (issue réalisée ex post)
*Résultat a posteriori d'un signal persisté (RM-29). Produit par le job d'évaluation ex post. Stockage à arbitrer (colonne sur `PatternAssessment` vs table dédiée, [06](06_ecarts_doc_code.md)). Cible.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `signalOutcomeId` | Mandatory | string | Identifiant stable. |
| `snapshotId` | Mandatory | string | Snapshot source. |
| `patternAssessmentId` | Mandatory | string | Évaluation de pattern source. |
| `outcome` | Mandatory | enum | `TARGET_HIT` / `INVALIDATION_HIT` / `STILL_OPEN` / `NOT_EVALUABLE` ([02](02_glossaire_et_taxonomies.md#signaloutcome-issue-r%C3%A9alis%C3%A9e-ex-post)). |
| `evaluationWindowDays` | Mandatory | int | Fenêtre d'évaluation appliquée (à arbitrer, [06](06_ecarts_doc_code.md)). |
| `evaluatedAtUtc` | Mandatory | datetime | Date du calcul ex post. |
| `firstHitAtUtc` | Optional | date | Date du premier franchissement de niveau, si applicable. |
| `policyVersion` | Mandatory | string | Version de la règle d'évaluation ex post. |

**Invariants** : déterministe ; compare `PriceHistory` aux niveaux `TargetPrice` / `InvalidationPrice` **persistés au snapshot** (jamais recalculés à partir de l'état courant, RM-20) ; alimente alertes (RM-25) et KPI qualité des signaux (§3.bis).

### 2.8 AuthenticatedUserProfile
*Profil self-service pour `account` / `account-security`.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `userId` | Mandatory | string | Identifiant utilisateur stable. |
| `displayName` | Mandatory | string | Nom de compte lisible. |
| `email` | Mandatory | string | Email de connexion/contact. |
| `role` | Mandatory | enum `UserRole` | `User` / `Admin` ([02](02_glossaire_et_taxonomies.md#userrole)). |
| `status` | Mandatory | enum `UserStatus` | Cycle de vie du compte. |
| `preferredLanguageCode` | Optional | string | Langue UI préférée. |
| `preferredMarketScope` | Optional | string | Préférence seulement, **pas** un override de périmètre V1. |
| `securitySummary` | Mandatory | object | `hasPassword`, `lastPasswordChangeUtc?`, `recoveryEmailMasked?`. |
| `notificationPreferences` | Mandatory | NotificationPreferenceSet | Réglages de notification. |

**Invariants** : `role` pilote le routage post-login ; les préférences de notification se gèrent depuis le compte, pas le centre de notifications (RM-23).

### 2.9 NotificationItem & NotificationPreferenceSet

**NotificationItem** : `notificationId`, `userId`, `category` (enum `NotificationCategory`), `status` (enum `NotificationStatus`), `title`, `summary`, `createdAtUtc`, `targetScreen?` (enum `NotificationTargetScreen`), `targetEntityId?`, `trigger?` (enum `AlertTrigger`, [02](02_glossaire_et_taxonomies.md#d%C3%A9clencheurs-dalerte)), `dedupeKey?` (string).
- `trigger` est renseigné pour les **alertes proactives** (RM-25) ; absent pour les notifications produit classiques.
- `dedupeKey` = (instrument × trigger × jour) garantit le dédoublonnage (RM-25b).
**Invariant** : le centre priorise et route ; il ne possède **pas** l'édition des préférences (RM-23) ; une alerte route et n'explique pas, et n'est pas une prédiction (RM-25b).

**NotificationPreferenceSet** : `watchlistEnabled`, `analysisEnabled`, `learningEnabled`, `accountEnabled` (tous bool, Mandatory).
- Une catégorie désactivée **bloque** la génération des alertes correspondantes (RM-25b). Le mapping déclencheur→catégorie est en [02](02_glossaire_et_taxonomies.md#d%C3%A9clencheurs-dalerte).

### 2.10 HelpEntry
`helpEntryId`, `category` (enum `HelpCategory`), `title`, `summary`, `targetScreen?` (enum `HelpTargetScreen`), `displayOrder`.
**Invariant** : aide contextuelle et déterministe ; pas un chat / ticketing / fil de notifications.

### 2.11 Recouvrement de mot de passe
**PasswordRecoveryRequest** : `email` (Mandatory). Invariant : la réponse **ne révèle pas** l'existence du compte.
**PasswordResetCommand** : `resetToken`, `newPassword`, `confirmPassword` (Mandatory). Invariant : `newPassword == confirmPassword` à la validation.

### 2.12 AdminUserSummary
`userId`, `displayName`, `email`, `role` (enum `UserRole`), `status` (enum `UserStatus`), `lastAccessUtc?`.
**Invariant** : couvre listage/filtrage/ouverture du détail ; n'implique **pas** de modèle multi-tenant.

### 2.13 Lecture support (fondamentale + PEA) — contrats
*Capacité parallèle (RM-12). Traçabilité requise : `scoringVersion`, `universeId`, `asOfUtc`, `providerId`, `snapshotId`, `peaEligibilityStatus`, `peaPolicyVersion`.*

- **FundamentalCategoryScore** : `category` (Profitability/Valuation/FinancialStrength/Growth/Income), `percentileScore` (0–1), `metricsPresent`, `metricsMissing`.
- **CompositeScore** : `availability` (cf. [02](02_glossaire_et_taxonomies.md#disponibilit%C3%A9-du-score-composite)), `value?` (présent si `AVAILABLE`), `coverageRatio`, `categoriesPresent`, `categoriesMissing`.
- **PeaEligibility** : `status` (enum `PeaEligibilityStatus`), `sourceType` (enum `PeaEligibilitySourceType`), `checkedUtc`, `policyVersion`.
**Invariants** : composite indisponible si couverture < 3 catégories valides ou PEA non `ConfirmedEligible` (RM-14) ; `Unknown` jamais traité comme éligible (RM-15) ; vérité PEA = registre interne, pas fournisseur de marché.

### 2.14 KpiMetric (pilotage admin)
*Forme générique d'un KPI exposé à l'admin (RM-29). Cible.*

| Champ | Statut | Type | Sens |
|---|---|---|---|
| `kpiId` | Mandatory | string | Identifiant stable du KPI. |
| `family` | Mandatory | enum | `SIGNAL_QUALITY` / `ENGAGEMENT` / `USAGE_FUNNEL` / `OPS_HEALTH`. |
| `availability` | Mandatory | enum | `KPI_AVAILABLE` / `KPI_INSUFFICIENT_DATA` / `KPI_WINDOW_TOO_YOUNG` ([02](02_glossaire_et_taxonomies.md#disponibilit%C3%A9-dun-kpi)). |
| `value` | Optional | number | Valeur courante (présente si `KPI_AVAILABLE`). |
| `unit` | Optional | string | Unité (%, count, ms…). |
| `series` | Optional | array | Points temporels `{ date, value }` pour tendance/sparkline. |
| `window` | Mandatory | enum | `D7` / `D30` / `D90` / fenêtre custom. |
| `previousValue` | Optional | number | Valeur de la période précédente (variation). |
| `formulaRef` | Mandatory | string | Référence de la formule (§3.bis). |
| `sourceRef` | Mandatory | string | Source de données traçable (entité/colonne ou `Analytic`). |

**Invariants** (RM-29) : tout KPI déclare `formulaRef` + `sourceRef` ; **n'invente aucune vérité** (agrège l'existant) ; expose toujours une `availability` ; les KPI nominatifs respectent l'anonymisation (RM-29b).

> Structures spécialisées dérivées : `RetentionCohort` (cohorte × {J+1, J+7, J+30}), `ActivationFunnelStep` (étape, taux de passage, délai médian), `ConfidenceCalibrationRow` (`ConfidenceLabel` × taux d'atteinte de cible). Toutes héritent des invariants de `KpiMetric`.

---

## 3. Surface API — endpoints réels existants

*Préfixe : `${apiUrl}`. Tous protégés sauf parcours anonymes. Source : `client-finance.service.ts` + contrôleurs `FinanceBack`.*

| Méthode | Endpoint | Écran(s) servis |
|---|---|---|
| GET | `/ClientFinance/dashboard` | Home `C.1` |
| GET | `/ClientFinance/assets/search?query=` | Watchlist `C.2`, Analyse `C.4` |
| GET | `/ClientFinance/watchlist` | Watchlist `C.2` |
| POST | `/ClientFinance/watchlist` | Watchlist `C.2` (ajout) |
| DELETE | `/ClientFinance/watchlist/{symbol}` | Watchlist `C.2` (retrait) |
| GET | `/ClientFinance/quote/{symbol}` | Watchlist, Détail instrument |
| GET | `/ClientFinance/portfolio` | Portefeuille `C.3` |
| GET | `/ClientFinance/history?take=` | Historique `C.9` |
| GET | `/ClientFinance/analysis/{analysisId}` | Résultat `C.5` |
| GET | `/ClientFinance/instruments/{symbol}` | Détail instrument `C.6` |
| GET | `/ClientFinance/instruments/{symbol}/analysis-history?take=` | Détail instrument `C.6`, Historique filtré |
| POST | `/ClientFinance/transactions` | Portefeuille `C.3` |
| GET | `/ClientFinance/transactions?take=` | Portefeuille `C.3` |
| DELETE | `/ClientFinance/transactions/{transactionId}` | Portefeuille `C.3` |
| POST | `/ClientFinance/analysis/run` | Analyse `C.4` → `C.5` |
| GET | `/ClientFinance/analysis/recent?take=` | Home `C.1` |
| POST | `/ClientFinance/simulation/run` | Simulation `C.8` |
| — | `Account*` (register, login, reset, change, refresh, logout, profile) | Auth `B.*`, Compte `C.15` |
| — | `Admin*` (overview, instrument-registry, pea-registry, scoring-policy, parameter-dictionary, snapshot-audit, wording-versions, data-quality) | Admin `D.*` |

> Le contrôleur `Trading` est **retiré** (renvoie `410 Gone`). Ne pas l'utiliser.

---

## 3.bis KPI — familles, formules et sources

*Référentiel des KPI admin (RM-29). Écrans : [03 §D.10](03_specification_ecrans.md#d10--pilotage-kpi). Chaque KPI agrège la vérité déjà persistée — il n'en crée aucune. Faisabilité : ✅ données prêtes (agrégation) · 🟠 job/colonne requis · 🔵 dérivé des logs `Analytic`.*

### Famille A — Qualité des signaux (ex post) ⭐

| KPI | Formule | Source | Faisabilité |
|---|---|---|---|
| Taux de confirmation | confirmés ÷ (formés + surveillés + confirmés) | `PatternAssessment` historisé | ✅ |
| Taux d'invalidation | invalidés ÷ total évalués | `PatternAssessment` | ✅ |
| **Taux d'atteinte de cible** | `TARGET_HIT` ÷ (`TARGET_HIT` + `INVALIDATION_HIT`) | `SignalOutcome` (§2.7.1) | 🟠 |
| **Calibration de la confiance** | taux d'atteinte par bucket `LOW`/`MEDIUM`/`HIGH` | `PatternAssessment.Confidence` + `SignalOutcome` | 🟠 |
| Performance modèle | évolution `Precision`/`F1`/`RocAuc` par `ModelVersion` | `ModelSnapshot` | ✅ |
| Performance par pattern | taux d'atteinte par `PatternId` | `PatternId` + `SignalOutcome` | 🟠 |

> Pré-requis : le **job d'évaluation ex post** qui peuple `SignalOutcome` en rejouant `PriceHistory` contre les niveaux persistés au snapshot.

### Famille B — Engagement & rétention

| KPI | Formule | Source | Faisabilité |
|---|---|---|---|
| Inscriptions / jour | `count(User)` par `date(CreatedAt)` | `User.CreatedAt` | ✅ |
| DAU / WAU / MAU | utilisateurs distincts actifs sur 1/7/30 j | `RefreshToken.CreatedAtUtc` ou `User.LastConnection` | ✅ |
| Stickiness | DAU ÷ MAU | idem | ✅ |
| Rétention par cohorte | % d'une cohorte revenue à J+1/J+7/J+30 | `User.CreatedAt` × activité | ✅ |
| Analyses / utilisateur actif | `count(AnalysisRun)` ÷ actifs | `AnalysisRun` | ✅ |
| Taux de lecture notifications | `Read` ÷ total créées | `UserNotification.ReadAtUtc`/`CreatedAtUtc` | ✅ |

### Famille C — Usage & funnel d'activation

| KPI | Formule | Source | Faisabilité |
|---|---|---|---|
| Funnel d'activation | taux de passage inscription → 1ère watchlist → 1ère analyse → 1ère transaction | min timestamps `User`/`UserAsset`/`AnalysisRun`/`AssetTransaction` | ✅ |
| Délai médian entre étapes | médiane des écarts | idem | ✅ |
| Patterns les plus détectés | `count` par `PatternId` | `PatternAssessment` | ✅ |
| Instruments les plus analysés | `count` par `AssetId` | `AnalysisRun` | ✅ |
| Écrans les plus visités | `count` par `Request` | table `Analytic` | 🔵 |

### Famille D — Santé opérationnelle & data

| KPI | Formule | Source | Faisabilité |
|---|---|---|---|
| Taux d'échec d'analyse | `Failed` ÷ total runs | `AnalysisRun.Status` | ✅ |
| Latence d'analyse p50/p95 | percentiles (`CompletedAtUtc` − `StartedAtUtc`) | `AnalysisRun` | ✅ |
| Couverture PEA | instruments à statut connu ÷ actifs | `AssetPeaEligibility` | ✅ |
| Complétude des snapshots | runs complétés avec `ModelSnapshot` **et** `DecisionSignal` ÷ complétés | relations `AnalysisRun` | ✅ |
| Fraîcheur des données | distribution `FRESH`/`AGING`/`STALE`/`MISSING` | `Asset.LastProfileSyncUtc` | ✅ |

> État de départ réel : l'`AdminOverview` actuel ne renvoie que 10 compteurs instantanés et l'`AdminDataQuality` que 5 contrôles de complétude (pas de tendance). La table `Analytic` (logs HTTP) est présente mais non agrégée. Détail en [06](06_ecarts_doc_code.md).

---

## 4. Lacunes de surface API (cible vs. existant)

Classification : `EXISTE` · `EXISTE_PAYLOAD_INSUFFISANT` · `ROUTE_MANQUANTE` · `À_ARBITRER`.

| Besoin écran | Statut | Note |
|---|---|---|
| Lecture portefeuille structurée (lignes FIFO + PRU dérivé) | `EXISTE_PAYLOAD_INSUFFISANT` | Vérifier que `portfolio` expose les lignes ouvertes et le contexte FIFO complet. |
| Détail paramètre (`C.7`) côté user | `ROUTE_MANQUANTE` | Pas de route user ; le dictionnaire existe côté admin (`D.6`). |
| Comparaison de snapshots côté user (`C.10`) | `ROUTE_MANQUANTE` | Existe côté admin (`/admin/snapshot-audit/compare`). |
| Learn (`C.11`) | `ROUTE_MANQUANTE` | Aucune surface. |
| Help center (`C.13`) | `ROUTE_MANQUANTE` | Aucune surface ; contrat `HelpEntry` défini mais non exposé. |
| Notifications (lecture/maj statut) | `À_ARBITRER` | Composant front existe ; confirmer la persistance/endpoints. |
| Lecture support (composite + PEA) dans le payload d'analyse | `À_ARBITRER` | Confirmer que `analysis/{id}` et `instruments/{symbol}` exposent la lecture support complète et la persistent au snapshot (RM-20). |
| Confiance expliquée (`ConfidenceBreakdown`) dans le payload | `EXISTE_PAYLOAD_INSUFFISANT` | Les sous-objets `detection`/`validation`/`invalidation` existent ; confirmer leur sérialisation comme critères (§2.6.1). |
| Plan d'action (`ActionPlan`) | `ROUTE_MANQUANTE` | À générer côté backend (§2.6.2) et exposer au payload d'analyse + snapshot. |
| Alertes proactives (génération + déclencheur) | `ROUTE_MANQUANTE` | Nécessite la ré-évaluation périodique + `NotificationItem.trigger` (§2.9) ; mutualiser avec le job ex post. |
| Évaluation ex post (`SignalOutcome`) | `ROUTE_MANQUANTE` | Job batch + persistance (§2.7.1) ; pré-requis des KPI famille A. |
| KPI admin (4 familles) | `ROUTE_MANQUANTE` | Agrégations sur données prêtes (§3.bis) ; familles B/C/D faciles, famille A dépend de `SignalOutcome`. |
| Détail paramètre user / glossaire inline | `ROUTE_MANQUANTE` | Exposer le dictionnaire gouverné (admin `D.6`) en **lecture user** (`C.7` + info-bulles). |

> Détail et preuves : [06](06_ecarts_doc_code.md). Règle : ne **jamais** confondre route absente, payload insuffisant et choix de design non tranché.

---

## 5. Hors de ce gel (volontairement différé)

SSO · MFA · contrats organisation/workspace · aide conversationnelle · orchestration des canaux de notification · push temps réel · explications IA libres · synchronisation courtier · contrats d'exécution/trading.
