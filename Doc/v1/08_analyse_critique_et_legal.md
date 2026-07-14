# 08 — Analyse critique du produit et cadre légal (V1)

> **Propriétaire de** : l'analyse critique de la spec V1 (lacunes, risques produit, cas-limites, positionnement) et le cadre légal applicable au produit (droit français et européen).
>
> **Avertissement** : la Partie II (cadre légal) identifie les risques et formule des recommandations de posture produit. Elle **ne remplace pas un avis juridique professionnel**. Une consultation d'un avocat spécialisé AMF/RGPD est **requise avant toute mise en production commerciale**.
>
> **Principes** : les règles métier (RM-xx) sont dans [01](01_specification_produit.md). Les flux métier sont dans [07](07_flux_metier_client_admin.md). Les écrans sont dans [03](03_specification_ecrans.md). Les gaps code/doc sont dans [06](06_ecarts_doc_code.md).

---

## Partie I — Analyse critique du produit

### §1 Lacunes structurelles de la spec

Ces lacunes sont classées par impact décroissant sur la livraison V1 et la conformité légale.

#### 1.1 Inscription utilisateur non documentée

**Lacune** : aucun écran `B.4` (inscription), aucune user story `US-AUTH-00`, aucun flux de création de compte ne figurent dans les documents 03 et 04. La spec couvre `login` (`B.1`), `forgot-password` (`B.2`) et `reset-password` (`B.3`), mais pas l'inscription.

**Impact** :
- Le produit ne peut pas acquérir de nouveaux utilisateurs sans ce flux.
- La confirmation d'email (nécessaire pour tracer le consentement RGPD) n'est pas spécifiée.
- L'acceptation explicite des CGU et de la politique de confidentialité (obligation légale) n'est pas documentée.

**Action** : créer l'écran `B.4`, la user story `US-AUTH-00`, et le FLUX-C-01 (voir [07](07_flux_metier_client_admin.md#flux-c-01--inscription--onboarding-)).

#### 1.2 Suppression de compte absente

**Lacune** : aucun flux ni écran ne décrit la suppression de compte par l'utilisateur. Cette fonctionnalité est un **droit fondamental** (droit à l'effacement, Art. 17 RGPD) et son absence est une non-conformité documentée.

**Impact** :
- Non-conformité RGPD exploitable par la CNIL.
- Absence de clarté sur le sort des données associées (snapshots, transactions, watchlist).
- Absence de clarté sur l'impact des suppressions sur les KPI admin agrégés.

**Questions à arbitrer avant implémentation** :
- Suppression self-service via `C.15` ou demande via `C.13` (centre d'aide) ou workflow admin `D.2` ?
- Les snapshots sont-ils anonymisés (conservation agrégée pour les KPI) ou supprimés intégralement ?
- Délai de grâce avant suppression effective (permettant une réactivation) ?

**Action** : documenter le flux de suppression de compte dans [07](07_flux_metier_client_admin.md) et dans [04](04_user_stories.md). Ajouter un endpoint dans [05](05_contrats_donnees_api.md).

#### 1.3 Rôle admin unique aligné

**État** : le périmètre V1 retient désormais deux rôles actifs (`User`, `Admin`). `Admin` est l'unique rôle d'accès à l'espace de gouvernance et la documentation est alignée sur cette fusion.

**Impact positif** :
- Plus d'ambiguïté sur les guards et le routage post-login.
- Plus de matrice d'accès à maintenir pour un rôle sans usage métier distinct.

**Action** : conserver cette simplicité tant qu'aucun besoin métier prouvé ne justifie un sous-rôle admin séparé.

#### 1.4 Persistance des notifications non confirmée

**Lacune** : le composant frontend de notifications existe, mais la chaîne backend (création, lecture, mise à jour du statut) n'est pas confirmée (A-04 dans [06](06_ecarts_doc_code.md)).

**Impact** : les alertes proactives (FLUX-C-12, FLUX-T-01) ne peuvent pas fonctionner sans la persistance côté serveur.

#### 1.5 Comportement instrument désactivé non documenté

**Lacune** : si un instrument présent en watchlist ou en portefeuille est désactivé par un admin (FLUX-A-03), le comportement attendu côté user n'est pas spécifié dans [03](03_specification_ecrans.md) ni dans [07](07_flux_metier_client_admin.md).

**Comportement attendu suggéré** : l'instrument reste visible (position ouverte conservée) ; les nouvelles analyses échouent avec `UnsupportedInstrument` (RM-24) ; une alerte `DATA_STALE` peut se déclencher si les données deviennent obsolètes (RM-25). À documenter explicitement.

#### 1.6 Concurrence multi-devices non documentée

**Lacune** : un utilisateur connecté sur plusieurs appareils simultanément n'est pas traité. Le comportement des tokens (invalidation sur déconnexion, coexistence de refresh tokens) n'est pas spécifié.

#### 1.7 Sort des snapshots lors d'une suppression de compte

**Lacune** : liée à §1.2. Si un compte est supprimé, les snapshots associés (`analysisRunId` lié à `userId`) doivent être soit supprimés, soit anonymisés pour conserver leur contribution aux KPI admin. La décision n'est pas documentée.

---

### §2 Faiblesses UX et produit

#### 2.1 Les différenciateurs clés sont les moins construits

Le plan d'action (C-09, RM-26) et la confiance expliquée (C-08, RM-27) sont les deux capacités les plus différenciantes de PredictFinance vis-à-vis de n'importe quel outil de charting : elles transforment un signal brut en compréhension. Elles sont toutes deux 🔴 non construites.

**Risque** : sans ces deux blocs, le produit est perçu comme un outil de signal technique parmi d'autres, sans l'accompagnement pédagogique qui justifie son positionnement. Le différenciateur « aide à la décision explicable » n'existe pas encore fonctionnellement.

**Recommandation** : traiter C-09 et C-08 comme des priorités P0 avant la mise en production commerciale, conformément à [04](04_user_stories.md).

#### 2.2 L'onboarding vide crée un décrochage au premier jour

C.14 (onboarding-empty, C-05) est 🔴 non construit. Un investisseur arrivant sur une home vide sans guidage ne comprend pas la valeur du produit et abandonne avant de lancer sa première analyse.

**Risque** : taux de rétention J+1 très faible. Le funnel d'activation (inscription → watchlist → analyse → transaction, KPI famille C) sera structurellement cassé sans onboarding.

#### 2.3 Le glossaire inline et l'aide contextuelle manquent simultanément

C-04 (help-center, `C.13`) et C-13 (glossaire inline) sont tous deux 🔴 non construits. Un débutant confronté à des termes techniques (« rectangle de continuation », « score composite », « invalidation ») sans explication inline abandonnera avant d'atteindre la valeur.

#### 2.4 La lecture support est perçue comme un bonus

Le score fondamental et l'éligibilité PEA sont des différenciateurs forts face aux outils techniques purs. Pourtant, leur affichage est partiel (C-07, 🟡) et leur logique d'indisponibilité (PEA `Unknown` bloquant le composite, RM-14) est complexe à expliquer sans accompagnement.

**Risque** : la lecture support est perçue comme une fonctionnalité secondaire alors qu'elle est centrale à la proposition de valeur « analyse holistique (technique + fondamental + PEA) ».

#### 2.5 Absence de feedback utilisateur sur la qualité des analyses

La boucle de feedback ex post (FLUX-T-01) est automatisée (prix historiques), mais le produit ne dispose d'aucun mécanisme de retour **subjectif** de l'utilisateur sur la qualité d'une analyse (ex. « Cette recommandation m'a aidé »).

