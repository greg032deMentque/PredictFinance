# Spécification produit détaillée
## Application d’aide à l’investissement par analyse de patterns

Version de cadrage produit consolidée, alignée sur le besoin métier, le contrat V1 gelé et la dernière spec visuelle fournie.

- Produit cible : application pédagogique d’aide à l’investissement pour particulier débutant.
- Portée V1 : actions françaises, analyse journalière, moteur déterministe explicable, sans IA obligatoire.
- Principe directeur : l’API est la source de vérité métier ; l’IA reste facultative et périphérique.
- Objectif de conception : architecture ouverte pour ajouter facilement de nouveaux actifs, marchés, patterns et règles.
- Règle d’expérience V1 : avant connexion, aucun shell applicatif n’est visible ; après connexion, l’utilisateur entre dans un espace cohérent selon son rôle.

## 1. Résumé exécutif

Le produit vise à aider un investisseur particulier débutant à suivre son portefeuille et sa watchlist, à comprendre l’état technique d’une valeur, et à recevoir une aide pédagogique à la décision d’achat, d’attente, de conservation, d’allègement ou de vente.

Le produit ne passe aucun ordre et n’accède pas aux comptes bancaires. Il fournit des analyses, des recommandations pédagogiques et des notifications contextuelles basées sur des règles déterministes auditées.

Le cœur du système est un moteur d’analyse versionné, explicable et historisé. Chaque analyse doit pouvoir être rejouée, comprise et comparée dans le temps.

## 2. Vision produit

Transformer des données de marché complexes en conclusions compréhensibles, pédagogiques et traçables pour un utilisateur non expert.

Permettre à l’utilisateur de suivre les valeurs qui l’intéressent, de contextualiser les signaux au regard de son portefeuille, et de capitaliser sur un historique d’analyses pour apprendre et progresser.

Construire un socle technique durable, extensible et maintenable, capable d’évoluer sans refonte majeure lors de l’ajout de nouvelles familles d’actifs, de nouveaux marchés ou de nouveaux patterns.

## 3. Problème métier adressé

Un particulier débutant manque souvent de méthode pour lire un graphique, identifier des configurations techniques crédibles et relier ces observations à une décision d’investissement prudente.

Les informations disponibles sur le marché sont abondantes, hétérogènes et souvent difficiles à interpréter sans expérience. Le produit doit réduire cette complexité sans promettre de résultat financier.

Le besoin n’est pas d’automatiser le trading mais de fournir un cadre d’analyse pédagogique, cohérent et traçable.

## 4. Positionnement produit

Outil d’aide pédagogique à l’investissement.

Outil d’analyse technique centrée patterns.

Outil de recommandation pédagogique et contextualisée.

Le produit n’est pas un robot d’exécution ni un système de trading haute fréquence.

## 5. Parties prenantes et personas

### 5.1 Persona principal

| Champ | Spécification |
|---|---|
| Type d’utilisateur | Particulier débutant |
| Motivation | Comprendre quand surveiller, acheter, attendre, conserver ou vendre une valeur sans devoir maîtriser seul l’analyse technique. |
| Niveau d’expertise | Faible à intermédiaire en lecture de graphique et gestion du risque. |
| Attentes clés | Clarté, pédagogie, justification explicite, conservation de l’historique, simplicité de navigation. |
| Tolérance au risque | Variable, mais non personnalisée en V1 ; les recommandations sont d’abord issues de l’analyse marché. |

### 5.2 Parties prenantes secondaires

- Équipe produit : définit les règles métier, les priorités de version et les garde-fous de communication.
- Équipe technique : implémente le moteur d’analyse, l’historisation, le front et les interfaces d’administration.
- Équipe contenu / métier : formalise les patterns à partir de sources de référence et rédige les descriptions pédagogiques.

## 6. Objectifs produit

| Catégorie | Objectif détaillé |
|---|---|
| Compréhension | Rendre l’analyse technique lisible par un débutant sans simplifier à l’excès ni masquer les incertitudes. |
| Décision | Fournir une recommandation pédagogique contextualisée, séparée de la détection technique et de l’évaluation du risque. |
| Traçabilité | Conserver chaque analyse, ses hypothèses, ses scores, sa recommandation et sa performance ex post. |
| Extensibilité | Permettre l’ajout de nouveaux patterns, nouveaux actifs et nouveaux marchés sans refonte du noyau. |
| Confiance | Assurer l’explicabilité de tous les résultats présentés et rendre auditable la chaîne de calcul. |
| Orientation | Donner à l’utilisateur un point d’entrée clair, des notifications utiles et une aide contextuelle sans multiplier les parcours concurrents. |

## 7. Espaces produit V1

