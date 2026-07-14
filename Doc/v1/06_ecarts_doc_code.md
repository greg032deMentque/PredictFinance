# 06 — Écarts entre la cible documentée et le code réel

> **Propriétaire de** : tous les écarts entre les documents cibles (01→05) et l'état réel du code (`FinanceBack/`, `FinanceFront/`), au 2026-05-21.
> **Principe** : les documents 01→05 décrivent le **produit cible V1 cohérent**. Cette page est le **seul** endroit où l'on dit honnêtement « voici ce qui diffère aujourd'hui ». Aucun écart n'est caché ; aucun document cible n'est dégradé pour coller à la dette.

## Classification

| Code | Sens |
|---|---|
| **DIVERGENCE-DOC** | L'ancienne doc affirmait quelque chose de faux ; corrigé dans le nouveau pack, conforme au code. |
| **DETTE-TECH** | Le code dévie de la cible pour raison historique ; à résorber, ne pas propager. |
| **À-CONSTRUIRE** | Écran/route/capacité cible non encore construit. |
| **À-ARBITRER** | Décision produit ou technique non tranchée ; nécessite un choix humain. |

---

## 1. Divergences de documentation corrigées (DIVERGENCE-DOC)

| # | Sujet | Ancienne doc disait | Code réel | Résolution |
|---|---|---|---|---|
| D-01 | `AnalysisOutcome` | `ANALYSIS_COMPLETED / INSTRUMENT_NOT_ELIGIBLE / INSUFFICIENT_MARKET_DATA / NO_ENABLED_PATTERN / NO_CREDIBLE_PATTERN` (dans `contract_freeze.md`) | `CrediblePatternFound / MultipleCompatiblePatterns / NoCrediblePattern / InsufficientData / UnsupportedInstrument / UnsupportedContext` (`Common/enums/AnalysisOutcome.cs`) | Pack aligné sur le **code** ([02](02_glossaire_et_taxonomies.md#analysisoutcome)). |
| D-02 | `UserRole` | `USER / ADMIN` (deux rôles) | `User / Admin` (`Common/enums/UserEnums.cs`) | Pack aligné sur les **deux** rôles actifs. |
| D-03 | `AssetType` | « EQUITY uniquement » | `Stock / Etf / Crypto` (`Common/enums/AssetTypeEnum.cs`) | L'enum est plus large que le périmètre runtime ; documenté en [02](02_glossaire_et_taxonomies.md#assettype). Garde-fou : `UnsupportedInstrument` pour Etf/Crypto (lié à T-04). |
| D-04 | `PatternStatus` | Cohérent (`Forming/Monitoring/Confirmed/Invalidated/Completed`) | Idem + état `Absent` dans le **read model** séparé (`PatternProgressStatusEnum`) | Précisé que `Absent` est un état de read model, pas de domaine. |
| D-05 | `DOUBLE_TOP` | Encore cité comme pattern actif dans des docs/résidus | Retiré ; **rejeté** par `PatternIds.RequireActivePatternId` (test dédié) | Aucune surface ne le présente comme supporté ([02](02_glossaire_et_taxonomies.md#patterns-pris-en-charge-v1)). |
| D-06 | Verbes de reco | Listés ailleurs sans rappeler l'enum secondaire | `RecommendationKind` (V1) **et** `RecommendationActionEnum` (trading retiré) coexistent | Pack désigne `RecommendationKind` comme canonique ; `RecommendationActionEnum` = legacy à ne pas utiliser. |

---

## 2. Dette technique (DETTE-TECH)

> Issues du registre de dérogations existant + de l'inspection de code. À résorber lors des milestones qui touchent la zone concernée — **ne pas étendre**.

| # | Dette | Évidence | Règle d'opération |
|---|---|---|---|
| T-01 | Couplage `Services → ViewModels` | Les services dépendent des ViewModels (devrait être l'inverse) | Ne pas introduire de **nouveau** couplage sans justification explicite. |
| T-02 | Contrats d'analyse dans `Common/AnalysisV1` | Pas de projet `Contracts` dédié ; contrats éparpillés `Common/AnalysisV1` | Ne pas étendre inutilement `AnalysisV1` ; viser un emplacement de contrats cohérent. |
| T-03 | États fermés stockés en `string` | `AnalysisRun.Status`, `PatternAssessment.PatternId/Phase`, **`HoldingContext`** stockés en string au lieu d'enums | Ne **pas** créer de nouveau champ d'état fermé en string ; cible = enums ([02](02_glossaire_et_taxonomies.md)). |
| T-04 | Périmètre d'actif plus large que le runtime | `AssetType` contient `Etf/Crypto` mais V1 = actions FR | Garantir que tout instrument non-`Stock`/non-FR produit `UnsupportedInstrument` (RM-24), **pas** un élargissement silencieux. |
| T-05 | Profondeur de snapshot pour la lecture support | À vérifier : le snapshot persiste-t-il assez de lecture support (composite, PEA, couverture) pour audit/comparaison sans reconstruction (RM-20) ? | Si insuffisant, compléter `AnalysisSnapshot` avant d'industrialiser l'historique. |

---

## 3. Écrans / capacités cible à construire (À-CONSTRUIRE)

> Frontend Angular réel comparé à l'inventaire canonique d'écrans ([03](03_specification_ecrans.md)).
>
> **Mise à jour 2026-05-28** : colonne **Priorité** ajoutée suite à l'analyse multi-perspective (voir [09](09_diagnostic_strategique.md#6-recommandations-p0--p1--p2)). **P0** = bloquant avant lancement · **P1** = avant croissance · **P2** = post-PMF.

| # | Priorité | Écran cible | État front | Note |
|---|---|---|---|---|
| C-01 | P2 | `parameter-detail` (user, `C.7`) | 🔴 non routé côté user | Le dictionnaire existe côté admin (`D.6`) ; manque la surface user + sa route/endpoint. |
| C-02 | P2 | `snapshot-comparison` (user, `C.10`) | 🔴 non routé côté user | L'équivalent admin existe (`/admin/snapshot-audit/compare`). |
| C-03 | P2 | `learn` (`C.11`) | 🔴 non construit | Aucune route/composant. |
| C-04 | P1 | `help-center` (`C.13`) | 🔴 non construit | Contrat `HelpEntry` défini ([05](05_contrats_donnees_api.md)) mais aucune surface. |
| **C-05** | **P0** | `onboarding-empty` (`C.14`) | 🔴 non construit | **Abandon J+1 documenté.** À déclencher depuis la home quand l'utilisateur n'a aucune donnée. |
| C-06 | P1 | `analysis-result` (`C.5`) | 🟡 partiel | Existe surtout comme **sous-composant** (`finance-analysis-result`), pas comme page de détail pleinement autonome ; la route `/client/analysis/:analysisId` existe. |
| C-07 | P1 | Lecture support (composite + PEA) en UI | 🟡 partiel | À confirmer dans le détail instrument / résultat ; lié à T-05 backend. |
| **C-08** | **P0** | **Confiance expliquée** (`ConfidenceBreakdown`, `C.5`/`C.6`) | 🟡 partiel | **Moat pédagogique.** Sous-objets `detection`/`validation`/`invalidation` présents en base ; à exposer comme critères au payload + UI (RM-27, [05 §2.6.1](05_contrats_donnees_api.md#261-confidencebreakdown)). |
| **C-09** | **P0** | **Plan d'action** (`ActionPlan`, `C.5`/`C.6`) | 🔴 non construit | **Différenciateur #1 — moat pédagogique.** Générateur déterministe backend + bloc UI ; données sources déjà au contrat (RM-26, [05 §2.6.2](05_contrats_donnees_api.md#262-actionplan)). |
| **C-10** | P1 | **Alertes proactives** (`C.12`, déclencheurs) | 🔴 non construit | Nécessite la ré-évaluation périodique + `NotificationItem.trigger` ; infra `UserNotification` présente (RM-25). |
| **C-11** | **P0** | **Évaluation ex post** (`SignalOutcome`) | 🔴 non construit | **Mécanisme de rétention #1.** Chantier structurant : job batch rejouant `PriceHistory` ; pré-requis des KPI famille A et des alertes de niveau (RM-29, [05 §2.7.1](05_contrats_donnees_api.md#271-signaloutcome-issue-r%C3%A9alis%C3%A9e-ex-post)). |
| **C-12** | P1 | **KPI admin** (`D.10`, `admin-signal-quality`, `admin-engagement`) | 🔴 non construit | `AdminOverview` ne fait que du comptage instantané ; familles B/C/D = pure agrégation sur données prêtes, famille A dépend de C-11 (RM-29). |
| **C-13** | P1 | **Glossaire inline + onboarding** (`C.14`, info-bulles) | 🔴 non construit | Onboarding = nouvel écran ; glossaire = exposer le dictionnaire gouverné en lecture user (RM-28). |

> ⚠️ **Constat positif** : côté **admin**, le front est **plus avancé** que ne le décrivait l'ancienne doc — détails de dictionnaire, détails de wording, audit de snapshot avec **détail + comparaison**. La cible documentée a été enrichie pour refléter ces routes réelles ([03 §D](03_specification_ecrans.md#d-espace-admin)).
>
> 💡 **Mutualisation clé** : C-10 (alertes) et C-11 (ex post) partagent la **ré-évaluation périodique des instruments suivis**. Construire ce mécanisme **une seule fois** (V1 : à la connexion / ouverture des surfaces de suivi ; V2 : batch nocturne). C'est aussi le premier client du batch nocturne V2.

---

## 4. Décisions à arbitrer (À-ARBITRER)

| # | Question ouverte | Pourquoi ça compte | Recommandation |
|---|---|---|---|
| A-02 | `HoldingContext` : enum ou string ? | Aujourd'hui string (T-03). La cible est un enum fermé (`NOT_HELD/HELD`). | Migrer vers enum lors du prochain passage sur la recommandation. |
| A-03 | Snapshot : profondeur de lecture support persistée (T-05) | Conditionne l'auditabilité et la comparaison sans reconstruction (RM-20). | Auditer `AnalysisSnapshot` réel ; compléter si nécessaire. |
| A-04 | Notifications : persistance et endpoints | Le composant front existe ; la chaîne backend (création, lecture, maj statut) n'est pas confirmée. | Confirmer/compléter la persistance et les endpoints avant d'annoncer la capacité. |
| A-05 | ETF : enum présent, runtime exclu | Risque d'élargissement silencieux du périmètre V1. | Maintenir le garde-fou `UnsupportedInstrument` ; n'activer les ETF qu'après contrat V2 dédié. |
| A-06 | Périmètre exact des endpoints user manquants (paramètre, comparaison, learn, help) | Détermine le reste-à-faire V1 réel. | Trancher au cas par cas : route manquante vs payload insuffisant ([05 §4](05_contrats_donnees_api.md#4-lacunes-de-surface-api-cible-vs-existant)). |
| **A-07** | **🟡 BLOQUANT-EX-POST — Fenêtre d'évaluation ex post** (`evaluationWindowDays`) | Détermine quand un signal est jugé `TARGET_HIT`/`INVALIDATION_HIT`/`STILL_OPEN` — donc tous les KPI famille A. **Doit être arbitré avant de construire C-11.** | Choisir `reviewHorizonDays` du signal, ou un horizon fixe (ex. 20/60 j). Documenter et versionner (`policyVersion`). |
| **A-08** | **🟡 BLOQUANT-EX-POST — Stockage de `SignalOutcome`** | Colonne sur `PatternAssessment` vs table dédiée `SignalOutcome`. **Doit être arbitré avant de construire C-11.** | Préférer une table dédiée (multi-horizons futurs, traçabilité), [05 §2.7.1](05_contrats_donnees_api.md#271-signaloutcome-issue-r%C3%A9alis%C3%A9e-ex-post). |
| **A-09** | **Définition canonique de « actif »** (DAU/WAU/MAU) | Login ? requête ? analyse ? Change toutes les métriques d'engagement. | Fixer une définition unique (ex. au moins une requête authentifiée dans la fenêtre) et la documenter. |
| **A-10** | **🔴 BLOQUANT-LÉGAL — RGPD / anonymisation des KPI nominatifs** (RM-29b) | `Analytic` stocke IP + identifiant en clair ; les KPI d'usage s'en servent. | **Obligation CNIL — non optionnel.** Cap à 13 mois, anonymisation mensuelle automatique. À implémenter avant ouverture au public. |
| **A-11** | **Cadence de ré-évaluation V1** (alertes + ex post) | Conditionne la fraîcheur des alertes et des KPI avant le batch nocturne V2. | V1 : à la connexion / ouverture des surfaces de suivi. Mutualiser le mécanisme (cf. §3, mutualisation clé). |

---

## 5. Synthèse de maturité

| Domaine | Maturité | Commentaire |
|---|---|---|
| Backend cœur analyse | **Élevée** | Moteur 4 patterns, orchestration analyse→risque→reco→explication→persistance, registry extensible. |
| Auth & sécurité | **Élevée** | JWT + refresh, BCrypt, rôles, rate limiting, lockout, tests d'intégration. |
| Admin (back + front) | **Élevée** | 8 surfaces de gouvernance, plus avancées que l'ancienne doc. |
| User core (watchlist/portefeuille/analyse/historique/simulation) | **Moyenne-élevée** | Construit ; à fiabiliser sur lecture support et états non-exécutables. |
| Accompagnement (plan d'action, confiance expliquée, onboarding, glossaire) | **Faible** | Données sources présentes ; surfaces et générateurs déterministes à construire (C-08/09/13). |
| Alertes proactives & boucle ex post | **Nulle** | Cœur de l'accompagnement V1 ; chantier structurant (C-10/11) ; infra notifications présente. |
| Pilotage (KPI admin) | **Faible** | Données brutes prêtes ; admin ne fait aujourd'hui que du comptage instantané (C-12). |
| Surfaces d'orientation user (learn, help, onboarding, paramètre, comparaison) | **Faible** | Plusieurs écrans cible non construits (§3). |
| Cohérence doc ↔ code | **Restaurée** | Divergences d'enums corrigées (§1) ; ce registre tient le reste à jour. |

---

## 6b. Blocages légaux identifiés — 2026-05-28

> Issus de l'analyse risk-assessment et de la revue [08](08_analyse_critique_et_legal.md). Ces blocages **doivent être levés avant tout lancement commercial** — ils ne sont pas des décisions de priorisation produit mais des obligations légales.

| Blocage | Référence légale | Gravité | Action requise |
|---|---|---|---|
| **Requalification CIF/AMF non tranchée** | Art. L541-1 CMF, Directive MIF 2 | 🔴 EXISTENTIEL | Consulter avocat AMF/MIF 2 — avis écrit avant commercialisation. Sans avis favorable, toute mise en ligne commerciale est à risque. |
| **Suppression de compte absente** | RGPD Art. 17 | 🔴 CRITIQUE | Implémenter flux de suppression (auto-service ou admin) + politique de rétention des snapshots. Amende potentielle : jusqu'à 4% du CA mondial. |
| **Export données absent** | RGPD Art. 20 (portabilité) | 🟡 ÉLEVÉ | Ajouter `GET /account/data-export` (portfolio + watchlist + historique analysé) en JSON/CSV. |
| **Logs Analytic nominatifs non cappés à 13 mois** | Délibération CNIL, RGPD Art. 5(1)(e) | 🔴 CRITIQUE | Job d'anonymisation mensuel automatique (arbitration A-10 = obligation légale, non optionnel). |
| **Documents légaux absents** (CGU, politique confidentialité, mentions légales, disclaimer AMF) | LCEN Art. 6, RGPD Art. 13, Code conso | 🔴 CRITIQUE | Rédiger + publier avant toute mise en ligne publique. La rédaction finale dépend de l'avis AMF. |

---

## 6. Règle de maintenance de ce registre

À chaque modification de code touchant une zone listée ici : mettre à jour la ligne correspondante (résolue / encore ouverte). Quand un écart est résolu, le **retirer** d'ici — il n'a pas vocation à grossir indéfiniment. Ne **jamais** déplacer un écart non résolu dans un document cible (01→05) : la cible reste la cible.
