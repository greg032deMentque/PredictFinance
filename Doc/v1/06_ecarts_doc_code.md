# 06 — Écarts entre la cible documentée et le code réel

> **Propriétaire de** : tous les écarts entre les documents cibles (01→05) et l'état réel du code (`FinanceBack/`, `FinanceFront/`).
> **Dernière réconciliation : 2026-07-13** (le code fait foi). La refonte du moteur, la boucle ex post, l'enrichissement fondamental, le multi-portefeuille et la couche d'accompagnement ont été livrés depuis la version initiale de ce registre (2026-05-21) : les écarts correspondants sont **retirés** ci-dessous (la règle du pack impose de retirer un écart une fois résolu, §6). Ne subsistent que les écarts **réels restants**.
> **Principe** : les documents 01→05 décrivent le **produit cible V1 cohérent**. Cette page est le **seul** endroit où l'on dit honnêtement « voici ce qui diffère aujourd'hui ». Aucun écart n'est caché ; aucun document cible n'est dégradé pour coller à la dette.

## Classification

| Code | Sens |
|---|---|
| **DIVERGENCE-DOC** | L'ancienne doc affirmait quelque chose de faux ; corrigé dans le pack, conforme au code. |
| **DETTE-TECH** | Le code dévie de la cible pour raison historique ; à résorber, ne pas propager. |
| **À-CONSTRUIRE** | Écran/route/capacité cible non encore construit. |
| **À-ARBITRER** | Décision produit ou technique non tranchée ; nécessite un choix humain. |

---

## 0. Livré depuis la refonte (écarts résolus, retirés du registre)

> Consigné pour mémoire. Ces éléments étaient des écarts au 2026-05-21 ; ils sont **conformes au code** aujourd'hui et ne sont plus suivis ici.

- **Moteur patterns** : passé de 4 à **8 patterns actifs** (ajout des retournements `DOUBLE_BOTTOM`, `DOUBLE_TOP`, `HEAD_AND_SHOULDERS`, `INVERSE_HEAD_AND_SHOULDERS`), ATR/stop-loss/take-profit/volume/position sizing dans le contexte de risque.
- **Boucle ex post** : entité/table `SignalOutcome` + `SignalOutcomeEvaluationJob` (batch quotidien 03:00 UTC), fenêtre = `HorizonDays` (défaut 30), stats win-rate Wilson.
- **Accompagnement** : `ConfidenceBreakdown` (assemblé, 3 critères) et `ActionPlan` (générateur déterministe, ≤ 3 étapes) livrés et rendus inline dans le détail d'analyse.
- **Alertes proactives** : `UserNotification` + déclencheur `LevelCrossed` émis par le job ex post ; endpoints notifications + préférences.
- **Écrans user** : `parameter-detail`, `snapshot-compare`, `learn`, `help`, `analysis-detail` (page routée), page `glossary`, `onboarding` — tous construits.
- **KPI admin** : `kpi/engagement` + `kpi/signal-quality` (+ exports) et leurs écrans.
- **Fondamentaux** : score composite **câblé au front**, 6 catégories, percentile intra-secteur (repli univers), consensus analystes en carte informative séparée.
- **Multi-portefeuille** : CRUD portefeuilles + archive/restore + détail par portefeuille.
- **RGPD** : suppression de compte (`DELETE account/self` + `User.DeletedAt`), export de données (`POST account/data-export`), gestion des consentements.
- **HoldingContext** : migré de string vers enum `HoldingStatusEnum`.

---

## 1. Divergences de documentation corrigées (DIVERGENCE-DOC)

