# PredictFinance - Product Architecture

## 1. Role du document

`PRODUCT_ARCHITECTURE.md` fait le lien entre:

- l'architecture technique reelle du repo
- les domaines fonctionnels du produit
- la trajectoire d'evolution attendue

Ce document ne remplace pas:

- `README.md` pour le constat technique detaille et les fichiers cles
- `IDEAS.md` pour la priorisation produit et le catalogue d'idees
- `AI_AGENT_WORKFLOW.md` pour la methode d'intervention des agents

## 2. Vision globale du produit

PredictFinance doit etre lu comme un produit organise en trois niveaux.

### Observer

Le niveau Observer couvre ce qui aide l'utilisateur a voir l'etat du marche et de ses actifs:

- marche
- cours
- watchlist
- news
- alertes

### Comprendre

Le niveau Comprendre couvre ce qui aide l'utilisateur a comprendre sa situation financiere:

- portefeuille
- performance
- exposition
- risque

### Interpreter

Le niveau Interpreter couvre l'analyse technique et la lecture des signaux:

- patterns
- signaux techniques
- conseil metier derive

Point important:

- le niveau Interpreter n'absorbe pas les deux autres
- l'IA reste specialisee sur les patterns techniques
- le portefeuille, la performance, l'exposition et le risque doivent rester des domaines non-IA

## 3. Domaines fonctionnels

### Market Data

Role:

- alimenter le produit en donnees de marche utilisables

Valeur produit:

- rend l'application utile meme sans analyse IA
- supporte watchlist, detail actif, dashboard et future lecture portefeuille

Dependances:

- fournisseurs de donnees de marche
- services quotes/historique

Nature:

- non-IA

Ancrage existant:

- quotes live et recherche d'actifs existent deja

### Portfolio Analytics

Role:

- calculer positions, performance, exposition et risque

Valeur produit:

- apporte une valeur durable hors patterns
- permet de comprendre le portefeuille sans dependre du moteur IA

Dependances:

- transactions
- asset metadata
- historiques de prix

Nature:

- non-IA

Ancrage existant:

- transactions, dashboard et premieres aggregations existent deja

### News & Alerts

Role:

- contextualiser le marche et notifier l'utilisateur des evenements utiles

Valeur produit:

- augmente l'engagement quotidien
- relie marche, watchlist et portefeuille

Dependances:

- donnees news
- evenements financiers
- modele d'alertes

Nature:

- non-IA par defaut

Ancrage existant:

- peu present aujourd'hui, plutot futur domaine produit

### Pattern Intelligence

Role:

- detecter, scorer et interpreter des patterns techniques

Valeur produit:

- constitue le domaine IA specialise du produit
- fournit detection, probabilite, phase et contexte technique

Dependances:

- donnees de marche
- bridge API <-> Python
- catalogue de patterns
- contrats de sortie stables

Nature:

- IA

Ancrage existant:

- `DOUBLE_TOP` est operationnel
- le multi-pattern est partiellement prepare mais pas encore produit

## 4. Architecture technique associee

### Angular frontend

Responsabilites:

- saisie utilisateur
- navigation client/admin
- affichage des resultats
- traduction UX des enums/codes en libelles, badges et icones
- orchestration d'ecrans
- consommation des endpoints backend

Ce que le front ne doit pas faire:

- porter la logique metier critique
- recalculer des decisions IA
- afficher directement des libelles backend pour les domaines fermes
- reimplementer des regles de portefeuille ou de risque complexes

Ancrage reel:

- `FinanceFront/src/app/app.routes.ts`
- `FinanceFront/src/app/services/client-finance.service.ts`
- guards et interceptors front

### .NET backend

Responsabilites:

- authentification et autorisation
- validation serveur
- orchestration metier
- derivation du conseil utilisateur (`Buy` / `Sell` / `Hold`) a partir de l'analyse IA
- persistance
- exposition des DTO
- pont vers le moteur Python

Ce que le backend doit faire:

- rester la couche de controle et de stabilisation
- securiser les parcours
- historiser les analyses
- garder la cohesion des contrats

Ancrage reel:

- `FinanceAPI/BackPredictFinance.API/Program.cs`
- `FinanceAPI/BackPredictFinance.API/Controllers/*`
- `FinanceAPI/BackPredictFinance.Services/*`

