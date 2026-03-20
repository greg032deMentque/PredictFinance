# PredictFinance - IDEAS

## 1. Role du document

`IDEAS.md` sert de catalogue vivant des idees produit, des blocs fonctionnels possibles, des priorites et des dependances entre sujets.

Ce document ne remplace pas:

- `README.md` pour l'etat technique reel du repo
- `PRODUCT_ARCHITECTURE.md` pour la vision systeme et le decoupage fonctionnel/technique
- `AI_AGENT_WORKFLOW.md` pour la methode de travail attendue des agents IA

Ce document doit aider un agent a comprendre:

- ce qu'il faut faire d'abord
- ce qui est proche ou non de l'existant
- ce qui releve de l'IA et ce qui doit rester deterministe
- dans quel ordre les blocs doivent etre traites

Ancrage actuel:

- le MVP existe deja sur 3 briques: `FinanceFront`, `FinanceAPI`, `FinanceIA`
- le pattern reellement exploite aujourd'hui est `DOUBLE_TOP`
- le repo couvre deja watchlist, quotes, transactions, dashboard, analyse et simulation
- la cible produit est plus large que l'IA de patterns

## 2. Principes de priorisation

Regles de priorisation:

- stabiliser avant d'etendre
- securiser avant d'enrichir
- rapprocher d'abord le produit de l'existant reel
- traiter d'abord les evolutions a fort impact et faible risque
- differer les modules complexes tant que les contrats et parcours de base ne sont pas fiables
- eviter de lancer un chantier IA avance tant que le socle produit n'est pas propre
- distinguer systematiquement les blocs IA et non-IA

Principe structurant du produit:

- le domaine IA est specialise dans la detection et l'interpretation de patterns techniques
- le domaine non-IA couvre portefeuille, performance, exposition, risque, news, alertes et dashboard
- l'IA ne doit pas devenir le moteur principal de tout le produit

## 3. PRIORITE 0 - Stabilisation et securite

Cette priorite passe avant tout enrichissement fonctionnel.
Elle vise a fiabiliser ce qui existe deja dans le code et dans les parcours utilisateur.

### 3.1 Parcours client

#### Idea

Fiabiliser les parcours client existants de bout en bout.

Description:

- verifier login
- verifier refresh token
- verifier expiration et reconnexion
- verifier deconnexion
- verifier navigation client
- verifier appels API authentifies
- verifier gestion d'erreur cote front

Priority: 0
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- `AccountController`
- `AuthService`
- `StorageService`
- guards Angular
- interceptors front

AffectedAreas:

- `FinanceAPI/BackPredictFinance.API/Controllers/AccountController.cs`
- `FinanceFront/src/app/services/AuthService.service.ts`
- `FinanceFront/src/app/services/storage.service.ts`
- `FinanceFront/src/app/interceptor/*`
- `FinanceFront/src/app/guard/client.guard.ts`

ProductValue:

- rend le produit utilisable au quotidien
- evite les regressions sur le socle d'acces

TechnicalRisk:

- eleve si ignore
- moyen si traite par petites verifications guidees

RecommendedTiming:

- immediate

### 3.2 Parcours admin

#### Idea

Fiabiliser la separation admin/client et les parcours admin existants.

Description:

- verifier login admin
- verifier controle des roles
- verifier guards Angular admin
- verifier endpoints admin securises
- verifier separation admin/client dans les redirections

Priority: 0
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- policies d'autorisation API
- `AdminGuard`
- `ClientGuard`
- pages admin existantes

AffectedAreas:

- `FinanceAPI/BackPredictFinance.API/Controllers/*`
- `FinanceFront/src/app/guard/admin.guard.ts`
- `FinanceFront/src/app/guard/client.guard.ts`
- `FinanceFront/src/app/Routes/app.routes.admin.ts`

ProductValue:

- securise les fonctions d'administration
- reduit le risque de confusion entre espaces

TechnicalRisk:

- eleve en cas de faille role/route

RecommendedTiming:

- immediate

### 3.3 Flux metier existants

#### Idea

Verifier et fiabiliser les flux deja presents dans le MVP.

Description:

- analyse d'actif
- simulation
- communication API <-> Python
- coherence des payloads
- coherence des reponses
- absence d'erreurs silencieuses
- verification du fallback mono-pattern actuel

Priority: 0
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: Yes
DependsOn:

- contrats actuels
- `PythonApiService`
- `ClientFinanceService`
- composants client/admin existants

AffectedAreas:

- `FinanceAPI/BackPredictFinance.Services/PythonServices/PythonApiService.cs`
- `FinanceAPI/BackPredictFinance.Services/ClientFinanceServices/ClientFinanceService.cs`
- `FinanceFront/src/app/services/client-finance.service.ts`
- `FinanceIA/src/finance_ia/cli/*`

ProductValue:

- stabilise la base avant toute extension

TechnicalRisk:

- eleve si les contrats restent instables

RecommendedTiming:

- immediate

### 3.4 Securite

#### Idea

Durcir la securite reelle du socle.

Description:

- verifier JWT
- verifier refresh tokens
- verifier logout
- verifier roles
- restreindre CORS
- fiabiliser le rate limiting
- renforcer la validation serveur
- verifier la gestion des erreurs
- eviter l'exposition de donnees sensibles

Priority: 0
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- pipeline API
- auth API
- guards et interceptors front

AffectedAreas:

- `FinanceAPI/BackPredictFinance.API/Program.cs`
- `FinanceAPI/BackPredictFinance.API/Middleware/*`
- `FinanceAPI/BackPredictFinance.API/Controllers/AccountController.cs`
- `FinanceFront/src/app/interceptor/*`
- `FinanceFront/src/app/guard/*`

ProductValue:

- protege tout le produit, quel que soit le futur fonctionnel

TechnicalRisk:

- tres eleve si reporte

RecommendedTiming:

- immediate

## 4. PRIORITE 1 - Evolutions proches de l'existant

Bloc cible:

- evolutions de faible a moyenne complexite, proches du code deja en place

### Idea

Watchlist amelioree.

Description:

- tags personnalises
- notes utilisateur
- objectifs de prix
- horizon d'investissement

Priority: 1
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- watchlist existante
- detail actif plus riche

AffectedAreas:

- front client
- DTO watchlist
- persistance user assets

ProductValue:

- forte valeur quotidienne sans chantier lourd

TechnicalRisk:

- faible a moyen

RecommendedTiming:

- court terme

### Idea

Dashboard plus utile sans changer de perimetre produit.

Description:

- resume portefeuille plus lisible
- top gainers/losers
- alertes importantes
- dernieres analyses
- patterns detectes recemment

Priority: 1
Complexity: Low
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- dashboard existant
- meilleurs flux d'historique

AffectedAreas:

- dashboard client
- aggregations backend

ProductValue:

- visibilite immediate des donnees deja disponibles

TechnicalRisk:

- faible

RecommendedTiming:

- court terme

### Idea

Nettoyage des doublons et des points d'entree concurrents restants.

Description:

- continuer a converger vers un seul flux officiel par usage
- reduire les chemins redondants front/backend
- clarifier les DTO officiels

Priority: 1
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- audit des flux existants

AffectedAreas:

- services Angular
- services applicatifs .NET
- documentation

ProductValue:

- diminue le risque de regression pour toutes les etapes suivantes

TechnicalRisk:

- moyen

RecommendedTiming:

- court terme

## 5. PRIORITE 2 - Fonctions non-IA a forte valeur

Ces modules apportent de la valeur produit sans etendre le moteur IA.

### Portfolio Management

#### Idea

Etendre la gestion portefeuille a partir des transactions deja presentes.

Description:

- positions
- transactions
- PRU
- plus-values / moins-values
- cash
- valeur portefeuille

Priority: 2
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- transactions existantes
- dashboard existant
- calculs portefeuille deterministes

AffectedAreas:

- entites finance
- services client finance
- dashboard et pages portefeuille

ProductValue:

- valeur produit forte hors IA

TechnicalRisk:

- moyen

RecommendedTiming:

- apres stabilisation du socle

### Performance Analytics

#### Idea

Ajouter une lecture structuree des performances.

Description:

- performance globale
- performance par ligne
- performance par secteur
- comparaison benchmark

Priority: 2
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- portefeuille plus fiable
- donnees historiques suffisantes

AffectedAreas:

- backend analytics
- dashboard
- pages portefeuille

ProductValue:

- forte comprehension pour l'utilisateur

TechnicalRisk:

- moyen

RecommendedTiming:

- moyen terme proche

### Exposure Analytics

#### Idea

Mesurer la structure du portefeuille.