**Opportunité V2** : une notation simple (pouce levé/baissé sur un résultat d'analyse) alimenterait un signal qualitatif complémentaire aux KPI ex post.

#### 2.6 Simulation non reliée au plan d'action

Un investisseur qui simule un scénario (`C.8`) et un investisseur qui lit son plan d'action (`C.5`) explorent des dimensions complémentaires. Le lien entre les deux surfaces n'est pas documenté dans la spec ni dans les flux.

**Opportunité** : la simulation pourrait prépopuler ses champs depuis les niveaux du plan d'action (invalidation, cible), renforçant la cohérence pédagogique.

---

### §3 Cas-limites et flux manquants

Les cas suivants ne sont pas couverts dans la spec actuelle et pourraient produire des comportements indéfinis ou des incohérences UX.

| Cas-limite | Description | Risque | Décision requise |
|---|---|---|---|
| **Suppression de compte** | Voir §1.2 | RGPD, UX | Oui — flux + politique données |
| **Transaction Buy sur instrument `isActive=false`** | Un utilisateur a acheté un instrument qui est depuis désactivé. La position reste dans le portefeuille mais aucune nouvelle analyse ne peut être lancée. | UX confus | Documenter le comportement attendu |
| **Suppression d'un Buy consommé par un Sell FIFO** | Si l'utilisateur supprime une transaction d'achat déjà absorbée par une vente FIFO, le recalcul rétroactif est indéfini. | Corruption de données | À-ARBITRER (voir FLUX-C-06) |
| **Transition NOT_HELD → HELD après vente totale** | Si le portefeuille arrive à zéro après une vente, le dashboard `C.1` doit basculer les verbes de `HELD` à `NOT_HELD`. Le délai de ce basculement n'est pas documenté. | UX incohérente | Définir le déclencheur (immédiat après POST transaction ?) |
| **Alerte `DATA_STALE` sur instrument jamais analysé** | Un instrument en watchlist avec données obsolètes peut déclencher `DATA_STALE` même sans snapshot. Cohérent (basé sur la fraîcheur), mais à clarifier dans le wording de l'alerte. | Confusion utilisateur | Clarifier le wording |
| **Multi-device session** | Voir §1.6 | Sécurité, UX | À documenter |
| **Résultat ex post sur snapshot sans `TargetPrice`** | Si le snapshot ne porte pas de niveau cible (analyse avec `NoCrediblePattern`), le `SignalOutcome` doit être `NOT_EVALUABLE`. Ce cas est cohérent mais doit être documenté explicitement dans FLUX-T-01. | KPI erronés | Documenter dans la logique FLUX-T-01 |

---

### §4 Modèle de monétisation — risque non documenté

**Constat** : la spec V1 ne définit aucun modèle de monétisation. Cette absence est un risque de viabilité économique et conditionne plusieurs obligations légales.

**Options possibles** (non exhaustives) :

| Modèle | Description | Implications légales |
|---|---|---|
| **SaaS — abonnement mensuel/annuel** | Accès illimité contre abonnement récurrent | CGU solides, droit de rétractation 14j, Loi Chatel pour renouvellement annuel |
| **Freemium** | Accès limité gratuit + fonctions premium payantes | Risque de pression commerciale implicite sur les analyses ; à cadrer dans les CGU |
| **B2B — intégration courtier** | Revente du moteur à des courtiers comme enrichissement pédagogique | Cadre contractuel B2B différent ; potentiellement change le cadre AMF |

**Recommandation** : définir et documenter le modèle cible **avant la mise en production commerciale**, car il conditionne les obligations CGU, le cadre AMF et la politique RGPD.

---

### §5 Risques de dépendance au fournisseur de données

Le fournisseur de données de marché et fondamentales n'est pas nommé dans la spec (seuls `providerId`, `providerSymbol` et `lastProfileSyncUtc` apparaissent dans [05](05_contrats_donnees_api.md)). Cette opacité génère quatre risques.

| Risque | Description | Mitigation suggérée |
|---|---|---|
| **Continuité de service** | Changement d'API, de format ou de tarifs par le fournisseur → refonte des connecteurs | Nommer le fournisseur dans la spec ; documenter les connecteurs ; évaluer les alternatives |
| **Droits de redistribution** | Certains fournisseurs interdisent contractuellement la redistribution des données à des fins commerciales | Valider les droits d'utilisation avant commercialisation |
| **Couverture des small-caps FR** | Si le fournisseur n'a pas de données pour certaines actions françaises, elles tombent en `InsufficientData` systématiquement | Documenter et tester la couverture réelle du fournisseur |
| **Qualité des données fondamentales** | Données incorrectes (P/E erroné, ratios financiers décalés) → scores fondamentaux incorrects | Contrôle qualité périodique admin (FLUX-A-09) ; disclaimer sur la source |

---

### §6 Scalabilité et performance

#### 6.1 Analyse synchrone à la demande

L'analyse est lancée en POST synchrone (`/ClientFinance/analysis/run`) et l'utilisateur attend le résultat. Pour des patterns nécessitant de larges fenêtres historiques, la latence peut devenir problématique avec de nombreux utilisateurs simultanés.

**Mitigation V1** : cache des données de marché entre analyses proches ; timeout explicite avec retry.
**Mitigation V2** : batch nocturne (architecturalement planifié — le même moteur est réutilisé).

#### 6.2 Boucle ex post déclenchée à la connexion

**[À-ARBITRER A-11]** : si la boucle ex post est déclenchée à chaque connexion, un investisseur avec 20 instruments en watchlist et 10 en portefeuille génère 30 ré-évaluations à chaque login. Avec 1 000 connexions/jour, cela représente 30 000 ré-évaluations quotidiennes en V1.

**Mitigation** : mutualiser le mécanisme de ré-évaluation ; ne ré-évaluer que les instruments dont les données ont changé depuis la dernière ré-évaluation (cf. [06 §3](06_ecarts_doc_code.md#3-%C3%A9crans--capacit%C3%A9s-cible-%C3%A0-construire), mutualisation clé).

#### 6.3 KPI admin sur table `AnalysisSnapshot`

À 1 000 utilisateurs × 5 analyses/semaine × 52 semaines = 260 000 lignes/an dans `AnalysisSnapshot`. Les requêtes d'agrégation pour les KPI admin (FLUX-A-10) sans indexation correcte deviendront lentes.

**Recommandation** : prévoir des index sur `(instrumentId, createdAt)` et `(userId, createdAt)` dès V1 ; envisager des vues matérialisées pour les KPI de famille A.

#### 6.4 Logs `Analytic` : croissance non bornée

Les logs `Analytic` (IP + identifiant, RM-29b) s'accumulent sans politique de rotation ou d'anonymisation documentée. Au-delà de 13 mois, ils constituent une non-conformité RGPD (voir §II.2).

---

### §7 Positionnement concurrentiel

**Niche cible** : pédagogie + actions françaises + analyse déterministe explicable + éligibilité PEA = combinaison absente du marché actuel.

| Concurrent | Force | Faiblesse vs. PredictFinance |
|---|---|---|
| TradingView | Charting professionnel, communauté | Pas pédagogique, pas de scoring fondamental intégré, pas de PEA |
| Boursorama / BFM Bourse | Marque, trafic, données | Informations génériques, pas d'analyse guidée déterministe |
| Robo-advisors (Yomoni, etc.) | Gestion déléguée, simplicité | Modèle opposé (délégation vs. compréhension) |
| Blogs / YouTube finance (Parlons Long Terme, etc.) | Contenu pédagogique riche | Pas d'outil d'analyse personnalisé |

**Remparts concurrentiels** : la vitesse d'exécution (être en production avant qu'un acteur établi imite) et la profondeur pédagogique (plan d'action + confiance expliquée) sont les barrières à l'entrée. Leur non-construction retarde la différenciation.

**Risque structurel** : si un acteur établi (Boursorama, BNP) décide d'intégrer une fonctionnalité similaire dans sa plateforme, le rapport de forces est défavorable sur les ressources. La niche PEA + pédagogie + traçabilité doit être consolidée rapidement.

---

### §8 Confiance et rétention des utilisateurs

#### 8.1 La boucle ex post est le principal vecteur de confiance

Quand un investisseur peut constater que le moteur avait raison (`TARGET_HIT`) ou tort (`INVALIDATION_HIT`) sur des analyses passées, il construit une relation de confiance basée sur des preuves. Sans cette capacité (C-11, 🔴), le produit est perçu comme un oracle sans accountability.

**Priorité** : la construction de FLUX-T-01 est structurante pour la rétention à moyen terme, pas seulement pour les KPI admin.

#### 8.2 Risque de sur-confiance des débutants

Un investisseur débutant peut interpréter `Confiance élevée` comme une garantie de résultat. La confiance expliquée (RM-27) atténue ce risque, mais elle est 🔴 non construite. En son absence, un disclaimer visible sur l'écran `C.5` est **indispensable** (voir §II.1).

#### 8.3 Recommandation erronée par mauvais contexte de détention

Si un investisseur déclare `NOT_HELD` alors qu'il détient l'instrument, la recommandation sera structurellement incorrecte (verbes non détenus au lieu de détenus). Le produit ne peut pas détecter cette incohérence.

**Recommandation UX** : ajouter un avertissement visible à l'écran `C.4` lors du choix du contexte de détention (ex. « Vérifiez que ce choix correspond à votre situation réelle. »).

---

## Partie II — Cadre légal

> **Rappel** : les recommandations ci-dessous sont des orientations de posture produit. Elles ne constituent pas un avis juridique. Une consultation d'un avocat spécialisé en droit financier et en droit des données personnelles est **obligatoire** avant toute mise en production commerciale.

---

### §1 AMF et réglementation financière

#### 1.1 Question centrale : PredictFinance est-il un CIF ?

Le statut de **Conseiller en Investissement Financier (CIF)** est régi par l'article L541-1 du Code monétaire et financier. Il est requis pour toute personne qui, « à titre de profession habituelle, fournit des conseils en investissement ». Le conseil en investissement est défini par **MIF 2** (Directive 2014/65/UE, transposée en France) comme « la fourniture de recommandations personnalisées à un client concernant une ou plusieurs transactions portant sur des instruments financiers ».

#### 1.2 Analyse de la qualification

**Arguments favorables à la non-qualification en CIF** :

1. Le produit est déclaré explicitement **pédagogique** ; il n'est pas présenté comme un service de conseil.
2. Il n'accède pas aux comptes bancaires et ne passe aucun ordre (aucun service d'investissement au sens MIF 2).
3. Les règles d'analyse sont **déterministes** : le même instrument à la même date produit le même résultat pour tous les utilisateurs. Il n'y a pas de recommandation individualisée au sens d'une prise en compte du profil global de l'investisseur (appétit au risque, horizon d'investissement, patrimoine global).
4. L'utilisateur **déclare lui-même** le contexte de détention — ce n'est pas une prise en compte de sa situation par le service.

**Arguments défavorables (risques de requalification)** :

1. La recommandation contextualisée par la détention (verbes `HELD`/`NOT_HELD`, RM-10) **ressemble à une recommandation personnalisée** au sens de MIF 2 — elle est adaptée à la situation spécifique de l'utilisateur.
2. Les verbes d'action sont **explicites** : `Acheter`, `Vendre`, `Renforcer`, `Alléger` — ce ne sont pas des signaux bruts mais des prescriptions d'action.
3. Le produit est fourni **à titre habituel** dès lors qu'il est en production commerciale.
4. Les termes « aide à la décision » et « analyse » peuvent être requalifiés en « conseil » par les régulateurs selon la présentation.

#### 1.3 Recommandations de posture produit

| Recommandation | Description |
|---|---|
| **Vocabulaire** | Nommer systématiquement les résultats « aide pédagogique à la décision » et non « conseil en investissement » ni « recommandation » au sens financier. |
| **Disclaimer visible** | Afficher sur chaque écran présentant une recommandation (`C.5`, `C.6`, `C.1`) : « Ce résultat est une analyse pédagogique déterministe et ne constitue pas un conseil en investissement personnalisé au sens de la directive MIF 2. » |
| **Formulation des verbes** | Éviter les formulations à certitude implicite (« vous devriez acheter ») ; préférer des formulations conditionnelles (« Piste d'exploration dans ce contexte : Acheter »). |
| **Profil de risque** | Ne jamais présenter le produit comme prenant en compte le profil de risque global de l'utilisateur — ce n'est pas le cas (la reco dépend uniquement du contexte de détention déclaré). |
| **Consultation juridique** | **Obligatoire avant commercialisation** : obtenir une position juridique formelle d'un avocat spécialisé AMF/MIF 2 sur la qualification du service. |

#### 1.4 Implications si la qualification CIF est retenue

Si un avocat conclut que le produit doit être qualifié de CIF :
- Agrément AMF obligatoire.
- Adhésion à une association professionnelle agréée (ex. CNCIF, ANACOFI-CIF).
- Obligation de profilage de risque client (questionnaire MIF).
- Exigences de documentation et de reporting réglementaire.

Ces obligations transformeraient radicalement le produit — d'où l'importance de l'avis juridique préalable.

**Niveau de risque : CRITIQUE** — risque le plus élevé du produit.

---

### §2 RGPD / GDPR

#### 2.1 Données personnelles traitées

| Catégorie | Données | Sensibilité |
|---|---|---|
| Identité | Email, nom d'affichage (`displayName`) | Standard |
| Compte | Mot de passe hashé (BCrypt), rôle, statut | Standard |
| Données financières | Lignes de portefeuille (quantités, prix, dates, frais) | Sensible au sens pratique (non Art. 9 RGPD) |
| Données d'usage | Watchlist, historique d'analyses, snapshots | Standard |
| Données techniques | Logs `Analytic` (IP + identifiant utilisateur), RM-29b | Nominatives |

> Les données financières de portefeuille ne sont **pas** des données « sensibles » au sens de l'Article 9 RGPD (qui couvre la santé, l'origine ethnique, etc.). Elles restent des données personnelles ordinaires mais présentent un risque pratique élevé en cas de fuite (usurpation d'identité financière, ciblage commercial non consenti).

#### 2.2 Bases légales de traitement

| Traitement | Base légale (Art. 6 RGPD) |
|---|---|
| Compte utilisateur, watchlist, portefeuille, analyses | Art. 6(1)(b) — Exécution du contrat |
| Communications liées au service (alertes, notifications) | Art. 6(1)(b) — Exécution du contrat |
| Communications marketing (si applicable) | Art. 6(1)(a) — Consentement |
| Logs techniques de sécurité (rate limiting, lockout) | Art. 6(1)(f) — Intérêt légitime |
| Logs `Analytic` d'usage (IP + identifiant) | Art. 6(1)(f) — Intérêt légitime (à encadrer) |

#### 2.3 Droits des personnes — lacunes à combler

| Droit | Article RGPD | Lacune identifiée | Action requise |
|---|---|---|---|
| **Droit d'accès** | Art. 15 | Aucun endpoint d'export des données personnelles dans [05](05_contrats_donnees_api.md). | Ajouter un endpoint `GET /account/data-export` retournant toutes les données de l'utilisateur. |
| **Droit à l'effacement** | Art. 17 | Aucun flux de suppression de compte (voir §I.1.2). | Implémenter le flux de suppression (self-service ou admin). |
| **Droit à la portabilité** | Art. 20 | Aucun export en format lisible (CSV/JSON) documenté. | Ajouter l'export portefeuille + watchlist + historique en CSV/JSON. |
| **Droit d'opposition** | Art. 21 | Les préférences de notification (`C.15`) couvrent partiellement ce droit pour les alertes. Les analytics d'usage ne peuvent pas être opposés. | Ajouter une option dans `C.15` pour s'opposer aux traitements basés sur l'intérêt légitime (analytics). |
| **Droit à la limitation** | Art. 18 | Non documenté. | À documenter dans la politique de confidentialité. |

#### 2.4 Durées de conservation

Aucune durée de conservation n'est documentée dans la spec actuelle. Elles doivent être définies et rendues publiques dans la politique de confidentialité.

| Donnée | Durée recommandée | Fondement |
|---|---|---|
| Données de compte (email, profil) | Durée de vie du compte + 3 ans après fermeture | Conservation pour litiges éventuels |
| Données de portefeuille (transactions) | Durée de vie du compte + 5 ans (données à caractère fiscal potentiel) | Prudence ; à valider avec juriste |
| Snapshots d'analyse | Durée de vie du compte | Données de service |
| Logs `Analytic` (IP + identifiant) | **13 mois maximum**, puis anonymisation | Recommandation CNIL standard |
| Logs de sécurité (tentatives, lockout) | 6 à 12 mois | Lutte contre la fraude |
| Données après suppression de compte | Anonymisation immédiate ou sous 30 jours | RM-29b + Art. 17 RGPD |

> **Formalisation de A-10** : la décision A-10 (« RGPD / anonymisation des KPI nominatifs », actuellement À-ARBITRER dans [06](06_ecarts_doc_code.md)) doit être tranchée en conformité avec les durées ci-dessus. Ce n'est plus une décision optionnelle — c'est une **obligation légale**.

#### 2.5 DPO (Délégué à la Protection des Données)

Le DPO est obligatoire (Art. 37 RGPD) pour les organismes dont l'activité principale consiste en traitements à grande échelle de données sensibles. En phase initiale (startup, faible volume), le DPO n'est **pas immédiatement obligatoire** pour PredictFinance.

Cependant, la CNIL recommande la désignation volontaire d'un DPO dès le démarrage. Une personne interne formée peut remplir ce rôle.

#### 2.6 Cookies et tracking

La spec ne mentionne pas de politique de cookies. Si des outils analytiques (Matomo, Google Analytics, etc.) sont utilisés :
- Les cookies analytiques nécessitent un **consentement préalable** (Loi Informatique et Libertés + recommandations CNIL).
- Les cookies strictement nécessaires (session JWT, sécurité) sont exemptés de consentement.
- Une bannière de consentement conforme est requise.

#### 2.7 Registre des activités de traitement

L'Art. 30 RGPD impose à tout responsable de traitement de tenir un registre des activités de traitement (RAT). Ce registre n'est pas mentionné dans la spec. Il doit être créé et maintenu.

---

### §3 Droit de la consommation (si abonnement B2C)

Ces obligations s'appliquent si PredictFinance propose un abonnement payant à des particuliers (service en ligne, contrat à distance).

#### 3.1 Droit de rétractation

L'Art. L221-18 du Code de la consommation accorde 14 jours de rétractation pour les contrats conclus à distance.

**Attention** : si l'accès au service est fourni immédiatement (pas de délai entre souscription et accès), l'utilisateur doit **explicitement renoncer** à son droit de rétractation pour utiliser le service avant l'expiration du délai de 14 jours. Ce renoncement doit être documenté lors de la souscription.

#### 3.2 CGU obligatoires

Les CGU doivent inclure au minimum :
- Description précise du service (nature pédagogique, non-conseil).
- Prix TTC, durée, conditions de résiliation.
- Limitation de responsabilité claire (notamment sur la qualité des analyses et des données).
- Juridiction compétente et loi applicable.
- Médiation de la consommation (obligatoire en France depuis 2016, Art. L612-1 Code conso).
- Clause spécifique sur la nature non-contraignante des analyses.

#### 3.3 Pratiques commerciales trompeuses

L'Art. L121-1 du Code de la consommation interdit les pratiques commerciales trompeuses. Risques spécifiques à PredictFinance :

- Mise en avant de performances passées du moteur (taux d'atteinte de cible) sans mention des limitations → potentiellement trompeur.
- Utilisation du terme « recommandation » dans la communication marketing sans clarification de sa nature pédagogique et non-réglementée.
- Toute formulation impliquant une garantie de résultat.

#### 3.4 Renouvellement automatique (Loi Chatel)

Pour un abonnement annuel, l'Art. L215-1 du Code de la consommation impose d'informer l'utilisateur de son droit à ne pas renouveler, **1 à 3 mois avant la date d'anniversaire**.

---

### §4 Responsabilité liée à l'information PEA

#### 4.1 Risque spécifique

Si le registre PEA interne indique `ConfirmedEligible` pour un instrument qui n'est pas réellement éligible PEA, et qu'un utilisateur l'achète dans son PEA sur la foi de cette information, il pourrait subir :
- Une remise en cause de l'avantage fiscal.
- Dans les cas graves, la clôture de son PEA.

#### 4.2 Atténuants déjà en place

La spec dispose de mécanismes d'atténuation solides :
- Source et date de vérification enregistrées dans le registre (`checkedUtc`, `policyVersion`).
- Processus de gouvernance admin pour la mise à jour (FLUX-A-04).
- `Unknown` jamais traité silencieusement comme éligible (RM-15).

#### 4.3 Recommandations complémentaires

1. **Disclaimer systématique** sur chaque affichage du statut PEA : « Le statut PEA présenté est fourni à titre informatif et pédagogique uniquement. Confirmez l'éligibilité auprès de votre courtier avant tout achat en PEA. »
2. **Mention dans les CGU** : l'information PEA n'est pas garantie et peut être obsolète ; la responsabilité finale d'un investissement en PEA incombe à l'investisseur et à son courtier.
3. **Processus de mise à jour** : documenter la fréquence minimale de révision du registre PEA (ex. trimestrielle).

**Niveau de risque : MOYEN** — atténuable par les disclaimers et la gouvernance.

---

### §5 Sécurité des données financières

#### 5.1 Obligations en cas de violation de données

En cas de fuite de données (y compris les données de portefeuille) :
- **Notification à la CNIL dans les 72 heures** si la violation présente un risque pour les droits et libertés (Art. 33 RGPD).
- **Notification aux personnes concernées** si le risque est élevé (Art. 34 RGPD).

Un plan de réponse aux incidents doit être préparé et testé.

#### 5.2 Mesures déjà documentées

- BCrypt pour les mots de passe (documenté dans [06 §5](06_ecarts_doc_code.md#5-synth%C3%A8se-de-maturit%C3%A9)).
- JWT + refresh token.
- Rate limiting et lockout.

#### 5.3 Mesures manquantes à documenter et implémenter

| Mesure | Priorité |
|---|---|
| Chiffrement at rest des données de portefeuille | ÉLEVÉE — données financières personnelles |
| Politique de logs d'accès aux données sensibles | ÉLEVÉE |
| Plan de réponse aux incidents (CNIL 72h) | CRITIQUE |
| Tests de pénétration planifiés | ÉLEVÉE — avant mise en production |
| HTTPS enforced, headers de sécurité (HSTS, CSP) | CRITIQUE |

---

### §6 Obligations d'audit trail

La spec est déjà solide sur l'auditabilité des analyses (RM-03, RM-19, RM-20). L'enjeu légal est de cadrer les **durées de conservation** des traces nominatives.

Ce tableau formalise la décision A-10 (actuellement À-ARBITRER dans [06](06_ecarts_doc_code.md)) en **obligation légale non optionnelle** :

| Type de log | Durée maximale nominative | Action après expiration |
|---|---|---|
| Logs `Analytic` (IP + `userId`) | **13 mois** | Anonymisation (supprimer l'IP et l'identifiant, conserver les agrégats pour KPI) |
| Logs de sécurité (tentatives de connexion, lockout) | 12 mois | Suppression |
| Snapshots d'analyse (`userId` en clé étrangère) | Durée de vie du compte | Anonymisation ou suppression lors de la suppression du compte |
| Données de compte (email, profil) | Durée de vie du compte + 3 ans | Suppression |

> **Recommandation d'implémentation** : mettre en place un job planifié (cadence : mensuelle) qui anonymise les logs `Analytic` âgés de plus de 13 mois. Consigner les anonymisations dans un registre interne.

---

### §7 Documents légaux à produire avant mise en production

Ces documents sont **obligatoires** au sens légal (LCEN, RGPD, Code de la consommation) et doivent être produits, validés par un juriste, et accessibles depuis toutes les pages du produit.

| Document | Contenu essentiel | Priorité | Obligation légale |
|---|---|---|---|
| **CGU / Conditions Générales d'Utilisation** | Nature pédagogique (non-conseil), limitations de responsabilité, prix, résiliation, médiation conso | CRITIQUE | Code de la consommation, LCEN |
| **Politique de Confidentialité** | Données collectées, bases légales, droits des utilisateurs, durées de conservation, coordonnées DPO | CRITIQUE | Art. 13 RGPD |
| **Mentions légales** | Identité de l'éditeur, hébergeur, directeur de publication | CRITIQUE | Art. 6 LCEN |
| **Disclaimer AMF / financier** | Visible sur chaque écran de recommandation (`C.5`, `C.6`, `C.1`) | CRITIQUE | Posture AMF / MIF 2 |
| **Politique de Cookies** | Types, finalités, durées, consentement | ÉLEVÉE | Loi Informatique et Libertés, CNIL |
| **Disclaimer PEA** | Visible sur chaque affichage de statut PEA | ÉLEVÉE | Responsabilité civile |

---

### §8 Matrice des risques légaux

| Risque | Probabilité | Sévérité | Score | Action prioritaire |
|---|---|---|---|---|
| **Qualification AMF comme CIF** | Moyenne | Critique — blocage opérationnel, amende, cessation | **CRITIQUE** | Consultation avocat AMF/MIF 2 avant commercialisation |
| **RGPD — absence flux suppression de compte** | Élevée (lacune connue) | Élevée — amende CNIL jusqu'à 4% du CA mondial | **ÉLEVÉ** | Implémenter le flux + documenter les durées de conservation |
| **Droits redistribution fournisseur de données** | Inconnue (fournisseur non nommé) | Élevée — cessation de service possible | **ÉLEVÉ** | Nommer le fournisseur, valider les droits contractuels |
| **Logs nominatifs non anonymisés (A-10)** | Élevée (lacune documentée) | Moyenne — sanction CNIL | **MOYEN** | Planifier la rotation à 13 mois dès V1 |
| **Responsabilité information PEA erronée** | Faible (gouvernance rigoureuse) | Moyenne — litige individuel | **MOYEN** | Disclaimers systématiques + mention CGU |
| **Pratiques commerciales trompeuses** | Faible (si bien cadrées) | Élevée — amende DGCCRF + atteinte réputation | **MOYEN** | CGU solides, modèle documenté avant commercialisation |
| **Fuite données de portefeuille** | Faible (si sécurité correcte) | Élevée — CNIL + préjudice utilisateurs | **MOYEN** | Chiffrement at rest + plan de réponse incidents |
| **Absence de mentions légales LCEN** | Élevée (non encore créées) | Faible — amende administrative | **FAIBLE** | Ajouter avant toute mise en ligne publique |
| **Absence de droit d'accès / portabilité** | Élevée (lacune API connue) | Moyenne — CNIL | **MOYEN** | Ajouter endpoints export dans [05](05_contrats_donnees_api.md) |

---

## Synthèse exécutive

### Les 3 actions les plus urgentes

1. **Consultation d'un avocat AMF/MIF 2** avant commercialisation — le risque de requalification en CIF est le risque existentiel du produit.
2. **Implémenter le flux de suppression de compte** — lacune RGPD documentée avec risque de sanction CNIL.
3. **Rédiger et publier les CGU, la politique de confidentialité et les mentions légales** — obligations légales bloquant toute mise en ligne publique.

### Les 3 faiblesses produit les plus impactantes

1. **Plan d'action + confiance expliquée non construits** (C-08, C-09) — sans eux, le différenciateur pédagogique n'existe pas fonctionnellement.
2. **Onboarding vide non construit** (C-05) — décrochage systématique au premier jour.
3. **Boucle ex post non construite** (C-11) — sans elle, le produit n'a pas de mécanisme de confiance basé sur des preuves.

---

## §9 — Recommandations stratégiques issues de l'analyse multi-perspective (2026-05-28)

> Cette section synthétise les conclusions de 4 analyses finance (quick-take, investment memo, risk assessment, variant perception) réalisées sur la base de la documentation V1. Le diagnostic complet est dans [09](09_diagnostic_strategique.md).

### 9.1 Synthèse de la thèse différenciée

Le marché percevra probablement PredictFinance comme « un screener technique de plus ». Cette perception est **erronée si — et seulement si — les différenciateurs sont construits avant la croissance**. La valeur de PredictFinance n'est pas dans la détection de patterns (disponible gratuitement sur TradingView) mais dans trois actifs qui n'existent nulle part ailleurs combinés :

1. **L'explication déterministe versionnée** (C-08 Confidence Breakdown + C-09 Plan d'Action) — le « pourquoi » et le « quoi faire », pas juste le signal.
2. **La contextualisation par le portefeuille FIFO réel** — la recommandation tient compte de ce que l'utilisateur détient réellement.
3. **La boucle ex post vérifiable** (C-11) — l'utilisateur peut voir si les analyses passées ont atteint leur cible ou ont été invalidées.

Ces trois actifs, associés à l'éligibilité PEA native et au focus actions françaises, constituent un fossé défensif inimitable à court terme par les grands acteurs — qui ont structurellement un conflit d'intérêt entre la pédagogie qui réduit les transactions impulsives et leur modèle de revenus basé sur les commissions.

**Risque sous-estimé par le consensus** : la requalification AMF en CIF. Les fondateurs de startups fintech françaises sous-estiment systématiquement ce risque. La personnalisation contextuelle (verbes d'action selon les holdings déclarés) constitue potentiellement une « recommandation personnalisée à titre habituel » au sens de MIF 2. Probabilité estimée : 40–60%. Impact : existentiel.

### 9.2 Mise à jour de la matrice de risques §8

La matrice §8 ci-dessus couvre les risques légaux. L'analyse multi-perspective a élargi la matrice à 17 risques (produit, tech, légal, GTM). La matrice complète est dans [09 §3](09_diagnostic_strategique.md#3-matrice-de-risques-actualisée).

**Deux risques CRITIQUES absolus** (bloquants avant toute commercialisation) :

| ID | Risque | Probabilité | Impact | Action |
|---|---|---|---|---|
| **R1** | **Requalification AMF en CIF** | Moyenne (40–60%) | Existentiel | Avocat AMF/MIF 2 — avis écrit AVANT lancement |
| **R3** | **Modèle économique non défini** | Élevée | Existentiel | Arbitrer pricing avant toute acquisition utilisateurs |

### 9.3 Séquence de déblocage recommandée

#### P0 — Avant tout lancement commercial (bloquant)

1. **Consultation avocat AMF/MIF 2** → avis écrit sur la qualification CIF du produit
2. **Définir le modèle économique** → pricing, freemium vs. payant, B2B optionnel
3. **Contractualiser le fournisseur de données** → droits de redistribution validés
4. **Construire C-09 Plan d'Action + C-08 Confidence Breakdown** → les différenciateurs pédagogiques
5. **Implémenter suppression compte (RGPD Art. 17) + export données (RGPD Art. 20)**
6. **Construire écran onboarding-empty (C-05)** → bloquer l'abandon J+1
7. **Rédiger CGU, politique de confidentialité, mentions légales, disclaimer AMF** → après avis AMF

#### P1 — Avant croissance (pré-lancement à 6 mois post-lancement)

1. **Beta privée 50–100 utilisateurs** → NPS > 30, rétention S4 > 60%
2. **Job anonymisation logs Analytic > 13 mois** → obligation CNIL (A-10, non optionnel)
3. **Construire boucle ex post C-11** → premiers TARGET_HIT / INVALIDATION_HIT visibles
4. **Définir stratégie acquisition canal #1** → SEO/contenu, communautés, partenariat

### 9.4 Positionnement concurrentiel confirmé

L'analyse de la concurrence confirme l'absence de concurrent direct sur la combinaison actions françaises + PEA + pédagogie déterministe + débutants. Le tableau des 6 acteurs les plus proches montre que :

- **TradingView** est global, sans pédagogie ni PEA → menace faible
- **Boursorama / BNP** ont l'audience mais **un conflit structurel** : la pédagogie qui explique « Attendre » réduit leurs commissions → incentive aligné contre l'imitation
- **Les acteurs éditoriaux** (Investir.fr, Capital.fr) n'ont pas la culture produit SaaS pour pivoter rapidement

**Condition de succès** : livrer les différenciateurs (C-08/C-09/C-11) avant qu'une startup concurrente identifie la même niche. La boucle ex post avec 12–18 mois de données accumulées est temporellement inimitable.

### 9.5 Valorisation indicative

PredictFinance est une société privée. Ces estimations sont des ordres de grandeur à des fins de pilotage interne.

| Scénario | Probabilité | Valeur | Hypothèse clé |
|---|---|---|---|
| **Bull** | 25% | **€6M** | 5 000 abonnés × €20/mois × 5× ARR — fossé ex post constitué |
| **Base** | 50% | **€540k** | 1 000 abonnés × €15/mois × 3× ARR — C-08/C-09 livrés |
| **Bear** | 25% | **€75k** | CIF requalifié ou PMF raté |
| **Valeur attendue** | 100% | **~€1,6M** | 0,25×6 + 0,50×0,54 + 0,25×0,075 |

Le plus grand levier de création de valeur est la levée du risque CIF/AMF (R1) — elle seule déplace mécaniquement de la probabilité du scénario bear vers les scénarios base et bull.