### Python IA engine

Responsabilites:

- acquisition de donnees de marche pour l'analyse
- feature engineering
- scoring pattern
- phase detection
- generation des sorties techniques probabilistes

Ce que Python ne doit pas faire:

- gerer auth, roles, portefeuille, exposition ou dashboard
- produire le conseil final utilisateur `Buy` / `Sell` / `Hold`
- devenir un service metier global du produit

Ancrage reel:

- `FinanceIA/main.py`
- `FinanceIA/src/finance_ia/patterns/*`
- `FinanceIA/src/finance_ia/model/*`

## 5. Flux structurants

### Authentification

Flux existant:

- login, login admin, refresh et logout passent par `AccountController`
- le front utilise guards et stockage token

Flux cible:

- parcours auth verifies, plus robustes, mieux testes, plus uniformes dans les erreurs

### Suivi d'un actif

Flux existant:

- recherche d'actif
- consultation de quote
- ajout watchlist

Flux cible:

- detail actif enrichi avec contexte marche, watchlist, analyse et futurs evenements/news

### Simulation pattern

Flux existant:

- client ou admin lance une analyse/simulation
- l'API orchestre le moteur Python
- le pattern reel reste `DOUBLE_TOP`

Flux cible:

- simulation globale de candidats
- selection explicite d'un pattern
- analyse detaillee du pattern choisi

### Parcours admin

Flux existant:

- gestion utilisateurs
- lancement d'analyse sur valeur

Flux cible:

- supervision IA
- consultation historique des runs
- rejeu admin si pertinent

### Portefeuille

Flux existant:

- transactions et agregations simples

Flux cible:

- portefeuille complet avec positions, PRU, P/L, performance et benchmark

### Exposition

Flux existant:

- encore peu explicite dans le produit

Flux cible:

- exposition secteur, geographie, devise, concentration

### News / alertes

Flux existant:

- quasi absent

Flux cible:

- domaine propre relie a watchlist, portefeuille et detail actif

## 6. Trajectoire d'evolution

### MVP actuel

- auth et roles presents
- parcours client/admin de base presents
- watchlist, quotes, transactions, dashboard presents
- analyse et simulation IA presentes
- `DOUBLE_TOP` operationnel

### Stabilisation

- verification des parcours client/admin
- verification auth/refresh/logout/guards
- securisation CORS, rate limiting, validation, erreurs
- stabilisation des contrats API <-> Python
- normalisation des erreurs Python en JSON inter-process et historisation des echecs d'analyse dans `AnalysisRun`
- verrouillage des enums/codes entre API et front, avec affichage UX centralise cote Angular
- alignement OWASP et SonarQube sur les flux IA/front/API

### Enrichissements non-IA

- portefeuille
- performance
- exposition
- risque

### Services marche

- marche
- news
- calendrier
- alertes

### Multi-pattern IA

- catalogue de patterns expose
- multi-pattern reel
- selection utilisateur
- analyse detaillee

### Fonctionnalites avancees

- explainability IA
- alertes combinees
- supervision admin plus riche

## 7. Regles de coherence systeme

- ne pas melanger logique portefeuille et logique IA
- ne pas faire dependre tout le produit du moteur IA
- garder l'IA specialisee dans les patterns
- faire porter le conseil metier par l'API, pas par Python
- transporter des codes/enums stables cote backend et mapper les libelles cote front
- faire evoluer les contrats progressivement
- preserver la stabilite du socle avant toute extension ambitieuse
- ne pas introduire de refonte globale non justifiee
- privilegier les evolutions proches du code existant avant les chantiers plus lointains

## 8. Relations entre documents

`README.md`

- source de verite technique
- architecture reelle
- flux reels
- blocages et limites constates

`PRODUCT_ARCHITECTURE.md`

- vue systeme produit
- relation entre domaines fonctionnels et couches techniques
- trajectoire globale

`IDEAS.md`

- priorites produit
- idees par blocs
- dependances et ordre de traitement

`AI_AGENT_WORKFLOW.md`

- mode operatoire des agents IA
- ordre de lecture
- methode d'intervention
- checklists et anti-patterns