| Espace | Finalité | Règles produit |
|---|---|---|
| Anonymous | Authentifier ou récupérer l’accès | Aucun shell métier visible avant connexion. |
| User | Utiliser le produit au quotidien | Accès aux analyses, watchlist, portefeuille, historique, apprentissage, compte, notifications et aide. |
| Admin | Gouverner le produit et les utilisateurs | Espace distinct de l’espace user ; réservé à la gestion et à la gouvernance. |

## 8. Périmètre fonctionnel V1

### 8.1 Inclus en cible V1

- Création de compte, authentification et espace utilisateur.
- Récupération d’accès avec parcours « mot de passe oublié » puis « réinitialisation ».
- Gestion d’une watchlist de valeurs suivies.
- Gestion d’un portefeuille avec plusieurs lignes d’achat par actif.
- Analyse journalière à la demande dans un espace dédié.
- Historisation complète des analyses.
- Détection déterministe de patterns extensibles.
- Évaluation du risque : invalidation, stop loss, take profit, ratio risque/rendement, volatilité, drawdown potentiel si applicable.
- Affichage du pattern principal et des patterns alternatifs compatibles.
- Résumé pédagogique expliquant le résultat.
- Centre de notifications pour prioriser les éléments utiles et router vers les bonnes pages.
- Centre d’aide contextuel pour expliquer les résultats, les snapshots et les limites produit.
- Compte utilisateur segmenté en profil, préférences, notifications et sécurité.
- Espace admin distinct couvrant au minimum la vue d’ensemble, les utilisateurs et les registres / dictionnaires de gouvernance visibles dans la spec écran.

### 8.2 Hors scope explicite V1

- Passage d’ordres.
- Accès direct aux comptes bancaires.
- Trading temps réel tick par tick.
- Fiscalité et gestion détaillée PEA/CTO.
- Personnalisation avancée par profil investisseur.
- IA générative comme source primaire de vérité.
- Messagerie temps réel ou centre d’aide conversationnel.
- SSO, MFA imposée, fédération d’identité ou gestion d’organisation multi-tenant explicite.

## 9. Couverture marché et actifs

| Dimension | Décision produit |
|---|---|
| Actifs V1 | Actions françaises |
| Vision long terme | Architecture compatible avec l’ajout d’ETF en V2, puis d’autres actions et autres familles d’actifs dans des contrats ultérieurs. |
| Granularité V1 | Journalière |
| Élargissement futur | Ajout de nouveaux marchés et nouvelles devises sans casser le modèle métier central. |

## 10. Modèle métier central

- Surveiller un portefeuille et une watchlist.
- Analyser une valeur sur données journalières.
- Détecter plusieurs patterns compatibles si le marché le justifie.
- Séparer trois couches métier : détection technique, évaluation du risque, recommandation pédagogique.
- Conserver une trace versionnée de chaque analyse et mesurer sa performance ex post.
- Compléter l’analyse par des surfaces d’orientation non concurrentes :
  - `Notifications` priorise et route ;
  - `Help center` explique ;
  - `Account` configure ;
  - `Admin` gouverne.

## 11. Règles métier structurantes

| Règle | Description détaillée |
|---|---|
| R1 | L’API porte la vérité métier : données de marché normalisées, détection, scoring, risque, historique et traçabilité. |
| R2 | L’IA n’est pas nécessaire en V1 ; si elle est ajoutée plus tard, elle reste une couche d’assistance pédagogique et interprétative. |
| R3 | Chaque pattern déclare son propre contrat : prérequis, fenêtre d’analyse minimale, règles de détection, validation, invalidation et scoring. |
| R4 | Si aucun pattern n’est fiable, le système doit l’indiquer explicitement au lieu de forcer un faux 100 %. |
| R5 | Si plusieurs patterns sont compatibles, ils sont tous affichés avec leurs informations et l’explication de cette coexistence. |
| R6 | La recommandation dépend de l’analyse marché et du contexte de détention utilisateur. |
| R7 | Tous les résultats affichés doivent être explicables, auditables et historisés. |
| R8 | En V1, la reconstruction des lignes d’achat ouvertes après ventes suit une règle FIFO stricte pour la contextualisation portefeuille. |
| R9 | Avant authentification, aucun shell produit ne doit exposer la navigation user ou admin. |
| R10 | Après authentification, l’utilisateur est routé vers un espace cohérent unique selon son rôle. |
| R11 | Le centre d’aide n’est pas un second historique ni un centre de notifications ; il sert à expliquer et orienter. |
| R12 | Le centre de notifications n’est pas un paramétrage de compte ; il sert à prioriser et router. |

## 12. Contrat de sortie d’une analyse

Chaque analyse doit fournir au minimum les éléments suivants.

