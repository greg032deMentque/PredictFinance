# Planning de versioning détaillé
## Application d’aide à l’investissement par analyse de patterns

Document de pilotage produit — feuille de route détaillée de la V1 à la V3, fondée sur le besoin métier validé.

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
| V1 | Créer le socle produit et la refonte architecturale cible. | Application exploitable pour actions françaises, analyse journalière à la demande, moteur déterministe, historisation complète. |
| V2 | Industrialiser le moteur et enrichir l’expérience d’usage. | Batchs nocturnes, enrichissement des patterns, vues de suivi, alertes et meilleures capacités d’analyse comparative. |
| V3 | Étendre le périmètre et augmenter l’intelligence produit de manière contrôlée. | Couverture multi-actifs/multi-marchés, assistance pédagogique enrichie, personnalisation progressive et performance analytique accrue. |

## 3. V1 — Refonte cœur produit

### 3.1 Objectif version

Livrer une première version fiable, explicable et ouverte, sans dépendre de l’IA, avec un découpage métier durable entre données de marché, détection de patterns, évaluation du risque, recommandation et historisation.

### 3.2 Objectifs détaillés

- Refondre l’architecture pour placer l’API comme source de vérité métier.
- Mettre en place un moteur de patterns extensible.
- Séparer strictement détection technique, évaluation du risque et recommandation contextualisée.
- Permettre la gestion de watchlist et portefeuille complet.
- Historiser chaque analyse sous forme de snapshot versionné.
- Rendre les résultats pédagogiques et compréhensibles pour un débutant.

### 3.3 Périmètre fonctionnel V1

| Domaine | Fonctionnalités V1 |
|---|---|
| Compte utilisateur | Inscription, authentification, espace personnel. |
| Watchlist | Ajout, suppression et consultation de valeurs suivies. |
| Portefeuille | Gestion de plusieurs lignes d’achat par actif avec quantité, prix, date, PRU, frais et devise. Reconstruction des lignes ouvertes après ventes selon une règle FIFO stricte définie pour la V1. |
| Analyse | Analyse journalière à la demande sur actions françaises prises en charge. |
| Patterns | Détection déterministe des patterns priorisés, pattern principal + patterns alternatifs compatibles. |
| Risque | Invalidation, stop loss, take profit, ratio risque/rendement, volatilité et drawdown potentiel selon disponibilité. |
| Restitution | Résumé pédagogique, justification explicite, recommandation contextualisée. |
| Historique | Conservation complète des analyses et de leurs évolutions. |

### 3.4 Périmètre technique V1

- Normalisation des données de marché dans l’API.
- Contrat de pattern modulaire : identifiant, prérequis, fenêtre d’analyse, détection, validation, invalidation, scoring.
- Mécanisme de version du moteur d’analyse.
- Persistance des snapshots d’analyse.
- API de restitution pour le front.
- Front orienté pédagogie et lisibilité des résultats.
- Service de reconstruction du contexte portefeuille compatible avec la règle FIFO V1 pour les lignes ouvertes après ventes.

### 3.5 Livrables de version V1

| Livrable | Description de fin de version |
|---|---|
| Socle applicatif refondu | Architecture claire, maintenable et ouverte aux extensions. |
| Moteur de patterns V1 | Premiers patterns implémentés avec règles déterministes et tests. |
| Portefeuille & watchlist | Fonctionnels, persistés et reliés aux analyses, avec contextualisation portefeuille fondée sur des lignes ouvertes reconstruites en FIFO. |
| Restitution pédagogique | Pages et API restituant clairement l’analyse et la recommandation. |
| Historique versionné | Consultation des anciennes analyses et base pour l’évaluation ex post. |
| Cadre qualité | Tests métier, règles de traçabilité et premiers critères de monitoring. |

### 3.6 Critères de sortie V1

- Le système fonctionne sans IA.
- L’ajout d’un nouveau pattern ne nécessite pas de refonte du noyau.
- Une analyse peut être rejouée et comprise a posteriori.
- Le front présente des conclusions adaptées à un débutant.
- Le portefeuille influence la recommandation sans modifier la vérité technique calculée.
- La reconstruction des lignes ouvertes après ventes est déterministe, traçable et conforme à la règle FIFO V1.

## 4. V2 — Industrialisation et enrichissement

### 4.1 Objectif version

Passer d’un produit fonctionnel à un produit plus industrialisé, plus automatisé et plus riche dans son suivi utilisateur.

### 4.2 Axes d’évolution V2

- Automatiser une partie des analyses via batch nocturne.
- Étendre le catalogue de patterns et améliorer la profondeur de restitution historique.
- Ajouter des vues de synthèse sur les analyses récentes, les signaux en cours et les signaux invalidés.
- Ajouter une logique d’alertes contextuelles sur événements significatifs.
- Structurer davantage la mesure ex post des performances de signaux.
- Préparer l’ouverture à d’autres instruments proches, notamment ETF et autres actions hors premier marché couvert.

### 4.3 Périmètre fonctionnel V2