| # | Sujet | Ancienne doc disait | Code réel | Résolution |
|---|---|---|---|---|
| D-01 | `AnalysisOutcome` | `ANALYSIS_COMPLETED / INSTRUMENT_NOT_ELIGIBLE / INSUFFICIENT_MARKET_DATA / NO_ENABLED_PATTERN / NO_CREDIBLE_PATTERN` | `CrediblePatternFound / MultipleCompatiblePatterns / NoCrediblePattern / InsufficientData / UnsupportedInstrument / UnsupportedContext` | Pack aligné sur le **code** ([02](02_glossaire_et_taxonomies.md#analysisoutcome)). |
| D-02 | `UserRole` | `USER / ADMIN` | `User / Admin` (`Common/enums/UserEnums.cs`) | Pack aligné sur les **deux** rôles actifs. |
| D-03 | `AssetType` | « EQUITY uniquement » | `Stock / Etf / Crypto` (`AssetTypeEnum.cs`) | Enum plus large que le runtime ; garde-fou `UnsupportedInstrument` pour Etf/Crypto ([02](02_glossaire_et_taxonomies.md#assettype)). |
| D-04 | `PatternStatus` | Cohérent (`Forming/Monitoring/Confirmed/Invalidated/Completed`) | Idem + état `Absent` dans le **read model** séparé (`PatternProgressStatusEnum`) | `Absent` = état de read model, pas de domaine. |
| D-05 | Patterns de retournement | Doc antérieure : « `DOUBLE_TOP` retiré, moteur = 4 continuation » | **8 patterns actifs** (4 continuation + 4 retournement, `DOUBLE_TOP` **de nouveau actif**) — `PatternIds.cs` | Pack aligné sur la whitelist réelle ([02](02_glossaire_et_taxonomies.md#3-patterns-pris-en-charge-v1)). |
| D-06 | Verbes de reco | Enum secondaire non rappelé | `RecommendationKind` (V1) **et** `RecommendationActionEnum` (trading retiré) coexistent | `RecommendationKind` canonique ; `RecommendationActionEnum` = legacy à ne pas utiliser. |

---

## 2. Dette technique (DETTE-TECH)

> À résorber lors des milestones qui touchent la zone concernée — **ne pas étendre**.

| # | Dette | Évidence | Règle d'opération |
|---|---|---|---|
| T-01 | Couplage `Services → ViewModels` | Les services dépendent des ViewModels (devrait être l'inverse) | Ne pas introduire de **nouveau** couplage sans justification explicite. |
| T-02 | Contrats d'analyse dispersés | Contrats répartis entre `Common/AnalysisV1` et `Patterns/Contracts` ; pas de projet `Contracts` dédié | Ne pas étendre inutilement `AnalysisV1` ; viser un emplacement de contrats cohérent. |
| T-03 | États fermés persistés en `string` | `PatternAssessment.PatternId` et `.Phase` restent des strings libres (non enum-backed) ; `AnalysisRun.Status` est un enum en code mais **persisté en string** ; `PatternAssessment.Direction` en string. Convention DB mixte (certains enums stockés en `int`). | Ne **pas** créer de nouveau champ d'état fermé en string libre ; cible = enums ([02](02_glossaire_et_taxonomies.md)). |
| T-04 | Périmètre d'actif plus large que le runtime | `AssetType` contient `Etf/Crypto` mais V1 = actions FR | Garantir que tout instrument non-`Stock`/non-FR produit `UnsupportedInstrument` (RM-24), **pas** un élargissement silencieux. |
| T-05 | Lecture support non persistée au snapshot | Le snapshot persiste les niveaux de risque et le contexte portefeuille, mais **pas** le score composite / la lecture PEA / les zones support-résistance (recalculés à la volée). | Compléter `AnalysisSnapshot` si l'audit/comparaison de la lecture support sans reconstruction devient requis (RM-20). |

---

## 3. Écrans / capacités cible à construire (À-CONSTRUIRE)

> Reste réel après la vague de livraisons (§0). Priorités : **P0** bloquant · **P1** avant croissance · **P2** post-PMF.

| # | Priorité | Capacité cible | État | Note |
|---|---|---|---|---|
| C-07 | P1 | **Lecture support persistée au snapshot** (composite + PEA) | 🟡 partiel | Exposée en direct dans le panneau fondamentaux ; **non persistée** au snapshot (lié à T-05). À compléter si l'audit historique de la lecture support est requis. |
| C-10 | P1 | **Écran d'alertes proactives dédié** | 🟡 partiel | Le déclencheur `LevelCrossed` et les préférences existent ; il manque une **surface de consultation des alertes** en tant que telle (au-delà du fil de notifications). |
| C-13 | P1 | **Glossaire inline (info-bulles)** | 🟡 partiel | Page glossaire et dictionnaire gouverné livrés ; il manque le **composant réutilisable d'info-bulle** injectant les termes au fil du texte (RM-28). |
| C-14 | P2 | **Empty-state onboarding déclenché depuis la home** | 🟡 partiel | La page `onboarding` existe ; l'empty-state J+1 n'est pas un déclencheur distinct depuis la home quand l'utilisateur n'a aucune donnée. |

---

## 4. Décisions à arbitrer (À-ARBITRER)

| # | Question ouverte | Pourquoi ça compte | Recommandation |
|---|---|---|---|
| A-03 | Profondeur de lecture support persistée au snapshot (T-05 / C-07) | Conditionne l'auditabilité et la comparaison de la lecture support sans reconstruction (RM-20). | Trancher si le composite/PEA doit être figé au snapshot ou rester une lecture vivante. |
| A-05 | ETF : enum présent, runtime exclu | Risque d'élargissement silencieux du périmètre V1. | Maintenir le garde-fou `UnsupportedInstrument` ; n'activer les ETF qu'après contrat V2 dédié. |
| A-09 | Définition canonique de « actif » (DAU/WAU/MAU) | Login ? requête ? analyse ? Change toutes les métriques d'engagement. | Fixer une définition unique (ex. au moins une requête authentifiée dans la fenêtre) et la documenter. |

---

## 5. Synthèse de maturité

| Domaine | Maturité | Commentaire |
|---|---|---|
| Backend cœur analyse | **Élevée** | Moteur 8 patterns (continuation + retournement), ATR/risque, orchestration analyse→risque→reco→plan→explication→persistance. |
| Auth & sécurité & RGPD | **Élevée** | JWT + refresh, BCrypt, rôles, rate limiting, lockout ; suppression de compte, export de données, consentements livrés. |
| Admin (back + front) | **Élevée** | Gouvernance + KPI (engagement, qualité des signaux) livrés. |
| User core (watchlist/portefeuille/analyse/historique/simulation) | **Élevée** | Multi-portefeuille, comparaison de snapshots, détail d'analyse routé. |
| Accompagnement (plan d'action, confiance expliquée, glossaire, onboarding) | **Moyenne-élevée** | Plan d'action + confiance expliquée livrés ; reste le glossaire inline et l'empty-state déclenché. |
| Boucle ex post & alertes | **Moyenne-élevée** | `SignalOutcome` + job quotidien + alerte `LevelCrossed` livrés ; manque un écran d'alertes dédié. |
| Persistance de la lecture support | **Moyenne** | Exposée en direct mais non figée au snapshot (T-05 / C-07). |
| Rétention RGPD des logs `Analytic` | **Élevée** | `AnalyticsRetentionJob` (job mensuel hébergé) anonymise les lignes de plus de 13 mois (login haché, IP/body/UA/referer vidés). |
| Cohérence doc ↔ code | **Restaurée** | Réconciliée au 2026-07-13. |

---

## 6b. Blocages légaux — état 2026-07-13

> ✅ **Résolus depuis 2026-05-28** : suppression de compte (RGPD Art. 17 — `DELETE account/self` + `User.DeletedAt`) et export de données (RGPD Art. 20 — `POST account/data-export`). Retirés du tableau.
> ✅ **Résolu** : rétention `Analytic` cappée à 13 mois (`AnalyticsRetentionJob`, anonymisation mensuelle automatique). Retiré du tableau (ex A-10).

| Blocage | Référence légale | Gravité | Action requise |
|---|---|---|---|
| **Requalification CIF/AMF non tranchée** | Art. L541-1 CMF, Directive MIF 2 | 🟠 À CADRER | Pertinence conditionnée à une éventuelle mise en ligne commerciale (application à usage personnel aujourd'hui). |
| **Documents légaux** (CGU, confidentialité, mentions, disclaimer) | LCEN Art. 6, RGPD Art. 13, Code conso | 🟠 À CADRER | Requis avant toute mise en ligne publique ; sans objet en usage personnel. |

---

## 6. Règle de maintenance de ce registre

À chaque modification de code touchant une zone listée ici : mettre à jour la ligne correspondante. Quand un écart est résolu, le **retirer** (le §0 garde une trace synthétique) — ce registre n'a pas vocation à grossir. Ne **jamais** déplacer un écart non résolu dans un document cible (01→05) : la cible reste la cible.