| Bloc | Contenu attendu |
|---|---|
| Identification | Actif, marché, date/heure d’analyse, version du moteur, fenêtre réellement utilisée. |
| AnalysisOutcome | Résultat métier de premier rang, couvrant à la fois les analyses exécutables et les issues métier non exécutables. Les erreurs techniques restent séparées au niveau HTTP / API. |
| Patterns | Pattern principal d’affichage, patterns alternatifs compatibles, niveau de confiance par pattern, justification. |
| État | État d’avancement du pattern dans un vocabulaire lisible par un débutant, dérivé de la taxonomie canonique V1. |
| Recommandation | Verbe de recommandation autorisé selon le contexte de détention utilisateur. |
| Risque | Niveau d’invalidation, stop loss suggéré, take profit suggéré, ratio risque/rendement, volatilité, drawdown potentiel, selon disponibilité. |
| Explication | Résumé pédagogique clair, sans jargon non expliqué, explicitant le raisonnement. |
| Contexte portefeuille | Présence ou non d’une position, synthèse des lignes détenues utiles à la contextualisation. |

### 12.1 Statut du résultat d’analyse

La V1 distingue explicitement :
- les issues métier exécutables avec un résultat d’analyse normal ;
- les issues métier non exécutables avec un code et une explication produits ;
- les erreurs techniques, qui ne font pas partie de la taxonomie métier visible.

Une analyse non exécutable n’est donc pas une erreur silencieuse ; c’est un état métier de premier rang.

## 13. États recommandés pour l’état d’avancement du pattern

Pour rester compréhensible par un débutant, l’état d’avancement doit utiliser la taxonomie canonique V1 ci-dessous.

| Code canonique | Libellé UX FR recommandé | Signification produit |
|---|---|---|
| `FORMING` | En formation | Des éléments précurseurs existent et la configuration commence à se structurer. |
| `MONITORING` | Sous surveillance | Le pattern est crédible et doit être suivi, mais il n’est pas encore confirmé. |
| `CONFIRMED` | Confirmé | Les critères de validation définis par le pattern sont atteints. |
| `INVALIDATED` | Invalidé | Les conditions rendent le pattern non exploitable ou contredisent sa lecture. |
| `COMPLETED` | Scénario réalisé | Le scénario pédagogique suivi par le pattern a atteint sa conclusion attendue ou sa phase terminale de suivi. |

Règle de réconciliation :
- « pattern envisagé » et « proche de validation » sont des formulations pédagogiques possibles à l’intérieur des explications ;
- elles ne doivent pas être utilisées comme statuts canoniques distincts ;
- tout écran, snapshot ou API V1 doit exposer l’un des cinq codes canoniques ci-dessus.

## 14. Inventaire canonique des écrans V1

### 14.1 Anonymous

- `login`
- `forgot-password`
- `reset-password`

### 14.2 User

- `user-home`
- `watchlist`
- `portfolio`
- `analysis-entry`
- `analysis-result`
- `instrument-detail`
- `parameter-detail`
- `simulation`
- `history`
- `snapshot-comparison`
- `learn`
- `account`
- `account-security`
- `notifications`
- `onboarding-empty`
- `help-center`

### 14.3 Admin

- `admin-overview`
- `admin-users`
- `admin-instrument-registry`
- `admin-pea-registry`
- `admin-scoring-policy`
- `admin-parameter-dictionary`
- `admin-wording-versions`
- `admin-snapshot-audit`
- `admin-data-quality`

## 15. Principes UX verrouillés

- Avant login, l’utilisateur ne voit pas la navigation métier.
- Après login, un seul espace cohérent est visible.
- La home user sert de centre de décision quotidien.
- La watchlist sert aux instruments non détenus à surveiller.
- Le portefeuille sert aux positions détenues et à leur lecture décisionnelle.
- Le résultat d’analyse reste la page de vérité de la lecture consolidée.
- `Help center` répond aux doutes fréquents et route vers la bonne page explicative.
- `Notifications` rend visibles les priorités et route vers l’action utile.
- `Account` distingue explicitement profil, préférences, notifications et sécurité.
- L’espace admin reste séparé des surfaces d’usage quotidien user.

## 16. Critères de réussite documentaires pour la prochaine phase backlog API

La présente documentation est considérée comme suffisamment verrouillée pour dériver les prochains backlogs API si, et seulement si :
- chaque écran produit canonique peut être relié à un besoin API sans ambiguïté ;
- les écrans d’orientation (`notifications`, `help-center`, `account`) ont un rôle distinct et non chevauchant ;
- les parcours `login`, `forgot-password` et `reset-password` sont explicitement présents en V1 ;
- la séparation `anonymous / user / admin` est explicite ;
- aucune annexe d’implémentation front n’est nécessaire pour comprendre le besoin produit ;
- les éléments hors scope V1 ne sont pas présentés comme acquis.

## 17. Conclusion

La V1 vise un produit pédagogique, explicable et maintenable, centré sur l’analyse déterministe d’actions françaises. La spec produit inclut désormais le cœur métier, les surfaces d’orientation et les espaces d’accès réellement visibles dans la version UX récente, sans mélanger besoin produit, annexe d’implémentation et roadmap future.
