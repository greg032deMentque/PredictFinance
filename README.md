# PredictFinance - README de contexte projet

Ce document est la reference de contexte projet pour les futures sessions Codex.
Il doit etre lu avant toute modification structurante.

Documents complementaires:

- `PRODUCT_ARCHITECTURE.md`: pont entre architecture technique reelle et domaines produit
- `IDEAS.md`: catalogue priorise des idees produit et des blocs d'evolution
- `AI_AGENT_WORKFLOW.md`: methode de travail attendue pour les agents IA

## 1. Vue d'ensemble

PredictFinance est un projet d'analyse de valeurs financieres pilote par patterns boursiers.
Le but produit est d'aider un utilisateur a:

- selectionner une valeur financiere
- lancer une simulation ou une analyse
- recevoir une liste de patterns candidats
- choisir un pattern a explorer
- comprendre la phase courante de ce pattern
- lire une recommandation metier contextualisee

Le systeme ne doit pas etre pense comme un simple classifieur.
Il doit distinguer clairement:

- analyse de candidats: quels patterns semblent plausibles sur une valeur
- selection de pattern: quel pattern l'utilisateur ou le produit choisit d'explorer
- phase: ou la valeur se situe dans la structure du pattern choisi
- recommandation: action metier contextualisee a partir du pattern, de sa phase et de la confiance

Etat reel aujourd'hui:

- le MVP est reellement operationnel sur un seul pattern: `DOUBLE_TOP`
- le repo contient deja des briques de generalisation vers du multi-pattern
- la chaine effective reste cependant majoritairement mono-pattern

## 2. Architecture du projet

Le projet est compose de 3 briques:

- `FinanceFront`: application Angular 21
- `FinanceAPI`: API ASP.NET Core .NET 10
- `FinanceIA`: moteur Python IA

Role de chaque brique:

- `FinanceFront`: saisie utilisateur, navigation admin/client, affichage des resultats, consommation des endpoints backend
- `FinanceAPI`: validation serveur, authentification, autorisation, orchestration metier, persistance, appel au moteur Python, exposition des DTO au front
- `FinanceIA`: acquisition de donnees de marche, feature engineering, scoring, detection de phase, generation de sorties IA

Frontieres de responsabilite:

- la logique metier critique ne doit pas vivre dans Angular
- l'API ne doit pas reimplementer la logique pattern-specifique deja calculee par Python
- Python ne doit pas porter la logique d'autorisation, de role ou de restitution produit finale

Schema textuel simple:

`Angular client/admin -> API .NET -> Python IA -> API .NET -> Angular`

Flux secondaire:

`API .NET -> SQL Server`

Principaux points d'entree:

- Front routes: `FinanceFront/src/app/app.routes.ts`
- API host: `FinanceAPI/BackPredictFinance.API/Program.cs`
- Python CLI/runtime: `FinanceIA/main.py`

## 3. Etat actuel de l'existant

Ce qui fonctionne aujourd'hui:

- auth JWT + refresh token via l'API .NET
- separation front admin/client avec guards Angular
- analyse et simulation sur une valeur cote front client
- lancement d'analyse cote admin
- pipeline Python operationnel sur `DOUBLE_TOP`
- le flux d'analyse backend accepte maintenant un `RequestedPattern` optionnel et le propage jusqu'a la CLI Python `predict`
- les guards front honorent maintenant le refresh token avant redirection quand le token d'acces a expire
- build API OK, typecheck Angular OK, tests Python OK

Pattern reellement exploite:

- `DOUBLE_TOP`

Parcours reels implementes:

- client: selection d'une valeur, ajout a la watchlist, quote live, transactions, analyse IA, simulation
- admin: gestion utilisateurs, lancement d'analyse sur valeur selectionnee
- supervision IA: endpoints `IA/Health` et `IA/Status`

Limites connues:

