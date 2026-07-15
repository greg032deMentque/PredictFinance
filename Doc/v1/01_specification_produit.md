# 01 — Spécification produit V1

> **Propriétaire de** : vision, besoin métier, personas, périmètre, règles métier structurantes, modèle de domaine, contrat d'analyse, séparation des couches, roadmap.
> Les enums et le vocabulaire sont dans [02](02_glossaire_et_taxonomies.md). Le comportement des écrans est dans [03](03_specification_ecrans.md). Les écarts avec le code sont dans [06](06_ecarts_doc_code.md).

---

## 1. Résumé exécutif

PredictFinance est une **application web pédagogique d'aide à l'investissement** destinée à un **particulier débutant**. Elle l'aide à suivre son portefeuille et sa watchlist, à comprendre l'état technique et fondamental d'une action française, et à recevoir une **aide pédagogique à la décision** : surveiller, attendre, acheter, conserver, renforcer, alléger ou vendre.

Le produit **ne passe aucun ordre** et **n'accède à aucun compte bancaire**. Il produit des analyses, des recommandations pédagogiques et des notifications contextuelles fondées sur des **règles déterministes auditées**.

Le cœur du système est un **moteur d'analyse versionné, explicable et historisé** : chaque analyse peut être rejouée, comprise et comparée dans le temps.

---

## 2. Vision produit

1. **Rendre lisible le complexe.** Transformer des données de marché et des ratios fondamentaux en conclusions compréhensibles, pédagogiques et traçables pour un non-expert.
2. **Contextualiser.** Relier les signaux techniques à la situation réelle de l'utilisateur (détient / ne détient pas la valeur) sans jamais polluer le calcul technique.
3. **Capitaliser dans le temps.** Conserver un historique d'analyses versionnées pour apprendre, comparer et, à terme, mesurer la performance des signaux.
4. **Durer techniquement.** Construire un socle extensible qui accepte de nouveaux patterns, actifs et marchés **sans refonte du noyau**.

---

## 3. Problème métier adressé

Un particulier débutant manque de méthode pour :
- lire un graphique et identifier une configuration technique crédible ;
- distinguer une valeur fondamentalement solide d'une valeur fragile ;
- relier ces observations à une décision prudente adaptée à **sa** situation (détenue ou non).

L'information de marché est abondante, hétérogène et difficile à interpréter sans expérience. Le produit **réduit cette complexité sans promettre de résultat financier**. Il ne s'agit pas d'automatiser le trading mais de fournir un **cadre d'analyse pédagogique, cohérent et traçable**.

---

## 4. Périmètre V1

### 4.1 Positionnement

- ✅ Outil d'**aide pédagogique** à l'investissement.
- ✅ Outil d'**analyse technique** par patterns déterministes (continuation **et** retournement).
- ✅ Outil de **lecture fondamentale** relative et d'**éligibilité PEA**.
- ✅ Outil de **recommandation pédagogique contextualisée**.
- ❌ **N'est pas** un robot d'exécution, un courtier, ni un système de trading temps réel.

### 4.2 Inclus en cible V1

| Domaine | Contenu |
|---|---|
| Accès | Création de compte, connexion, mot de passe oublié, réinitialisation. |
| Espaces | Séparation stricte `Anonymous` / `User` / `Admin`. Aucun shell métier visible avant connexion. |
| Watchlist | Ajout, suppression, consultation de valeurs non détenues à surveiller. |
| Portefeuille | Plusieurs lignes d'achat par actif (quantité, prix, date, frais, devise) ; reconstruction des lignes ouvertes après ventes en **FIFO strict** ; PRU dérivé. |
| Analyse | Analyse **journalière à la demande** sur actions françaises prises en charge. |
| Patterns | Détection déterministe **multi-patterns** : pattern principal + alternatifs compatibles. |
| Lecture fondamentale | Score relatif par catégorie + score composite + couverture de données, **séparé** de l'analyse technique. |
| Éligibilité PEA | Statut explicite à 3 états (`ConfirmedEligible` / `ConfirmedIneligible` / `Unknown`). |
| Risque | Invalidation, stop loss, take profit, ratio risque/rendement, volatilité, drawdown potentiel selon disponibilité. |
| Restitution | Résumé pédagogique, justification explicite, recommandation contextualisée détenue / non détenue. |
| Historique | Snapshots versionnés complets ; comparaison de deux snapshots (côté user et admin). |
| Qualité des signaux | Évaluation **ex post** de chaque signal persisté (atteinte de cible vs. invalidation) pour mesurer si le moteur a eu raison. |
| Plan d'action | Bloc déterministe « Vos prochaines étapes » en fin d'analyse, reliant l'analyse à des actions concrètes. |
| Confiance expliquée | Décomposition lisible des critères qui justifient le niveau de confiance d'un pattern. |
| Simulation | Exploration de scénario (entrée, taille, invalidation, cible, frais) sans la confondre avec l'historique persisté. |
| Alertes & notifications | Centre de notifications **et alertes proactives** sur changement d'état de pattern ou franchissement de niveau : priorise et route vers la bonne page. |
| Aide | Help center contextuel + **glossaire inline** : explique et oriente. |
| Onboarding | Parcours de première valeur guidé pour l'utilisateur sans données. |
| Compte | Profil, préférences, préférences de notification, sécurité — **self-service uniquement**. |
| Administration | Espace distinct de gouvernance : utilisateurs, registres, politique de scoring, dictionnaire de paramètres, versions de wording, audit de snapshots, qualité de données, **et tableaux de pilotage (KPI)**. |