Description:

- exposition secteur
- geographie
- devise
- concentration
- poids des plus grosses lignes

Priority: 2
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- portefeuille consolide
- metadonnees asset plus riches

AffectedAreas:

- entites asset
- services portefeuille
- dashboard risque/exposition

ProductValue:

- tres forte valeur d'aide a la decision

TechnicalRisk:

- moyen

RecommendedTiming:

- moyen terme

### Risk Analytics

#### Idea

Ajouter des calculs de risque deterministes.

Description:

- volatilite
- drawdown
- beta
- score de risque
- stress tests simples

Priority: 2
Complexity: High
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- portefeuille consolide
- historiques fiables
- benchmark et metadonnees marche

AffectedAreas:

- backend analytics
- pages portefeuille/risque
- dashboard

ProductValue:

- forte valeur produit pour les utilisateurs reguliers

TechnicalRisk:

- moyen a eleve selon les choix de calcul

RecommendedTiming:

- moyen terme

## 6. PRIORITE 3 - Services marche et engagement

### Market Data

#### Idea

Faire evoluer le produit en vrai outil d'observation du marche.

Description:

- cours
- historique
- indices
- top movers
- tendances du marche

Priority: 3
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: No
DependsOn:

- recherche/quotes existantes
- services de donnees plus riches

AffectedAreas:

- services data marche
- pages front marche/detail actif

ProductValue:

- augmente la frequence d'usage du produit

TechnicalRisk:

- moyen

RecommendedTiming:

- apres portefeuille/performance de base

### News

#### Idea

Ajouter un domaine news relie aux actifs et au portefeuille.

Description:

- news marche
- news par actif
- news sectorielles
- news macro

Priority: 3
Complexity: Medium
NearCurrentCode: No
RequiresAI: No
DependsOn:

- choix d'une source de news
- modele de rattachement aux actifs/utilisateurs

AffectedAreas:

- nouveaux services backend
- nouvelles pages front
- dashboard et watchlist

ProductValue:

- forte valeur d'observation

TechnicalRisk:

- moyen

RecommendedTiming:

- moyen terme

### Events & Calendar

#### Idea

Ajouter les evenements financiers structurants.

Description:

- resultats entreprises
- evenements macro
- dividendes
- dates importantes

Priority: 3
Complexity: Medium
NearCurrentCode: No
RequiresAI: No
DependsOn:

- source de donnees calendrier/news

AffectedAreas:

- dashboard
- pages detail actif
- alertes

ProductValue:

- forte utilite contextuelle

TechnicalRisk:

- moyen

RecommendedTiming:

- moyen terme

### Alerts

#### Idea

Construire un moteur d'alertes utile et sobre.

Description:

- seuils de prix
- variations importantes
- news pertinentes
- evenements proches
- franchissements techniques

Priority: 3
Complexity: High
NearCurrentCode: No
RequiresAI: No
DependsOn:

- marche
- news/events
- watchlist enrichie

AffectedAreas:

- backend alerting
- dashboard
- notifications
- watchlist

ProductValue:

- augmente l'engagement quotidien

TechnicalRisk:

- moyen a eleve si bruit ou mauvais ciblage

RecommendedTiming:

- moyen terme

## 7. PRIORITE 4 - Extensions IA

Ces sujets ne doivent arriver qu'apres stabilisation du socle et progression des modules non-IA les plus utiles.

### Multi-pattern

#### Idea

Faire passer la simulation d'un mode mono-pattern a un vrai mode multi-pattern.

Description:

- plusieurs patterns
- classement des candidats
- selection utilisateur

Priority: 4
Complexity: High
NearCurrentCode: Yes
RequiresAI: Yes
DependsOn:

- contrats stabilises
- catalogue de patterns
- persistance fiable des analyses
- parcours UI clarifies

AffectedAreas:

- `FinanceIA`
- bridge .NET / Python
- DTO backend
- front analyse/simulation

ProductValue:

- extension IA majeure

TechnicalRisk:

- eleve

RecommendedTiming:

- apres priorites 0 a 3 les plus critiques

### Detailed Pattern Analysis

#### Idea

Approfondir l'analyse d'un pattern choisi.

Description:

- phase
- niveaux techniques
- recommandations enrichies
- ambiguite
- signaux detailles

Priority: 4
Complexity: Medium
NearCurrentCode: Yes
RequiresAI: Yes
DependsOn:

- multi-pattern ou au moins contrat pattern-selection stable
- payloads Python riches

AffectedAreas:

- `FinanceIA/src/finance_ia/patterns/*`
- DTO analyse
- composants analyse detaillee

ProductValue:

- forte valeur metier sur la partie interpretation

TechnicalRisk:

- moyen a eleve

RecommendedTiming:

- apres stabilisation des contrats d'analyse

### Explainability

#### Idea

Expliquer le score et la confiance du moteur IA.

Description:

- explication du score
- facteurs de decision
- confiance

Priority: 4
Complexity: High
NearCurrentCode: No
RequiresAI: Yes
DependsOn:

- multi-pattern ou patterns enrichis
- format de sortie Python versionne

AffectedAreas:

- moteur Python
- DTO backend
- restitution front

ProductValue:

- augmente la confiance utilisateur

TechnicalRisk:

- eleve si explique mal ou de facon trompeuse

RecommendedTiming:

- tardif par rapport au reste

## 8. Metadonnees de lecture rapide

Interpretation rapide des champs:

- `Priority`: 0 a 4, 0 etant le plus urgent
- `Complexity`: Low / Medium / High
- `NearCurrentCode`: `Yes` ou `No`
- `RequiresAI`: `Yes` ou `No`
- `DependsOn`: prerequis fonctionnels ou techniques
- `AffectedAreas`: zones du repo ou surfaces produit impactees
- `ProductValue`: valeur pour l'utilisateur ou pour le socle produit
- `TechnicalRisk`: niveau de risque si le sujet est mal traite
- `RecommendedTiming`: moment logique de traitement

Lecture recommandee:

- commencer par les idees `Priority: 0`
- privilegier ensuite les sujets `NearCurrentCode: Yes` et `Complexity: Low/Medium`
- repousser les sujets `RequiresAI: Yes` si le socle auth, parcours et contrats n'est pas encore propre

## 9. Regroupement en blocs d'execution progressifs

### Bloc A - Stabilisation des parcours

Objectif:

- verifier et fiabiliser les parcours client/admin et les flux metier existants

Pourquoi ici:

- condition de base avant toute extension

Dependances:

- aucune, c'est le premier bloc

Niveau de difficulte:

- moyen

Impact produit:

- critique

### Bloc B - Securite et fiabilisation auth

Objectif:

- durcir auth, roles, CORS, rate limiting, erreurs et validation

Pourquoi ici:

- protege le socle avant augmentation du perimetre fonctionnel

Dependances:

- lecture claire des parcours reels

Niveau de difficulte:

- moyen

Impact produit:

- critique

### Bloc C - Ameliorations proches du MVP

Objectif:

- mieux exploiter l'existant sans ajouter de grosses briques

Pourquoi ici:

- fort impact utilisateur, faible risque relatif

Dependances:

- blocs A et B

Niveau de difficulte:

- faible a moyen

Impact produit:

- eleve

### Bloc D - Portfolio non-IA

Objectif:

- faire du portefeuille un vrai domaine produit utile

Pourquoi ici:

- forte valeur sans dependre de l'IA

Dependances:

- stabilisation du socle
- transactions fiables

Niveau de difficulte:

- moyen

Impact produit:

- eleve

### Bloc E - Exposition et risque

Objectif:

- rendre le portefeuille intelligible et mesurable

Pourquoi ici:

- suite logique du domaine portefeuille

Dependances:

- bloc D

Niveau de difficulte:

- moyen a eleve

Impact produit:

- eleve

### Bloc F - News et alertes

Objectif:

- augmenter la frequence d'usage et la pertinence quotidienne

Pourquoi ici:

- plus utile une fois marche, watchlist et portefeuille plus riches

Dependances:

- blocs C, D et partiellement E

Niveau de difficulte:

- moyen a eleve

Impact produit:

- eleve

### Bloc G - Extensions IA

Objectif:

- faire evoluer le moteur pattern du mono-pattern vers une vraie interpretation multi-pattern

Pourquoi ici:

- volontairement tardif pour ne pas construire sur un socle instable

Dependances:

- blocs A et B obligatoires
- contrats stables
- persistance fiable
- restitution front mieux structuree

Niveau de difficulte:

- eleve

Impact produit:

- eleve, mais pas prioritaire sur la stabilite globale