- un seul pattern reellement calcule de bout en bout
- `FinanceIA` a une registry runtime mais l'entrainement et la validation restent relies a `double_top`
- l'API a maintenant un catalogue de patterns minimal, mais un seul pattern actif et exploite (`DOUBLE_TOP`)
- le front n'expose pas encore un vrai flux "patterns candidats -> selection -> phase -> recommandations"
- la persistance d'analyse ecrit maintenant un `AnalysisRun`, mais un fallback legacy sur `Recommendation` reste necessaire tant que la migration generee n'est pas appliquee sur les environnements
- le front client mappe maintenant davantage de champs deja exposes par l'API (`phase`, niveaux techniques, `modelStatus`, `modelMessage`, `isActionable`) mais sans encore proposer de vrai parcours multi-pattern

Incoherences connues:

- le code EF et le snapshot sont maintenant realignes, mais les environnements n'ont pas encore forcement applique la migration correspondante
- le payload riche Python est maintenant mieux exploite sur les flux client d'analyse et de simulation, mais pas encore de facon uniforme sur tous les parcours
- le front a maintenant `ClientFinanceService` comme point d'entree officiel unique pour l'analyse; le vieux chemin `PredictionService` a ete retire
- certains templates Angular utilisent encore des formes legacy alors que le projet est en Angular 21, meme si le flux finance client cible a deja ete remis en syntaxe moderne

## 4. Parcours fonctionnels

Parcours client actuel:

1. L'utilisateur se connecte.
2. Il accede a l'espace client.
3. Il choisit une valeur.
4. Il peut consulter le cours, gerer sa watchlist, saisir des transactions.
5. Il peut lancer une analyse IA.
6. Il peut lancer une simulation sur `DOUBLE_TOP`.
7. Il recoit une restitution simplifiee.

Parcours admin actuel:

1. L'admin se connecte.
2. Il accede a l'espace admin.
3. Il peut gerer les utilisateurs.
4. Il peut choisir une valeur autorisee.
5. Il peut lancer une analyse IA.

Parcours cible client:

1. Selection d'une valeur financiere.
2. Saisie optionnelle de quantite detenue.
3. Lancement d'une simulation globale.
4. Reception d'une liste ordonnee de patterns candidats.
5. Selection d'un pattern par l'utilisateur.
6. Recuperation de la phase courante du pattern choisi.
7. Affichage des recommandations metier contextualisees.

Parcours cible admin:

1. Lancement de simulations sur des valeurs.
2. Consultation de l'historique des runs.
3. Rejeu de simulations si la persistance le permet.
4. Supervision de l'etat du moteur IA et du catalogue de patterns.

## 5. Domaine fonctionnel

Concepts metier principaux:

- utilisateur: personne authentifiee utilisant l'application
- role: `User`, `Admin`, `SuperAdmin`
- valeur financiere / instrument: actif suivi par l'utilisateur
- pattern: structure boursiere analysee par l'IA
- pattern candidat: pattern probable sur une valeur a un instant donne
- phase: etat courant d'un pattern choisi
- recommandation: action metier produite a partir du pattern et de sa phase
- simulation: execution orientee restitution utilisateur
- run d'analyse: execution tracee avec contexte, sorties, statut et version moteur
- version moteur / modele / pattern: metadonnees de reproductibilite et d'audit

Etat du domaine dans le code:

- domaine utilisateur/roles: `FinanceAPI/BackPredictFinance.Datas/Entities/User.cs`
- domaine finance legacy: `Asset`, `UserAsset`, `Recommendation`, `PriceHistory`, `AssetTransaction`
- domaine analyse cible deja amorce: `AnalysisBatch`, `AnalysisRun`, `PatternAssessment`, `DecisionSignal`, `ModelSnapshot`

## 6. Contrats importants

Front -> API aujourd'hui:

- espace client via `ClientFinanceController`
- espace admin via reutilisation partielle du meme service front finance
- analyse client actuelle: `POST /api/ClientFinance/analysis/run`
- payload d'analyse actuel: `symbol` obligatoire, `requestedPattern` optionnel
- si `requestedPattern` est absent, le backend retombe sur `DOUBLE_TOP`
- si `requestedPattern` est fourni, seul `DOUBLE_TOP` est accepte a ce stade
- simulation client actuelle: `POST /api/ClientFinance/simulation/run`
- logout transmet maintenant le `refreshToken` courant afin de permettre sa revocation serveur

