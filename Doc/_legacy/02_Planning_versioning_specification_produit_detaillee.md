# Planning de versioning détaillé
## Application d’aide à l’investissement par analyse de patterns

Document de pilotage produit — feuille de route détaillée de la V1 à la V3, alignée sur le besoin métier validé, le gel contractuel V1 et la spec écran canonique.

- Principe de versioning : construire un socle métier explicable et extensible avant d’élargir la couverture fonctionnelle.
- V1 : refonte métier et technique prioritaire, entièrement exploitable sans IA.
- V2 : industrialisation, automatisation et montée en couverture.
- V3 : enrichissements avancés, personnalisation maîtrisée et expansion du périmètre.

## 1. Principes de construction de la feuille de route

La trajectoire produit doit privilégier la stabilité du noyau métier avant l’ajout de sophistication algorithmique ou de nouveaux périmètres de marché.

Chaque version doit apporter une valeur utilisateur nette, tout en préparant explicitement l’ouverture du système à de nouveaux patterns, actifs, marchés et modes d’analyse.

Le versioning doit éviter de figer prématurément la solution autour d’un actif unique, d’un seul pattern ou d’une implémentation difficile à faire évoluer.

## 2. Vision globale par version

| Version | Objectif directeur | Résultat attendu |
|---|---|---|
| V1 | Créer le socle produit et la refonte architecturale cible. | Application exploitable pour actions françaises, analyse journalière à la demande, moteur déterministe, historisation complète, parcours d’accès et surfaces d’orientation cohérents. |
| V2 | Industrialiser le moteur et enrichir l’expérience d’usage. | Batchs nocturnes, enrichissement des patterns, vues de suivi, alertes et meilleures capacités d’analyse comparative. |
| V3 | Étendre le périmètre et augmenter l’intelligence produit de manière contrôlée. | Couverture multi-actifs/multi-marchés, assistance pédagogique enrichie, personnalisation progressive et performance analytique accrue. |

## 3. V1 — Refonte cœur produit

### 3.1 Objectif version

Livrer une première version fiable, explicable et ouverte, sans dépendre de l’IA, avec un découpage métier durable entre données de marché, détection de patterns, évaluation du risque, recommandation, historisation et surfaces produit de navigation.

### 3.2 Objectifs détaillés

- Refondre l’architecture pour placer l’API comme source de vérité métier.
- Mettre en place un moteur de patterns extensible.
- Séparer strictement détection technique, évaluation du risque et recommandation contextualisée.
- Permettre la gestion de watchlist et portefeuille complet.
- Historiser chaque analyse sous forme de snapshot versionné.
- Rendre les résultats pédagogiques et compréhensibles pour un débutant.
- Verrouiller les parcours d’accès et d’orientation visibles dans la spec écran V1.

### 3.3 Périmètre fonctionnel V1

| Domaine | Fonctionnalités V1 |
|---|---|
| Authentification | Login, forgot password, reset password, séparation explicite des espaces après connexion. |
| Compte utilisateur | Espace personnel avec profil, préférences, sécurité et accès aux réglages de notifications. |
| Watchlist | Ajout, suppression et consultation de valeurs suivies. |
| Portefeuille | Gestion de plusieurs lignes d’achat par actif avec quantité, prix, date, PRU, frais et devise. Reconstruction des lignes ouvertes après ventes selon une règle FIFO stricte définie pour la V1. |
| Analyse | Analyse journalière à la demande sur actions françaises prises en charge. |
| Patterns | Détection déterministe des patterns priorisés, pattern principal + patterns alternatifs compatibles. |
| Risque | Invalidation, stop loss, take profit, ratio risque/rendement, volatilité et drawdown potentiel selon disponibilité. |
| Restitution | Résumé pédagogique, justification explicite, recommandation contextualisée. |
| Historique | Conservation complète des analyses et de leurs évolutions. |
| Notifications | Centre de notifications utile, orienté priorisation et routage vers les bonnes pages. |
| Aide | Help center contextuel expliquant résultats, snapshots, pages et limites du produit. |
| Administration | Overview admin, gestion utilisateurs et surfaces de gouvernance visibles dans la spec V1. |

### 3.4 Périmètre technique V1

- Normalisation des données de marché dans l’API.
- Contrat de pattern modulaire : identifiant, prérequis, fenêtre d’analyse, détection, validation, invalidation, scoring.
- Mécanisme de version du moteur d’analyse.
- Persistance des snapshots d’analyse.
- API de restitution pour le front.
- Services d’authentification et de récupération d’accès.
- API dédiées aux surfaces `account`, `notifications`, `help-center` et `admin-users`.
- Service de reconstruction du contexte portefeuille compatible avec la règle FIFO V1 pour les lignes ouvertes après ventes.

### 3.5 Livrables de version V1