| Domaine | Amélioration V2 |
|---|---|
| Analyses automatiques | Pré-calcul nocturne d’analyses consultables au réveil. |
| Tableaux de bord | Vue consolidée des valeurs suivies, derniers signaux, analyses les plus pertinentes. |
| Alertes | Signalement des changements d’état : validation, invalidation, proximité d’un niveau clé. |
| Historique comparatif | Visualisation de l’évolution des patterns, probabilités et recommandations dans le temps. |
| Mesure ex post | Premier tableau de bord interne de performance des signaux par horizon et par pattern. |
| Couverture | Montée en puissance progressive sur davantage de valeurs et premiers ETF si validés. |

### 4.4 Périmètre technique V2

- Orchestration batch nocturne robuste.
- Stratégie de cache et de recalcul maîtrisée.
- Modèle de scoring affiné sans casser la traçabilité.
- Services de notification et de planification.
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

### 5.3 Périmètre fonctionnel V3

| Domaine | Amélioration V3 |
|---|---|
| Couverture marché | Ajout d’autres marchés et d’autres instruments compatibles. |
| Pédagogie enrichie | Explications plus riches, comparaisons de scénarios, contenus d’accompagnement. |
| Personnalisation | Adaptation progressive de la restitution selon la situation ou le niveau utilisateur. |
| Exploration historique | Recherche de cas passés comparables et navigation approfondie dans l’historique. |
| Pilotage produit | KPI consolidés d’usage, d’efficacité pédagogique et de performance analytique. |

### 5.4 Périmètre technique V3

- Connecteurs supplémentaires de données de marché.
- Abstraction renforcée de la couche instrument / marché / pattern.
- Couches d’assistance avancée optionnelles sans dépendance critique pour la vérité métier.
- Capacités analytiques plus massives et outillage d’évaluation comparative.

## 6. Backlog structurant transversal

| Axe | V1 | V2 | V3 |
|---|---|---|---|
| Architecture ouverte | Socle modulaire | Extension simplifiée | Industrialisation multi-périmètres |
| Patterns | Catalogue initial | Catalogue enrichi | Catalogue avancé et outillé |
| Données marché | France actions | Couverture élargie | Multi-marchés / multi-actifs |
| Historisation | Snapshots versionnés | Comparaison enrichie | Exploration avancée |
| Décision utilisateur | Recommandation contextualisée | Alertes et suivi | Personnalisation maîtrisée |
| IA | Absente ou minimale | Assistance possible limitée | Assistance enrichie non critique |
| Portefeuille | Lignes d’achat multiples et reconstruction FIFO V1 des lignes ouvertes | Outillage portefeuille enrichi | Modèle portefeuille avancé et extensible |

## 7. Dépendances et prérequis par version

| Version | Pré requis métier / technique |
|---|---|
| V1 | Formaliser le premier lot de patterns, choisir les données de marché, figer le contrat d’analyse, le modèle de snapshot et la règle FIFO V1 de reconstruction des lignes ouvertes après ventes. |
| V2 | Disposer d’une V1 stable, de premiers signaux historisés et d’un protocole ex post déjà appliqué. |
| V3 | Disposer d’indicateurs fiables d’usage et de qualité pour guider l’expansion et éviter les enrichissements inutiles. |

## 8. Risques de roadmap

| Risque | Conséquence | Réponse recommandée |
|---|---|---|
| Ajouter trop tôt de l’IA | Opaque, difficile à auditer, risque de dette technique et métier. | Reporter l’IA après stabilisation du moteur déterministe. |
| Mélanger détection et recommandation | Logique métier confuse et difficile à tester. | Conserver une séparation stricte des couches. |
| Étendre trop vite la couverture marché | Complexité prématurée des données et de la UX. | Concentrer la V1 sur les actions françaises. |
| Ne pas versionner les analyses | Impossible de comparer les résultats et d’auditer les régressions. | Adopter les snapshots versionnés dès la V1. |
| Laisser implicite la reconstruction des lignes ouvertes | Incohérence métier, contextualisation non auditable, blocage d’implémentation. | Figer explicitement la règle FIFO V1 et l’appliquer de manière déterministe. |

## 9. Jalons de pilotage recommandés

| Jalon | Objectif |
|---|---|
| J1 | Spécification détaillée du besoin métier et du contrat d’analyse. |
| J2 | Architecture cible validée et découpage des modules V1. |
| J3 | Moteur de patterns V1 opérationnel sur les premiers patterns. |
| J4 | Portefeuille, watchlist et historisation intégrés. |
| J5 | Restitution front pédagogique complète et critères de sortie V1 validés. |
| J6 | Préparation du lot V2 : batch nocturne, alertes, vues de synthèse. |

## 10. Indicateurs de succès produit

| Catégorie | Indicateurs suggérés |
|---|---|
| Adoption | Nombre d’utilisateurs actifs, fréquence d’analyse, usage watchlist/portefeuille. |
| Compréhension | Taux de consultation des explications, récurrence d’usage après lecture d’une analyse. |
| Qualité signal | Taux de validation/invalidation, résultats à J+5 et J+20, drawdown observé. |
| Qualité produit | Temps moyen d’analyse, stabilité batch, taux d’erreurs, couverture de tests. |

## 11. Recommandation finale de pilotage

La V1 doit être considérée comme une version de fondation, pas comme une version de démonstration opportuniste.

La priorité absolue est de stabiliser un moteur métier explicable, versionné et extensible.

La V2 doit industrialiser et automatiser ce socle.

La V3 peut enrichir fortement le produit, à condition de ne jamais remettre en cause la traçabilité du noyau décisionnel.