### 4.3 Hors scope explicite V1

| Hors scope | Note |
|---|---|
| Passage d'ordres, accès bancaire, synchronisation courtier | Jamais en V1. |
| Trading temps réel tick par tick | Granularité V1 = journalière uniquement. |
| ETF, crypto et autres familles d'actifs | **Voir §4.4** : l'architecture les anticipe mais le runtime V1 reste limité aux actions françaises. |
| Fiscalité / comptabilité PEA-CTO détaillée | La règle FIFO V1 est une **contextualisation produit**, pas une politique fiscale ([§7.3](#73-r%C3%A8gle-fifo-de-reconstruction-des-lignes-ouvertes)). |
| Personnalisation avancée par profil investisseur | La tolérance au risque n'est pas personnalisée en V1. |
| IA générative comme source de vérité | L'IA reste périphérique et facultative ; aucun texte obligatoire n'est généré par IA en V1. |
| Messagerie / aide conversationnelle temps réel | Le help center est déterministe et contextuel. |
| SSO, MFA imposée, fédération d'identité, multi-tenant | Hors V1. |

### 4.4 Règle d'extensibilité (point critique)

> ⚠️ **Écart connu, à arbitrer.** L'enum `AssetType` du code contient déjà `Stock / Etf / Crypto`, et la roadmap prévoit les ETF en V2. Le **runtime V1 doit rester restreint aux actions françaises actives** : un instrument hors périmètre doit produire l'issue métier `UnsupportedInstrument`, jamais un élargissement silencieux. L'architecture est conçue pour ajouter des politiques ETF plus tard **sans** étendre le périmètre courant. Détail de l'écart : [06](06_ecarts_doc_code.md).

---

## 5. Personas et parties prenantes

> ⟶ **Voir [10 — Personas et profils utilisateurs](10_personas.md)** : personas enrichis, mental models, sessions types, critères succès/échec et parties prenantes. `10` fait désormais autorité sur cette section.

Résumé des personas définis dans `10` :

| ID | Persona | Rôle système | Objectif primaire |
|---|---|---|---|
| `PERSONA-ANON` | Visiteur Anonyme | — | Découvrir le produit, créer un compte |
| `PERSONA-U01` | Investisseur Découvrant | `User` | Lancer et comprendre une première analyse |
| `PERSONA-U02` | Investisseur Actif | `User` | Surveiller ses positions, détecter des opportunités |
| `PERSONA-A01` | Administrateur | `Admin` | Gouverner la vérité, piloter la qualité, gérer les accès |

> **Rôles techniques** : le code distingue `User` et `Admin`. `Admin` est l'unique rôle d'accès à l'espace `/admin/*`. Enums canoniques : [02](02_glossaire_et_taxonomies.md#userrole).

---

## 6. Modèle métier central : les quatre lectures

Le produit produit **quatre lectures distinctes** qui ne doivent **jamais fusionner en un score unique** :

| Lecture | Question à laquelle elle répond | Source |
|---|---|---|
| **Lecture marché** (technique) | « Quelle configuration technique le graphe montre-t-il, et avec quelle crédibilité ? » | Moteur de patterns. |
| **Lecture support** (fondamentale + PEA) | « Cette valeur est-elle fondamentalement solide relativement à son univers ? Est-elle éligible PEA ? » | Scoring fondamental + registre PEA. |
| **Lecture situation personnelle** | « Qu'est-ce que cela signifie pour MOI, selon que je détiens ou non ? » | Contexte portefeuille (FIFO). |
| **Lecture paramètre** | « Que veut dire CE chiffre précis, et qu'implique-t-il ? » | Dictionnaire de paramètres gouverné. |

La **recommandation** est en aval de ces lectures : elle les synthétise en un verbe d'action pédagogique, sans jamais les écraser visuellement ni se substituer à elles.

```
Lecture marché ─┐
Lecture support ─┼─►  Recommandation (verbe contextualisé détenue/non détenue) ──► Plan d'action
Contexte perso ─┘                                                                  (étapes concrètes)
Lecture paramètre  ──►  (pédagogie transverse, n'est jamais à elle seule une reco)
```

Deux principes de présentation découlent de ce modèle et sont des capacités V1 à part entière :

- **Le plan d'action** prolonge la recommandation : il reformule les vérités déjà calculées (niveau d'invalidation, horizon de revue, seuil d'alerte) en étapes concrètes, sans jamais introduire de nouvelle vérité (RM-26).
- **La confiance expliquée** rend tangible la lecture marché : le niveau de confiance d'un pattern est décomposé en critères issus des sous-objets `detection` / `validation` / `invalidation` déjà séparés, sans recalcul (RM-27).

### 6.1 La boucle de feedback (apprentissage ex post)

Les quatre lectures décrivent un instant. Pour que le produit **s'améliore** et **inspire confiance dans le temps**, chaque analyse persistée est rejouée a posteriori :

```
Analyse persistée (snapshot) ──► temps qui passe ──► Évaluation ex post
  (niveaux cible / invalidation)                       (cible atteinte ? invalidation touchée ?)
                                                                │
                          ┌─────────────────────────────────────┴───────────────────┐
                          ▼                                                           ▼
              Alertes proactives (utilisateur)                        KPI de qualité des signaux (admin)
       changement d'état / franchissement de niveau                calibration de la confiance, perf. par pattern
```

Cette boucle est le **cinquième pilier** du modèle métier : sans elle, le moteur ne peut ni se calibrer ni rendre des comptes. Elle alimente à la fois les **alertes** côté utilisateur (RM-25) et les **KPI de qualité des signaux** côté admin (RM-29). La taxonomie de l'issue réalisée est en [02](02_glossaire_et_taxonomies.md#signaloutcome-issue-r%C3%A9alis%C3%A9e-ex-post) ; le contrat en [05](05_contrats_donnees_api.md).

---

## 7. Règles métier structurantes

> Ces règles sont **normatives**. Elles sont numérotées pour pouvoir être référencées par les autres documents (ex. : « RM-04 » dans une user story).

### 7.1 Vérité et explicabilité

| ID | Règle |
|---|---|
| **RM-01** | L'**API porte la vérité métier** : données normalisées, détection, scoring, risque, recommandation, historique, traçabilité. Le frontend n'invente aucune vérité métier ni aucune explication. |
| **RM-02** | L'**IA est facultative et périphérique** en V1. Aucun texte obligatoire (explications, justifications, statuts) n'est généré par IA ; tous sont **déterministes, versionnés et auditables**. |
| **RM-03** | Tous les résultats affichés sont **explicables, auditables et historisés**. |

### 7.2 Détection technique

| ID | Règle |
|---|---|
| **RM-04** | Chaque pattern déclare son **propre contrat** : prérequis, fenêtre d'analyse minimale, détection, validation, invalidation, scoring. |
| **RM-05** | Si **aucun pattern fiable** n'est trouvé, le système l'indique explicitement (`NoCrediblePattern`) au lieu de forcer un faux signal. |
| **RM-06** | Si **plusieurs patterns** sont compatibles, ils sont **tous** restitués (`MultipleCompatiblePatterns`) avec leur niveau de confiance et l'explication de leur coexistence. Le **pattern principal** est un choix d'affichage prioritaire ; il **n'efface jamais** les alternatifs. |
| **RM-07** | La détection technique ne lit que l'instrument et la date (`asOfDate`). Elle **ne dépend jamais** des champs de détention du portefeuille. |

### 7.3 Règle FIFO de reconstruction des lignes ouvertes

| ID | Règle |
|---|---|
| **RM-08** | En V1, les lignes d'achat ouvertes sont reconstruites en **FIFO strict** pour la contextualisation portefeuille : chaque `Buy` crée une ligne candidate ; chaque `Sell` consomme d'abord la quantité des plus anciennes lignes ouvertes ; le `PortfolioContext` ne contient que les quantités restantes. Le PRU est dérivé des lignes ouvertes. |
| **RM-08b** | Cette règle est une **simplification de contextualisation produit**. Elle ne doit **jamais** être réinterprétée comme une politique fiscale ou comptable de courtier. |

### 7.4 Recommandation et contexte de détention

| ID | Règle |
|---|---|
| **RM-09** | La recommandation dépend de l'analyse marché **et** du contexte de détention. Le contexte peut **qualifier** la recommandation et l'explication, mais **ne modifie jamais** la vérité technique calculée. |
| **RM-10** | Les verbes autorisés dépendent du contexte : **non détenu** → `Surveiller`, `Attendre`, `Acheter` ; **détenu** → `Conserver`, `Renforcer`, `Alléger`, `Vendre`, `Attendre`. `Surveiller` n'est **jamais** la recommandation finale d'une position détenue. (Liste canonique : [02](02_glossaire_et_taxonomies.md#recommandation--verbes).) |
| **RM-11** | La recommandation ne contient **jamais** de niveaux de risque chiffrés (stop loss, take profit, ratio R/R, niveau d'invalidation) : ceux-ci restent dans les *risk hints*. Elle référence les patterns via leurs identifiants, pas leurs faits de détection. |

### 7.5 Lecture support (fondamentale + PEA)

| ID | Règle |
|---|---|
| **RM-12** | La lecture support est une capacité **parallèle**, jamais un remplacement de l'analyse technique. Un score fondamental seul ne décide jamais. |
| **RM-13** | Le score composite repose sur un **classement relatif non paramétrique** (percentiles) sur un univers explicite, robuste aux valeurs aberrantes, avec gestion déterministe des ex æquo. |
| **RM-14** | Le score composite est **indisponible** si la couverture de données est insuffisante (< 3 catégories valides) ou si l'éligibilité PEA n'est pas confirmée. L'indisponibilité est un **état métier explicite**, pas une erreur. |
| **RM-15** | L'**éligibilité PEA** a 3 états explicites. `Unknown` n'est **jamais** silencieusement traité comme éligible. La vérité PEA vient d'un **registre interne gouverné**, pas d'un fournisseur de données de marché. |

### 7.6 Pédagogie des paramètres

| ID | Règle |
|---|---|
| **RM-16** | Chaque paramètre exposé doit pouvoir être déplié en 4 couches : **définition** → **lecture de la valeur courante** → **pourquoi ça compte** → **implication pour ma situation**. |
| **RM-17** | Les explications de paramètres sont **générées par le backend** à partir d'un **dictionnaire gouverné** et versionné. Le frontend ne synthétise jamais de vérité explicative. |
| **RM-18** | Un paramètre seul ne devient **jamais** une recommandation à lui tout seul. |

### 7.7 Historisation

| ID | Règle |
|---|---|
| **RM-19** | Chaque analyse exécutable est persistée en **snapshot versionné complet** (issue, contexte portefeuille, recommandation, évaluations par pattern, versions de moteur/règles). |
| **RM-20** | L'historique et la comparaison lisent la **vérité persistée** du snapshot ; ils ne la **reconstruisent jamais** à partir de l'état courant. La non-comparabilité de deux snapshots doit être rendue **explicite**. |

### 7.8 Espaces et accès

| ID | Règle |
|---|---|
| **RM-21** | Avant authentification, **aucun shell produit** n'expose la navigation user ou admin. |
| **RM-22** | Après authentification, l'utilisateur est routé vers **un espace cohérent unique** selon son rôle. |
| **RM-23** | **Rôles distincts non concurrents** : `Notifications` priorise et route ; `Help center` explique ; `Account` configure (self-service) ; `Admin` gouverne. Aucune de ces surfaces n'empiète sur une autre. |

### 7.9 États métier de premier rang

| ID | Règle |
|---|---|
| **RM-24** | La V1 distingue trois niveaux : (a) issues métier **exécutables** (résultat normal) ; (b) issues métier **non exécutables** (`NoCrediblePattern`, `InsufficientData`, `UnsupportedInstrument`, `UnsupportedContext`…), qui sont des **états de premier rang** avec code et explication ; (c) **erreurs techniques** (HTTP/API), hors taxonomie métier visible. Une analyse non exécutable n'est **jamais** une erreur silencieuse. |

### 7.10 Alertes proactives

| ID | Règle |
|---|---|
| **RM-25** | Le produit **veille pour l'utilisateur** : tout instrument suivi (watchlist ou portefeuille) est ré-évalué périodiquement, et un changement notable génère une alerte. Déclencheurs canoniques : (a) changement d'état de pattern (`Monitoring`→`Confirmed`, ou `*`→`Invalidated`) ; (b) franchissement d'un niveau persisté (`InvalidationPrice` / `TargetPrice`) ; (c) bascule de fraîcheur des données d'une valeur suivie vers `STALE`. Liste : [02](02_glossaire_et_taxonomies.md#d%C3%A9clencheurs-dalerte). |
| **RM-25b** | Une alerte **route, n'explique pas** (cohérent RM-23) : la vérité reste sur l'écran de destination. Elle respecte les préférences de notification de l'utilisateur, est dédoublonnée par (instrument × type × jour), et **n'est pas une prédiction** (cohérent avec la règle de simulation). |

### 7.11 Plan d'action

| ID | Règle |
|---|---|
| **RM-26** | Tout résultat d'analyse exécutable se conclut par un **plan d'action déterministe** (« Vos prochaines étapes ») qui **reformule** des vérités déjà calculées (niveau d'invalidation, `reviewHorizonDays`, seuil d'alerte suggéré, verbe selon détention). Il **n'introduit aucun nouveau chiffre ni aucune nouvelle vérité** (cohérent RM-01), reste versionnable (RM-02), traçable à l'élément d'analyse source, et conforme au contexte détenue/non détenue (RM-10). |

### 7.12 Confiance expliquée & glossaire

| ID | Règle |
|---|---|
| **RM-27** | Le niveau de confiance d'un pattern est toujours accompagné d'une **décomposition lisible de ses critères**, issus des sous-objets `detection` / `validation` / `invalidation` déjà séparés ([05](05_contrats_donnees_api.md#25-patternassessment)). Cette décomposition **explique** la confiance, elle ne la **recalcule pas** et ne la **modifie pas**. |
| **RM-28** | La pédagogie est délivrée **au moment du doute** via un **glossaire inline** (info-bulles) dont le contenu provient du **dictionnaire de paramètres gouverné** (RM-17) — jamais de texte frontend libre. Le `Learn` reste pour le contenu conceptuel long ; pas de doublon (RM-23). |

### 7.13 Pilotage (KPI)

| ID | Règle |
|---|---|
| **RM-29** | L'admin dispose de **tableaux de pilotage (KPI)** couvrant quatre familles : qualité des signaux (ex post), engagement & rétention, usage & funnel d'activation, santé opérationnelle & data. Tout KPI a une **formule explicite et une source traçable** (cohérent RM-03) et **n'invente aucune vérité métier** : il agrège la vérité déjà persistée. Familles et formules : [03 §D.10](03_specification_ecrans.md#d10--pilotage-kpi). |
| **RM-29b** | L'exploitation analytique des données nominatives (logs de requêtes : IP, identifiant) doit cadrer **conservation et anonymisation** ([06](06_ecarts_doc_code.md#4-d%C3%A9cisions-%C3%A0-arbitrer)). |

---

## 8. Contrat de sortie d'une analyse

Toute analyse exécutable fournit au minimum :

| Bloc | Contenu attendu |
|---|---|
| **Identification** | Actif, marché, date/heure d'analyse, version du moteur, fenêtre réellement utilisée. |
| **Issue métier** (`AnalysisOutcome`) | Résultat de premier rang ([02](02_glossaire_et_taxonomies.md#analysisoutcome)), couvrant l'exécutable et le non-exécutable. |
| **Patterns** | Pattern principal d'affichage + alternatifs compatibles, statut, confiance, justification, par pattern. |
| **État du pattern** (`PatternStatus`) | Vocabulaire lisible par un débutant, dérivé de la taxonomie canonique ([02](02_glossaire_et_taxonomies.md#patternstatus)). |
| **Recommandation** | Verbe autorisé selon le contexte de détention (RM-10), avec justification déterministe. |
| **Confiance expliquée** | Niveau de confiance du pattern principal **accompagné de sa décomposition en critères** (RM-27). |
| **Risque** | Invalidation, stop loss, take profit, ratio R/R, volatilité, drawdown potentiel, selon disponibilité. |
| **Lecture support** | Disponibilité du score composite, complétude support, statut PEA, conditions bloquantes/partielles. |
| **Explication** | Résumé pédagogique clair, sans jargon non expliqué. |
| **Contexte portefeuille** | Détention ou non, synthèse des lignes ouvertes utiles à la contextualisation. |
| **Plan d'action** | « Vos prochaines étapes » déterministes reformulant les vérités ci-dessus (RM-26). |

Le détail des structures de données est dans [05](05_contrats_donnees_api.md).

### 8.1 Évaluation ex post d'un signal persisté

Au-delà de l'instant de l'analyse, chaque signal persisté reçoit, après écoulement du temps, une **issue réalisée** (`SignalOutcome` : `TARGET_HIT` / `INVALIDATION_HIT` / `STILL_OPEN` / `NOT_EVALUABLE`, [02](02_glossaire_et_taxonomies.md#signaloutcome-issue-r%C3%A9alis%C3%A9e-ex-post)). Cette évaluation déterministe (RM-29) :
- compare les prix postérieurs (`PriceHistory`) aux niveaux `TargetPrice` / `InvalidationPrice` persistés au snapshot ;
- alimente les **alertes** de franchissement de niveau (RM-25) ;
- alimente les **KPI de qualité des signaux** et la **calibration de la confiance** (RM-29).

---

## 9. Roadmap V1 → V3

Le versioning privilégie la **stabilité du noyau** avant l'élargissement.

| Version | Objectif directeur | Apports clés |
|---|---|---|
| **V1** *(en cours)* | Socle produit + refonte cœur **+ accompagnement et pilotage**. | Actions FR, analyse journalière à la demande, moteur déterministe, multi-patterns, lecture fondamentale + PEA, historisation versionnée, **boucle de feedback ex post**, **alertes proactives**, **plan d'action**, **confiance expliquée**, **onboarding + glossaire inline**, accès + surfaces d'orientation, admin de gouvernance **avec KPI**. **Exploitable sans IA.** |
| **V2** | Industrialisation et enrichissement. | **Batch nocturne pré-calculé** (industrialise la ré-évaluation qui sous-tend alertes et KPI V1), historique comparatif enrichi, profondeur de la mesure ex post (horizons multiples), **ouverture ETF** (après contrat dédié), montée en couverture. |
| **V3** | Expansion et intelligence contrôlée. | Multi-actifs / multi-marchés, assistance pédagogique enrichie (IA strictement périphérique), personnalisation progressive **si elle reste explicable**, comparaison multi-valeurs, approfondissement des KPI d'usage et de qualité des signaux. |

> Note d'architecture : en V1, la ré-évaluation périodique qui alimente les alertes (RM-25) et la qualité des signaux (RM-29) peut être déclenchée à la connexion / à l'ouverture des surfaces de suivi. La V2 l'**industrialise** en batch nocturne **sans dupliquer la logique** (même cœur d'analyse).

### Contraintes d'architecture imposées dès la V1

- L'ajout d'un **nouveau pattern** ne doit pas exiger de refonte du noyau (RM-04).
- Le passage au **batch nocturne** (V2) ne doit pas dupliquer la logique d'analyse : l'analyse à la demande et l'analyse automatisée partagent le même cœur.
- L'ajout d'**ETF / nouveaux actifs** (V2+) ne doit pas casser le modèle métier central ni élargir silencieusement le runtime V1 (RM-24, §4.4).

---

## 10. Critères de réussite de la V1

- ✅ Le système fonctionne **sans IA**.
- ✅ Ajouter un pattern ne nécessite **pas** de refonte du noyau.
- ✅ Une analyse peut être **rejouée et comprise** a posteriori.
- ✅ Le front présente des conclusions **adaptées à un débutant**.
- ✅ Le portefeuille **influence** la recommandation **sans modifier** la vérité technique.
- ✅ La reconstruction FIFO est déterministe et traçable.
- ✅ Les parcours `login` / `forgot-password` / `reset-password` sont couverts.
- ✅ `Notifications`, `Help center`, `Account`, `Admin` ont des rôles **distincts et documentés**.
- ✅ La séparation `Anonymous / User / Admin` est visible et cohérente.
- ✅ Les quatre lectures restent **séparées** (pas de méga-score).
- ✅ Chaque résultat exécutable produit un **plan d'action** et une **confiance expliquée**, sans inventer de vérité (RM-26, RM-27).
- ✅ Tout signal persisté reçoit une **issue réalisée ex post**, déclenchant **alertes** et **KPI de qualité** (RM-25, RM-29).
- ✅ Un nouvel utilisateur sans données obtient une **première valeur guidée** (onboarding).
- ✅ L'admin dispose de **KPI traçables** sur les 4 familles, sans nouvelle vérité métier inventée (RM-29).