| Livrable | Description de fin de version |
|---|---|
| Socle applicatif refondu | Architecture claire, maintenable et ouverte aux extensions. |
| Moteur de patterns V1 | Premiers patterns implémentés avec règles déterministes et tests. |
| Portefeuille & watchlist | Fonctionnels, persistés et reliés aux analyses, avec contextualisation portefeuille fondée sur des lignes ouvertes reconstruites en FIFO. |
| Restitution pédagogique | Pages et API restituant clairement l’analyse et la recommandation. |
| Accès & self-service | Login, récupération d’accès, compte et sécurité cohérents. |
| Surfaces d’orientation | Notifications et help center cohérents, non redondants, raccordés aux pages métier. |
| Administration V1 | Espace admin distinct avec première base de gestion utilisateurs et de gouvernance fonctionnelle. |
| Historique versionné | Consultation des anciennes analyses et base pour l’évaluation ex post. |
| Cadre qualité | Tests métier, règles de traçabilité et premiers critères de monitoring. |

### 3.6 Critères de sortie V1

- Le système fonctionne sans IA.
- L’ajout d’un nouveau pattern ne nécessite pas de refonte du noyau.
- Une analyse peut être rejouée et comprise a posteriori.
- Le front présente des conclusions adaptées à un débutant.
- Le portefeuille influence la recommandation sans modifier la vérité technique calculée.
- La reconstruction des lignes ouvertes après ventes est déterministe, traçable et conforme à la règle FIFO V1.
- Les parcours `login`, `forgot password` et `reset password` sont explicitement couverts.
- `Help center`, `notifications` et `account` ont des rôles distincts et documentés.
- La séparation `anonymous / user / admin` est visible et cohérente.
- La documentation V1 est suffisamment précise pour permettre, dans une conversation ultérieure, de produire des backlogs API écran par écran sans réinterprétation majeure.

## 4. V2 — Industrialisation et enrichissement

### 4.1 Objectif version

Passer d’un produit fonctionnel à un produit plus industrialisé, plus automatisé et plus riche dans son suivi utilisateur.

### 4.2 Axes d’évolution V2

- Automatiser une partie des analyses via batch nocturne.
- Étendre le catalogue de patterns et améliorer la profondeur de restitution historique.
- Ajouter des vues de synthèse sur les analyses récentes, les signaux en cours et les signaux invalidés.
- Ajouter une logique d’alertes contextuelles sur événements significatifs.
- Structurer davantage la mesure ex post des performances de signaux.
- Préparer l’ouverture à d’autres instruments proches, notamment les ETF en V2 puis d’autres actions hors premier marché couvert dans des contrats ultérieurs.

### 4.3 Périmètre fonctionnel V2

| Domaine | Amélioration V2 |
|---|---|
| Analyses automatiques | Pré-calcul nocturne d’analyses consultables au réveil. |
| Tableaux de bord | Vue consolidée des valeurs suivies, derniers signaux, analyses les plus pertinentes. |
| Alertes | Signalement des changements d’état : validation, invalidation, proximité d’un niveau clé. |
| Historique comparatif | Visualisation de l’évolution des patterns, probabilités et recommandations dans le temps. |
| Mesure ex post | Premier tableau de bord interne de performance des signaux par horizon et par pattern. |
| Couverture | Montée en puissance progressive sur davantage de valeurs et ETF prévus pour la V2 après contrat dédié validé. |

### 4.4 Périmètre technique V2

- Orchestration batch nocturne robuste.
- Stratégie de cache et de recalcul maîtrisée.
- Modèle de scoring affiné sans casser la traçabilité.
- Services de notification et de planification plus riches que le centre de notifications V1.
- Monitoring plus avancé des runs d’analyse et des anomalies de données.

### 4.5 Critères de sortie V2

- Le système supporte à la fois l’analyse à la demande et l’analyse automatisée.
- L’utilisateur peut suivre plus facilement les changements de situation dans le temps.
- L’équipe produit dispose de premiers indicateurs fiables sur la qualité des signaux.

## 5. V3 — Expansion et intelligence produit contrôlée

### 5.1 Objectif version

Étendre le périmètre du produit sans dégrader sa lisibilité ni sa traçabilité, et ajouter des assistances avancées lorsque le noyau métier est stable.

### 5.2 Axes d’évolution V3

- Ouverture à de nouvelles classes d’instruments prises en charge par l’architecture.
- Ajout d’assistance pédagogique enrichie, éventuellement appuyée par une IA strictement périphérique.
- Personnalisation progressive de la recommandation selon certains profils ou comportements observés, si cela reste explicable.
- Capacités d’exploration plus riches : comparaison multi-valeurs, bibliothèques de patterns, historique de cas similaires.
- Renforcement du pilotage produit par KPI d’usage et KPI qualité des signaux.

## 6. Règle d’usage de ce document

Ce document pilote la trajectoire de version.

Il ne remplace ni :
- la spécification produit détaillée pour le besoin fonctionnel ;
- le contract freeze pour les contrats métier et API ;
- la matrice écran canonique pour dériver les futurs backlogs API.

Il doit rester cohérent avec ces trois sources sans en dupliquer tous les détails.