API -> Python aujourd'hui:

- principalement via CLI:
- `python -m finance_ia.cli.predict`
- `python -m finance_ia.cli.simulate`
- `python -m finance_ia.cli.evaluate`
- le pont `predict` transmet maintenant explicitement `--pattern`
- un runtime HTTP FastAPI existe aussi mais n'est pas le pont principal actuellement

Statuts fonctionnels attendus a stabiliser:

- succes
- aucun signal exploitable
- donnees insuffisantes
- pattern non supporte
- erreur technique moteur
- erreur source de donnees

Erreurs importantes:

- validation d'entree
- timeouts Python
- artefacts absents
- pattern non supporte
- symboles hors liste autorisee

Champs structurants a preserver ou generaliser:

- `symbol`
- `pattern`
- `phase`
- `pattern_assessments`
- `decision_signal`
- `modelStatus`
- `modelMessage`
- `targetPrice`
- `invalidationPrice`
- `modelVersion`
- `selected_threshold`

## 7. Architecture cible

Trajectoire recommandee:

- etape 1: stabiliser l'existant mono-pattern
- etape 2: generaliser les contrats pour demander un pattern par run
- etape 3: introduire une simulation multi-pattern reelle
- etape 4: permettre la selection explicite d'un pattern puis l'analyse detaillee de sa phase

Principe de catalogue de patterns:

- l'API doit exposer un catalogue central des patterns supportes
- ce catalogue doit porter au minimum:
- cle pattern
- nom metier
- etat active/desactive
- dossier d'artefacts / modele
- version du pattern
- version du modele
- etat actuel: un catalogue minimal cote API existe deja pour resoudre `ModelDir` et `ModelVersion`, avec `DOUBLE_TOP` comme pattern par defaut

Logique de selection d'un pattern:

- la simulation globale retourne des patterns candidats tries
- l'utilisateur choisit ensuite le pattern qu'il souhaite explorer
- l'API demande alors au moteur Python le detail du pattern selectionne

Logique de detail d'un pattern selectionne:

- determination de phase
- niveaux techniques utiles
- confiance
- recommandation metier
- alertes/ambiguite si le signal est faible

## 8. Persistance et historique

Source de verite actuelle:

- en code applicatif, `AnalysisRun` devient la source privilegiee pour l'historique recent quand les tables existent
- un fallback sur `Recommendation` reste actif tant que le schema reel n'est pas aligne sur le modele EF

Source de verite cible:

- `AnalysisRun` doit devenir l'unite centrale de verite pour les analyses et simulations

Entites importantes:

- legacy:
- `Recommendation`
- `UserAsset`
- `PriceHistory`

- cible analyse:
- `AnalysisBatch`
- `AnalysisRun`
- `PatternAssessment`
- `DecisionSignal`
- `ModelSnapshot`
- `AssetQuoteSnapshot`
- `AssetCandleSnapshot`

Ecarts connus entre modele EF et migrations:

- les nouvelles entites d'analyse sont declarees dans `FinanceDbContext`
- elles sont configurees dans `ModelBuilderConfigurationExtensions`
- un `IDesignTimeDbContextFactory` existe maintenant pour permettre a `dotnet ef` de generer les migrations sans dependre du host complet
- une migration `AddAnalysisHistoryDomain` a ete generee pour realigner le snapshot sur le modele actuel

Consequence:

- il faut appliquer la migration generee sur les environnements cibles avant de supprimer le fallback legacy
- la migration generee rattrape un ecart plus large que `AnalysisRun` seul: tables d'analyse, snapshots et certaines colonnes `Asset` deja presentes dans le modele

## 9. Securite, observabilite, qualite

Auth/roles:

- auth JWT + refresh token rotatif cote API
- separation admin/client cote front et cote API

Validation serveur:

- a renforcer sur tous les DTO de simulation/analyse
- ne jamais faire confiance au front

Contraintes OWASP:

- aucune logique d'autorisation cote front
- validation stricte des entrees
- limitation des erreurs techniques exposees
- journalisation sans fuite sensible
- hardening du pont .NET / Python

Attentes SonarQube:

- responsabilites nettes par classe
- peu de duplication
- gestion explicite des erreurs
- noms clairs
- tests ajoutables facilement

Logs:

- Serilog dans l'API
- endpoint de statut IA disponible

TraceId:

- le middleware d'exception genere deja un `traceId`
- il doit devenir un element de correlation front/API/Python

Points faibles actuels:

- CORS trop permissif
- middleware de rate limiting maintenant branche dans le pipeline API, avec une implementation encore tres simple basee sur l'IP et l'`IMemoryCache`
- certaines erreurs Python ne sont pas encore taxonomisees proprement
- l'observabilite metier des simulations reste limitee

## 10. Front Angular

Organisation admin/client:

- routes centralisees dans `FinanceFront/src/app/app.routes.ts`
- routes admin: `Routes/app.routes.admin.ts`
- routes client: `Routes/app.routes.user.ts`

Services principaux:

- `ClientFinanceService`: principal point d'acces aux flux finance client
- `AuthService`, `StorageService`, interceptors auth/erreur

Conventions UI/etat observees:

- composants standalone
- RxJS dans les services et orchestration de page
- `takeUntilDestroyed` deja utilise sur plusieurs flux
- etat local surtout gere dans les composants/pages
- les modeles front d'analyse et de simulation sont maintenant plus proches des DTO backend reellement retournes

Points a corriger:

- sortir progressivement la liste des patterns des composants vers un vrai contrat backend; un constant shared temporaire remplace deja le hardcode local disperse
- continuer a nettoyer les duplications de parcours autour du seul point d'entree `ClientFinanceService`
- converger vers la syntaxe Angular 21 moderne dans tous les templates
- exposer un vrai flux multi-pattern dans l'UI

Interdiction:

- aucune logique metier critique de pattern, phase ou recommandation dans Angular

## 11. Backend .NET

Architecture observee:

- controllers ASP.NET Core
- services applicatifs
- EF Core + Identity
- orchestration Python via `PythonApiService`
- factory design-time EF dans `BackPredictFinance.Datas/Context/FinanceDbContextFactory.cs`

Conventions de validation:

- aujourd'hui surtout par garde-fous dans les services
- cible: DTO plus explicites + validation serveur plus systematique

Persistance d'analyse:

- nouvel axe present dans le domaine EF
- pas encore la source de verite operative

Anti-patterns actuels:

- restitution encore partiellement legacy selon les flux, malgre un mapping `predict` deja enrichi
- le catalogue de patterns existe cote API, mais il n'est pas encore expose comme contrat produit ni admin
- fallback historique encore maintenu sur `Recommendation` tant que les migrations EF ne sont pas alignees

Points a stabiliser:

- contrats API vers Angular
- contrats API vers Python
- suppression du fallback legacy quand `AnalysisRun` sera garanti par les migrations
- extension du catalogue de patterns a plusieurs entrees actives sans casser les flux actuels

## 12. Python IA

Pipeline actuel:

- fetch de donnees de marche
- ajout d'indicateurs
- labelisation `DOUBLE_TOP`
- entrainement LightGBM binaire
- prediction
- simulation

Pattern registry:

- un debut de registry existe dans `FinanceIA/src/finance_ia/patterns`
- aujourd'hui, seul `DOUBLE_TOP` y est enregistre

Limites actuelles:

- `build_dataset.py` est encore relie directement a `build_double_top_labels`
- `validate.py` reste relie a `DOUBLE_TOP`
- `simulate.py` refuse explicitement les autres patterns
- la CI/deploy restent nommes autour de `double_top`

Regles pour ajout futur de pattern:

- isoler la logique pattern-specifique dans un module dedie
- eviter les `if pattern == ...` disperses
- generaliser train/evaluate/predict/simulate autour d'une abstraction commune
- versionner les sorties et les artefacts

## 13. Dette technique connue

Dettes prioritaires:

1. Migration EF d'analyse a appliquer sur les environnements pour supprimer le fallback legacy
2. Mono-pattern dur dans les contrats et la configuration
3. Exploitation du payload riche Python encore non uniforme selon les flux
4. Historique d'analyse encore partiellement adosse a `Recommendation`
5. CORS et anti-abus a durcir
6. UI encore sans vrai parcours candidats -> selection -> detail
7. Catalogue de patterns non expose comme contrat produit/admin

Impact concret:

- freine le passage au multi-pattern
- cree des divergences possibles entre Python et .NET
- rend l'audit et la supervision d'analyse fragiles
- augmente le risque de regressions lors de l'ajout d'un nouveau pattern

Priorite:

- haute sur les points 1 a 5
- moyenne sur les points 6 et 7

## 14. Regles d'intervention pour Codex

Regles obligatoires:

- toujours lire ce README avant toute modification structurante
- toujours partir de l'existant reel du depot
- ne pas lancer de refonte big bang
- stabiliser les contrats avant toute extension fonctionnelle
- ne pas dupliquer la logique pattern/metier entre .NET et Python
- ne pas hardcoder `DOUBLE_TOP` dans les nouvelles couches
- ne pas mettre de logique metier critique dans Angular
- privilegier de petites etapes validables
- traiter explicitement les ecarts entre domaine, DTO, persistance et UI
- citer les fichiers reellement impactes avant une modification importante
- mettre a jour ce README apres toute modification structurante

Regles de conception:

- conserver les conventions existantes lorsqu'elles sont coherentes
- preferer l'extension locale au remplacement global
- faire d'`AnalysisRun` la source de verite cible des analyses
- introduire un catalogue de patterns avant de viser un multi-pattern avance

## 15. Plan d'evolution recommande

Phase 1 - Stabilisation de l'existant

- aligner les contrats existants
- passer le pattern demande de bout en bout
- etat courant: `requestedPattern` est deja transporte sur le flux d'analyse `predict`, avec fallback et support limite a `DOUBLE_TOP`
- ne plus recalculer la recommandation cote API
- preparer la migration EF des nouvelles entites

Phase 2 - Contrats et generalisation

- introduire les DTO cibles simulation/detail pattern
- etendre le catalogue de patterns cote API au-dela du pattern par defaut deja centralise
- generaliser les options Python par pattern

Phase 3 - Multi-pattern reel

- faire scorer plusieurs patterns pour une meme valeur
- retourner une liste de candidats
- permettre la selection explicite d'un pattern
- analyser sa phase et ses recommandations

Phase 4 - Admin avance / supervision / historique

- consultation des runs
- rejeu admin
- supervision du catalogue et des versions
- audit et observabilite metier renforces

## 16. Glossaire

- pattern: structure boursiere analysee par le moteur IA
- pattern candidat: pattern probable renvoye par une simulation globale
- phase: etat courant d'un pattern selectionne
- recommandation: action metier contextualisee
- simulation: execution orientee restitution produit
- analyse: lecture detaillee d'un pattern choisi
- run d'analyse: execution tracee d'une analyse ou simulation
- moteur IA: code Python portant les calculs
- modele: artefact entraine pour un pattern donne
- version pattern: version logique des regles de detection/phase d'un pattern
- version moteur: version du code Python global

## Procedure de mise a jour du README

Ce README doit etre mis a jour a chaque changement impactant:

- architecture
- contrats
- endpoints
- DTO
- flux front
- pattern supporte
- persistance
- configuration des patterns
- versionnement
- securite
- observabilite

Chaque mise a jour du README doit noter:

- ce qui a change
- pourquoi cela a change
- l'impact concret
- si cela rapproche ou non de la cible multi-pattern

Derniere mise a jour structurante documentee:

- ajout de `requestedPattern` sur le flux backend `analysis/run`
- propagation de ce champ jusqu'a la CLI Python `predict` via `--pattern`
- impact: contrat d'analyse mieux stabilise sans casser le fallback mono-pattern existant
- rapprochement de la cible multi-pattern: oui, partiel seulement car `DOUBLE_TOP` reste le seul pattern supporte
- ecriture et lecture de l'historique d'analyse via `AnalysisRun` des que les tables existent, avec fallback temporaire sur `Recommendation`
- impact: la source de verite cible est maintenant branchee cote service, sans casser les environnements ou les migrations ne sont pas encore appliquees
- rapprochement de la cible multi-pattern: oui, car `AnalysisRun` porte deja `RequestedPattern`, `PatternAssessments`, `DecisionSignal` et `ModelSnapshot`
- ajout d'un `FinanceDbContextFactory` design-time et generation de la migration `AddAnalysisHistoryDomain`
- impact: le schema EF peut maintenant etre aligne proprement via `dotnet ef`, ce qui debloque la suppression future du fallback `Recommendation`
- rapprochement de la cible multi-pattern: oui, car la persistance cible devient deployable au lieu de rester seulement theorique dans le modele
- introduction d'un `PatternCatalogService` cote API pour resoudre `ModelDir` / `ModelVersion` par pattern
- impact: la resolution des artefacts Python n'est plus dispersee autour d'un `ModelDir` global unique, meme si seul `DOUBLE_TOP` reste actif
- rapprochement de la cible multi-pattern: oui, car l'ajout d'un deuxieme pattern se fera d'abord par configuration/catalogue avant d'impacter les services
- suppression du `PredictionService` front et de son modele legacy au profit du seul `ClientFinanceService`
- impact: le front ne maintient plus deux chemins d'analyse concurrents (`Trading/predict` vs `ClientFinance/analysis/run`)
- rapprochement de la cible multi-pattern: oui, car un seul point d'entree front est plus simple a faire evoluer vers le futur contrat candidats -> selection -> detail
- remplacement des hardcodes locaux `DOUBLE_TOP` dans le flux de simulation client par un constant shared temporaire et alignement des modeles front avec les champs enrichis deja renvoyes par l'API
- impact: l'UI client affiche maintenant phase, niveaux techniques, statut modele et signal exploitable sans reintroduire de logique metier critique cote Angular
- rapprochement de la cible multi-pattern: oui, partiel, car le front depend encore d'une liste locale de patterns supportes tant que le catalogue n'est pas expose par l'API
- envoi explicite de `RequestedPattern` depuis le front d'analyse, meilleure lisibilite de l'historique client et messages plus clairs pour l'analyse/simulation en francais
- impact: les parcours client/admin d'analyse sont mieux alignes avec le contrat backend existant et la restitution front devient plus lisible sans changer le perimetre mono-pattern
- rapprochement de la cible multi-pattern: oui, leger, car le contrat d'analyse est plus coherent cote front et les champs deja exposes sont mieux exploites
- branchement effectif du `RateLimitingMiddleware` dans le pipeline `Program.cs`
- impact: les endpoints API sont maintenant proteges par un premier garde-fou anti-abus, sans modifier les contrats applicatifs
- rapprochement de la cible multi-pattern: indirect, car cela durcit l'exposition des futurs endpoints de simulation/analyse avant d'ajouter plus de charge metier
- revocation serveur du refresh token lors du logout et suppression du log front exposant les tokens
- impact: une deconnexion invalide maintenant la chaine de refresh associee au token presente, ce qui reduit le risque de reutilisation apres logout
- rapprochement de la cible multi-pattern: indirect, car cela durcit le socle auth sans changer les flux metier
- unification du comportement des guards front autour d'un controle centralise `ensureValidAccessToken`
- impact: la navigation client/admin reutilise maintenant le meme mecanisme de refresh avant de forcer un retour vers login, ce qui reduit les deconnexions inutiles
- rapprochement de la cible multi-pattern: indirect, car cela fiabilise l'acces au produit sans toucher aux contrats patterns

Regle de maintenance:

- ne pas attendre plusieurs evolutions pour mettre a jour ce document
- mettre a jour ce README dans la meme intervention que le changement structurant quand c'est possible
- si une information reste ambigue, l'ecrire explicitement au lieu de la masquer
