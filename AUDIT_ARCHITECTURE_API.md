# Audit d'architecture — API PredictFinance (backend `.NET`, branche `dev`)

> Perimetre : `FinanceBack/` (7 projets, 38 613 lignes de C# hors `bin`/`obj`/`Migrations`).
> Methode : lecture directe du code. Chaque finding cite un fichier et une ligne verifiee, et a survecu a une refutation adversariale par 3 relecteurs independants.
> 89 findings retenus : 2 CRITIQUE, 52 MAJEUR, 35 MINEUR.
>
> **Note de verification (post-audit) :** les 13 findings de la dimension "Provider marche (Yahoo)" et "Moteur de patterns"
> (§4.11-4.12) proviennent d'une 2e passe d'audit dont la refutation adversariale automatisee a echoue (limite de session).
> Ils ont ete relus et reverifies manuellement fichier par ligne : 12 sur 13 sont confirmes tels quels. Le 13e (`MKT-01`,
> vocabulaire pays) a ete corrige — le mecanisme decrit initialement ("ecrasement inconditionnel") etait inexact ; voir
> le detail corrige en §4.11.

---

## 1. Verdict

L'API **fonctionne et livre le bon resultat sur les chemins nominaux**, mais elle porte une dette structurelle lourde et deux defauts de perte de donnees reels. Deux fondations transverses polluent tout le backend : `BaseService` (56 services heritent d'un service-locator qui execute une requete SQL **morte** dans chaque constructeur) et la couche d'analyse (`ClientFinanceServices/Analysis/`, 18 fichiers a plat, zero des 9 frontieres de capability imposees). **Non, ce code ne se relit pas facilement** : la moitie des leviers de calibration, deux vocabulaires de pays, quatre calculs de prix de revient et deux implementations RSI/MACD coexistent sans qu'aucun ne reference l'autre — un relecteur doit reconstituer mentalement la verite metier a partir de sources dispersees.

## 2. Tableau de bord

### Par severite
| Severite | Nombre |
|---|---|
| 🔴 CRITIQUE | 2 |
| 🟠 MAJEUR | 52 |
| 🟡 MINEUR | 35 |
| **Total** | **89** |

### Par dimension
| Dimension | CRITIQUE | MAJEUR | MINEUR | Total |
|---|---|---|---|---|
| Acces BDD (EF) | 2 | 4 | 2 | 8 |
| Couches / frontieres | 0 | 6 | 3 | 9 |
| Duplication / lisibilite | 0 | 4 | 3 | 7 |
| Commentaires | 0 | 1 | 3 | 4 |
| Organisation des fichiers | 0 | 3 | 6 | 9 |
| Documentation | 0 | 4 | 3 | 7 |
| Securite | 0 | 5 | 4 | 9 |
| Correctness | 0 | 7 | 1 | 8 |
| Contrats / ViewModels | 0 | 5 | 2 | 7 |
| Tests / CI | 0 | 3 | 5 | 8 |
| Provider marche (Yahoo) | 0 | 4 | 0 | 4 |
| Moteur de patterns | 0 | 6 | 3 | 9 |
| **Total** | **2** | **52** | **35** | **89** |

---

## 3. Top 5 des priorites (meilleur ratio impact/effort)

### P1 — Suppression physique en cascade qui detruit l'historique fiscal `[DB-04, CRITIQUE, effort M]`
`ClientFinanceWatchlistPortfolioService.cs:180` fait `_financeDbContext.UserAssets.Remove(userAsset)` apres le seul garde `Quantity > 0` (l.175). Or une position soldee a `Quantity == 0` conserve tout son historique. La FK `FK_AssetTransactions_UserAssets_UserAssetId` est en `Cascade` (`InitMigration.cs:453`, confirme dans le ModelSnapshot) : un simple « retirer de la watchlist » (`ClientFinanceMarketController.cs:37`, `HttpDelete`) efface physiquement les `AssetTransaction` (utilisees par `TaxService.cs:35`) et les `Recommendation`, alors que ces lignes sont concues pour du soft-delete (`IsDeleted`). Violation directe de la regle « jamais de DELETE SQL ».
**Action** : passer la FK en `Restrict`, refuser la suppression tant qu'il existe une `AssetTransaction` (meme soft-deletee), ou ajouter `IsDeleted` a `UserAsset` et basculer les 2 `Remove` (`UserAssetService.cs:86` est du code mort a supprimer).

### P2 — Requete SQL morte dans le constructeur de `BaseService` `[SEC-02 / CORR-07 / DUP-04 / LAYER-01, MAJEUR, effort S]`
`BaseService.cs:56` execute `_currentUserRoles = _financeDbContext.UserRoles.Where(ur => ur.UserId == userId).ToList()` — **synchrone, bloquant, dans le constructeur**, herite par **56 services** `Scoped`. Or `grep _currentUserRoles` = 2 occurrences (declaration l.26 + affectation l.56) : le champ n'est **lu nulle part**. Chaque requete HTTP paie N `SELECT AspNetUserRoles` inutiles (5 rien que pour `ClientFinanceAnalysisController`). Le resultat est mort.
**Action immediate** : supprimer la ligne 56 et le champ l.26 ; conserver `_currentUserId` (issu des claims, largement utilise). Une ligne supprimee = N requetes SQL bloquantes en moins par requete HTTP. Chantier structurel separe : geler l'ajout de descendants de `BaseService` et injecter explicitement.

### P3 — `ChangeTracker.Clear()` au milieu d'une ecriture + faux `AnalysisId` retourne `[DB-03 / CORR-04, CRITIQUE, effort M]`
`AnalysisSnapshotPersistenceService.cs:294` fait `AddAsync(analysisRun)` (graphe complet) **avant** `UpsertCandlesAsync` (l.297) ; le seul `SaveChanges` est l.299. `UpsertSingleCandleAsync` fait un `SaveChanges` par bougie dans la boucle (l.112) et, sur conflit d'index unique (course avec `MarketDataRefreshJob`), un `catch (DbUpdateException) { ChangeTracker.Clear() }` (l.120) qui **detache le graphe `AnalysisRun` non encore ecrit** et toutes les mises a jour de bougies accumulees. La methode retourne quand meme `analysisRun.Id`, remonte comme `PublicId` au client (`ClientAnalysisOrchestrator.cs`) : **le front recoit un identifiant de snapshot qui n'existe pas en base**. Aucun `BeginTransaction` dans tout le backend.
**Action** : sortir l'upsert de bougies (1 SELECT groupe + 1 `SaveChanges`), ne jamais appeler `ChangeTracker.Clear()` sur le contexte partage, englober analyse+bougies dans une transaction explicite.

### P4 — Aucun `HasQueryFilter` : soft-delete reimplemente 46 fois `[DB-05, MAJEUR, effort M]`
`grep HasQueryFilter` = 0 sur tout le backend ; en face, 46 predicats `!x.IsDeleted` manuels sur ~20 fichiers pour 8 entites. Aucun oubli avere aujourd'hui, mais un seul `.Where` oublie sur une future requete exposera des lignes supprimees, sans filet compilateur/test/base.
**Action** : interface marqueur `ISoftDeletable` + `modelBuilder.Entity<T>().HasQueryFilter(x => !x.IsDeleted)` pour les 8 entites, retirer les 46 predicats, `IgnoreQueryFilters()` sur les rares chemins back-office. Attention : les 2 seeders (`EducationArticlesSeedService.cs:33`, `GlossaryTermsSeedService.cs:33`) lisent via des index uniques **non filtres** — les proteger.

### P5 — `BackPredictFinance.API.zip` (4,9 Mo) versionne avec appsettings de dev `[ORG-01, MAJEUR, effort S]`
`git ls-files` confirme deux zips suivis (`FinanceBack/BackPredictFinance.API.zip`, `FinanceFront/src.zip`). Le zip API contient `appsettings.json` et `appsettings.Development.json` — exclus par `.gitignore:11` — plus toute l'arborescence `bin/Debug/`. Contenu reel : `appsettings.json` = placeholders `__SET_ME__` (pas de secret prod), mais `appsettings.Development.json` porte un vrai `JWTToken.Secret` (124 car.), `ServerSalt` et les mots de passe de seed `adminPwd`/`userPwd` — secrets de dev a rotationner + 5 Mo de binaires dans l'historique git.
**Action** : `git rm --cached` les 2 zips, `*.zip` au `.gitignore`, rotationner les secrets de dev, ajouter un check de secrets en CI.

---

## 4. Findings par dimension

### 4.1 Acces BDD (EF)
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🔴 | `AnalysisSnapshotPersistenceService.cs:120` | `ChangeTracker.Clear()` dans un catch efface le graphe `AnalysisRun` non persiste + updates de bougies ; faux `PublicId` retourne. Aucune transaction dans le backend. | Upsert bougies isole (1 SELECT + 1 SaveChanges), transaction explicite, jamais de `Clear()` sur contexte partage |
| 🔴 | `ClientFinanceWatchlistPortfolioService.cs:180`, `UserAssetService.cs:86` | FK `AssetTransactions → UserAssets` en `Cascade` + `Remove` physique : perte fiscale sur position soldee (2 occurrences, 1 exposee, 1 morte) | FK `Restrict` + soft-delete `UserAsset` ; supprimer le code mort |
| 🟠 | `ClientFinanceProjectionService.cs:137` | `LoadLatestAnalysisByAssetIdAsync` materialise **tout** l'historique `AnalysisRuns` (`RawPayload` nvarchar(max)) pour garder la derniere ligne par actif, en memoire. Chemin chaud watchlist+portefeuille | `GroupBy(AssetId).Select(...First())` cote SQL + projection explicite |
| 🟠 | `UserAdminService.cs:81` + `UserRoleDataService.cs:144` | N+1 sur le listing admin (1 requete de roles par utilisateur), `CancellationToken` perdu au dernier saut, `PageSize` non plafonne | 1 requete `Where(ur => userIds.Contains(...))` + propager le `ct` |
| 🟠 | `AnalyticsRetentionJob.cs:54` + `ModelBuilderConfigurationExtensions.cs` | Table `Analytic` : aucune config EF, aucun index, colonnes `nvarchar(max)` ; purge RGPD plafonnee `Take(5000)` 1x/mois → retention 13 mois depassee sans alerte ; `EndsWith("@anon")` non-sargable | Index sur `Date`, booleen `IsAnonymized` indexable, boucle par lots ou `ExecuteUpdateAsync` |
| 🟠 | `BaseService.cs:56` | Requete SQL synchrone morte a chaque instanciation de 56 services (voir P2) | Supprimer la ligne et le champ |
| 🟡 | `DatabaseUpdater.cs:58` | Schema `RefreshTokens` defini 2x (migration EF + DDL brut idempotent rejoue au boot). No-op en nominal, contredit le fail-fast documente | Supprimer `EnsureRefreshTokenStorageAsync` |
| 🟡 | `Datas/Common/Extensions.cs:48` | `GetByPaginationAsync`/`GetTotalCountAsync`/`DeterministicGuid` sans appelant ; pagination par concat de chaines (`System.Linq.Dynamic.Core`), param `countryIds` fantome, classe `Extentions` (typo) | Supprimer le code mort, garder `OrderByDynamic`, whitelist sur `sortActive` |

### 4.2 Couches / frontieres
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `BaseService.cs:33` | Service-locator (`IServiceProvider.GetRequiredService` x9) herite par 56 services : dependances reelles invisibles, couplage force a EF+HttpContext+Identity | Injection explicite, geler les nouveaux descendants (`AGENTS.md:263`) |
| 🟠 | `DatabaseUpdater.cs:56` | DDL de persistance dans le projet API (delivery HTTP) — fuite de couche + doublon migration | Supprimer, corriger par migration si besoin |
| 🟠 | `ClientFinanceServices/Analysis/` | 18 fichiers a plat : **0 des 9 frontieres** `Analysis/*` imposees par `AGENTS.md:269-279`. Namespaces identiques → cycles non detectables | Migrer capability par capability (Persistence, Risk d'abord) ou consigner en `DEROGATION` |
| 🟠 | `SeedData/ConceptsSeedService.cs:23` | 3 `IHostedService` de seed dans le projet API font LINQ+`SaveChanges` ; un deserialise le JSON direct dans l'entite EF (mass-assignment sur cle) alors que ses voisins passent par un record de seed | Deplacer vers Services ; introduire `AnalysisConceptSeedRecord` |
| 🟠 | `AnalysisSnapshotPersistenceService.cs:307` | La capability Persistence appelle le provider marche externe (Yahoo) en plein chemin d'ecriture + N+1 SQL par bougie | Sortir la resolution de date de resultats du service ; chargement groupe |
| 🟠 | `BackPredictFinance.Patterns/Common/ContinuationPatternDefinitionBase.cs:89` | Le projet Patterns produit du wording FR utilisateur (`ModelMessage`, `StatusReason`) contournant le circuit de wording gouverne (`AGENTS.md:286`) | Codes structures (`StatusReasonCode`) + resolution par `IAnalysisAccompanimentWordingProvider` |
| 🟡 | `ProgramServiceExtensions.cs:34,85,44` | `IPathService` et `IEtfProfileProvider` enregistres sans consommateur ; `AnalyticService` enregistre sur classe concrete | Supprimer les morts, `IAnalyticService` |
| 🟡 | `PortfolioRiskMetricsService.cs`, `TickerService.cs:10`, `TwelveDataServices/` | Namespaces desalignes des dossiers ; dossier `TwelveDataServices` contient un provider Yahoo (nom mensonger propage dans 25 fichiers) | Renommer `MarketDataProviders`, corriger namespaces |
| 🟡 | `Middleware/ExceptionMiddleware .cs` | Espace dans le nom de fichier | `git mv` |

### 4.3 Duplication / lisibilite
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `Common/AnalysisV1/TechnicalIndicators.cs` + `Indicators/TechnicalIndicatorsService.cs` | RSI, MACD, EMA implementes 2x avec 2 jeux de constantes (14/12/26/9, seuils 70/30) et comportements de bord divergents (50m vs null, 6 vs 4 decimales). Sources de donnees differentes (`TickerCandle` vs `PriceHistory`) — 3 findings recoupes (DUP-01, LAYER-03, CORR-09) | Unifier sur `Common.AnalysisV1.TechnicalIndicators` + adaptateur ; test back justifie |
| 🟠 | `BaseService.cs:18` | 10 dependances par service-locator, 4/10 pour 0-1 consommateur, requete SQL morte (voir P2) | Reduire au noyau reel |
| 🟠 | `PagedResultViewModel.cs` + 3 types Paged bespoke | Pagination reimplementee 4x ; le generique n'est utilise qu'1 fois ; normalisation de bornes copiee 3x ; `ClientFinanceHistoryReadService.cs:79` pagine **en memoire** (filtre sur JSON deserialise) vs SQL cote screener | Faire de `PagedResultViewModel<T>` le contrat unique + extension `ToPagedResultAsync` |
| 🟠 | `ModelBuilderConfigurationExtensions.cs:321,795,810` + `SeedData/*` | 3 entites de contenu seedees par 2 mecanismes concurrents (`HasData` EF + `HostedService` JSON) ; squelette des 3 services copie-colle ; fichier EF a 53% de contenu editorial. Jeux disjoints donc pas de doublon runtime, mais piege de maintenance | Un seul mecanisme par entite (`JsonContentSeeder<T>` generique) |
| 🟡 | `FundamentalScoringService.cs:76` | `ScoreAsync` 128 lignes / 6 responsabilites, `BuildScoreResult` 13 params dont 2 flags, codes de categorie en dur x3 malgre `FundamentalScoringPolicyDefaults.CategoryCodes` | Decouper + enum `ScoreOrigin` + `ScoringContext` record |
| 🟡 | `ClientFinanceAssetSupportService.cs:154` + 2 clones | `GetRequiredCurrentUserId` reecrit a l'identique dans 3 services, appele dans des lambdas EF (6 sites) | Consommer l'interface existante ou remonter dans `BaseService` |
| 🟡 | `AdminGlossaryService.cs:98` + 2 clones | Normalisation diacritiques ecrite 3x, 2 casses de sortie ; 1 seul chemin d'ecriture mais lecture desynchronisable | Helper `TextNormalizer` unique |

### 4.4 Commentaires
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `Datas/Common/DbTools.cs:12` | `// Je t'aime ChatGPT` commite ; `Datas/Common` entierement code mort (5/6 types), bug de double suffixe jamais execute, `Extentions` (typo), S1144 supprimee globalement | Supprimer `DbTools.cs`, `Extentions`, `DeterministicGuid`, `PredicateExtensions` |
| 🟡 | `BulkowskiReliability.cs:15` | Commentaire faux : `// Retournements (M4 — non encore branches)` alors que les 4 patterns sont cables, consommes, testes, en DI | Supprimer le marqueur |
| 🟡 | `RiskEvaluationService.cs:127,144` | Seuils volume (1.5/0.7) ecrits 3x : constante, prose XML, chaine utilisateur — 2 copies deriveront | Interpoler depuis la constante, `<see cref>` |
| 🟡 | `DoubleBottomPatternTests.cs:125` (+3 fichiers) | 16 bandeaux ASCII decoratifs en commentaire, masquent une triplication d'helpers de test | Extraire fixtures partagees, supprimer les bandeaux |

### 4.5 Organisation des fichiers
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `BackPredictFinance.API.zip`, `.gitignore:11`, `FinanceFront/src.zip` | 2 zips versionnes (5 Mo binaires) contenant appsettings exclus + secrets de dev (voir P5) | `git rm --cached`, `*.zip` gitignore, rotationner |
| 🟠 | `Datas/Common/DbTools.cs` + `Extensions.cs` | Dossier `Common` : 5/6 types publics morts, typo, camelCase public, commentaire parasite | Supprimer, ne garder que `QueryableOrderExtensions` |
| 🟠 | `ClientFinanceServices/` | Rangement par capability commence puis abandonne : 24 fichiers a plat doublonnant des sous-dossiers ; `Portfolio/` a 1 fichier alors que `PortfolioService` est a la racine ; `Fundamentals` a 2 endroits | Terminer le rangement, commit dedie sans changement fonctionnel |
| 🟡 | `.editorconfig:1` | 59 lignes de regles C++ uniquement, aucune convention C#/TS ; drift de style concentre dans `DbTools.cs` | `.editorconfig` .NET reel (charset, naming rules) |
| 🟡 | `ExceptionMiddleware .cs`, `CustomException .cs` | Espace dans le nom de 2 fichiers frequemment consultes | `git mv` |
| 🟡 | `TwelveDataServices/YahooFinanceMarketDataProvider.cs` | Dossier/namespace `TwelveDataServices` designe un provider Yahoo (propage 25x via `using`) | Renommer `MarketData`/`YahooFinance` |
| 🟡 | `Common/enums/` | Dossier minuscule (seul du repo), suffixe `Enum` incoherent, 8 fichiers fourre-tout, `TradingPatternEnum.cs` ne contient aucun enum de pattern | Convention unique + eclater ; S2344 est deja dans NoWarn |
| 🟡 | `ViewModels/.../Portfolio` vs `Portfolios`, `Fundamentals` x2 | 24 fichiers multi-types (dont 21 DTO+Profile AutoMapper co-localises, exception assumee), dossiers singulier/pluriel doublons | Fusionner les dossiers, fixer la convention |
| 🟡 | `API.csproj:11`, `Services.csproj:15,19` | `BCrypt.Net-Next` declare 2x jamais utilise (Identity fait le hash) ; `System.Linq.Dynamic.Core` inutile dans Services ; `Polly` utilise sans declaration (transitif) | Retirer les 3 refs mortes, declarer `Polly` |

### 4.6 Documentation
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `Documentation/deployment.md:20` + `Program.cs:247` | La doc affirme « migrations pas appliquees au boot » ; le code fait `MigrateAsync` dans TOUS les environnements sauf `Testing`, + DDL brut + seed de comptes (throw si credentials absents en prod) | Trancher : `IsDevelopment()` only, ou documenter le comportement reel |
| 🟠 | `README.md:47-49`, `AGENTS.md:469`, `.claude/CLAUDE.md:114` | 3/4 entrees de l'ordre de lecture pointent des fichiers inexistants (`Doc/product/*` archive sous `_legacy/`) ; la carte canonique designee par AGENTS.md n'existe plus ; `REMEDIATION-SUMMARY.md` reference 6x, absent | Repointer vers `Doc/v1/`, recreer ou rediriger, check de liens morts en CI |
| 🟠 | `Doc/v1/06_ecarts_doc_code.md:82,97,108` + `data-model.md:27` | L'ecart RGPD A-10 declare 🔴 CRITIQUE « retention nulle » alors que `AnalyticsRetentionJob` l'implemente (cap 13 mois, anonymisation mensuelle) | Retirer A-10, passer la maturite a Elevee |
| 🟠 | `local-setup.md:14`, `README.md:42`, `secrets-management.md` | Doc decrit un `appsettings.json` « committed » qui n'existe pas (gitignore exclut tout sauf `Testing`) ; regle « ajouter placeholder dans config commitee » inapplicable ; commentaire `.gitignore` contredit `secrets-management.md` | Aligner : aucun appsettings versionne hors Testing, completer le template Cors/Email/MarketData |
| 🟡 | `KNOWN_ISSUES.md:17` | Declare `DOUBLE_TOP` « legacy residue a retirer » ; contredit par README, registre d'ecarts, code (pattern first-class teste) | Supprimer le bloc |
| 🟡 | `api-endpoints.md:3` | Annonce « 30 controllers » (reel 32), omet `ClientFinanceAlertsController`, se contredit avec `README.md:19` (31) | Corriger le compte ou generer depuis le code/Swagger |
| 🟡 | `architecture.md:32`, `data-model.md:31`, `pitfalls.md:51` | Soft-delete presente comme « never » absolu ; `AssetTransaction` omis des 8 entites `IsDeleted` ; 2-3 `Remove` physiques (dont code mort) contredisent l'absolu | Ajouter `AssetTransaction`, remplacer « never » par la regle reelle |

### 4.7 Securite
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `JwtGeneratorService.cs:208` + `AccountController.cs:117,125` | 2 endpoints `[AllowAnonymous]` declenchent PBKDF2 100k iterations sur un refresh token 64 octets (entropie max, KDF inutile) → amplification DoS CPU sous le plafond de 200/min | HMAC-SHA256 avec cle serveur, ou palier strict `/api/Account` |
| 🟠 | `BaseService.cs:56` | Requete SQL synchrone bloquante inutile x N par requete HTTP → starvation pool de threads (voir P2) | Supprimer |
| 🟠 | `YahooFinanceMarketDataProvider.cs:452` + `TickersController.cs:31` | `interval` de query string interpole sans echappement dans l'URL sortante Yahoo (symbole echappe, pas interval) + cle de cache pilotable sans `SizeLimit` | Whitelist explicite d'intervalles (modele : `ScreenerService` `SortWhitelist`) |
| 🟠 | `Program.cs:106` + `BaseService.cs:48` + `UserAdminService.cs:113` | Aucune revocation serveur des JWT : compte desactive/supprime garde l'acces jusqu'a expiration (15 min par defaut, 60 max) ; `DeleteAsync` sans revocation des refresh tokens | Claim SecurityStamp + `OnTokenValidated`, revoquer a la desactivation/suppression |
| 🟠 | `RateLimitingMiddleware.cs:13,27` + `Program.cs:154` | Rate-limit unique global en memoire (200/min/IP), aucun palier sur `/api/Account`, `KnownProxies` `?? []` sans fail-fast (contraire au CORS) → derriere proxy non declare, seau unique partage | Palier `/api/Account` (10/min), fail-fast ForwardedHeaders, `AddRateLimiter` natif |
| 🟡 | `AccountService.cs:239` | `ResetPassword` renvoie « Utilisateur introuvable » → enumeration de comptes, alors que `ForgotPassword`/`ResendConfirmation` sont durcies | Message generique + delai aleatoire uniforme |
| 🟡 | `Program.cs:59` | Politique mot de passe `RequiredLength = 6` (< NIST 8) sur donnees patrimoniales ; seuil duplique dans le ViewModel | Porter a 10-12, relacher les regles de composition, source unique |
| 🟡 | `ScreenerCsvWriter.cs:44` + `AdminKpiCsvWriter.cs:93` | Export CSV sans neutralisation des prefixes de formule (`=`,`+`,`-`,`@`) sur donnees provider + un champ `CompanyName` pilotable client | Prefixer `'` les valeurs a risque, helper partage |
| 🟡 | `AnalyticService.cs:76,114` | IP d'audit lue depuis `X-Forwarded-For` brut (falsifiable) alors que `RemoteIpAddress` est normalise ; sanitisation par denylist de 4 mots-cles | `context.Connection.RemoteIpAddress`, allowlist |

### 4.8 Correctness
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `SignalOutcomeEvaluationJob.cs:151,177` + `SignalDirectionalScanEvaluator.cs:68` | Scan ex-post inclut la bougie du jour d'emission (`>= windowStart`) : un extreme intraday **anterieur** au signal compte comme hit → biais de look-ahead bidirectionnel dans le taux de reussite affiche (Wilson) | `windowStart = ...Date.AddDays(1)` ou `> evaluationStartUtc` ; test back justifie |
| 🟠 | `PortfolioHoldingCalculator.cs:55,118` + `PortfolioContextLoader.cs:154` + `TaxService.cs:116` + `ClientFinanceInstrumentDetailService.cs:148` | Prix de revient calcule par 4 algos (FIFO frais inclus x2, PMP sans frais fiscal, moyenne brute ignorant les ventes) ; repli `Math.Max(buy - sell, 0)` soustrait un produit de vente d'un cout | Composant FIFO unique consomme partout ; `TaxCostBasis` explicite ; corriger le repli |
| 🟠 | `AnalysisSnapshotPersistenceService.cs:120,142` | `ChangeTracker.Clear()` efface les modifs en attente de toute la requete (voir P3, recoupe DB-03) | Idem P3 |
| 🟠 | `BaseService.cs:45,56` | Requete EF synchrone dans le constructeur → exception pendant la resolution DI, I/O bloquante, champ mort (voir P2) | Supprimer |
| 🟠 | `PortfolioRiskMetricsService.cs:203,239` | Max drawdown calcule sur la valeur brute du portefeuille (jours de transaction inclus) alors que les rendements les excluent : un retrait = -50% attribue au marche. Le front affiche « jours de transaction exclus » (faux) | Serie base 100 des rendements nets de flux |
| 🟠 | `Indicators/TechnicalIndicatorsService.cs` vs `Common/.../TechnicalIndicators.cs` | RSI/MACD/EMA 2x, bords divergents (voir 4.3, recoupe CORR-09/DUP-01/LAYER-03) | Unifier |
| 🟠 | `RateLimitingMiddleware.cs:27,31,41` | Compteur read-modify-write non atomique sur `IMemoryCache` : sous concurrence le compteur sous-compte ; cle `RateLimit_` partagee si IP nulle. Lockout Identity couvre le bruteforce login | `AddRateLimiter` natif ou `Interlocked` (classe MINEUR en pratique) |
| 🟡 | `YahooFinanceMarketDataProvider.cs:465,470` | 5 tableaux OHLCV indexes sur la longueur d'un 6e sans reconciliation ; `GetProperty` leve `KeyNotFoundException` non couverte par les filtres de repli → 500 sur reponse Yahoo degradee | `TryGetProperty` + `Math.Min` des longueurs |

### 4.9 Contrats / ViewModels
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `AdminSnapshotAuditService.cs:140,156` | L'audit lit `RawPayload` en PascalCase (`TryGetProperty` case-sensitive) alors qu'il est serialise en camelCase (`JsonSerializerDefaults.Web`) → TraceId/PatternId/Recommendation **vides pour 100% des analyses**, silencieux. `CompareAsync` renvoie « identique » pour tout couple | Deserialiser via `AnalysisSnapshotJsonOptions.Shared` (comme le chemin sain) |
| 🟠 | `screener-page.component.ts:120,213` + `ScreenerQueryViewModel.cs` | Filtre metier `minScore` execute en TypeScript (absent du contrat serveur) : ne filtre que la page courante, pagination sur total non filtre → « 450 resultats » + tableau vide, verite metier dans le rendering layer | Ajouter `MinScore` au DTO, filtrer avant pagination cote back |
| 🟠 | `FundamentalScoreRequestViewModel.cs:6` + `FundamentalScoringService.cs:119` | Un client `Bearer` peut envoyer `CoveragePenaltyEnabled: false` et desactiver la penalite de couverture gouvernee ; `Symbols` non borne ; le flag de politique `CoveragePenaltySupported` existe mais n'est jamais lu | Retirer les flags du DTO client, resoudre depuis la politique active, `[MaxLength]` sur `Symbols` |
| 🟠 | `Program.cs:176` + profils AutoMapper | 22 profils pour 154 constructions manuelles, aucune regle ; `AssertConfigurationIsValid` jamais appele (champ ajoute → null silencieux) ; regle monetaire `NetAmount` dupliquee profil/service (GET vs POST) ; `CreateMap<UserViewModel, User>` mort | Sortir le calcul metier des profils, `AssertConfigurationIsValid` au boot |
| 🟠 | `ApiErrorInterceptor.ts:71` + `ExceptionMiddleware .cs:87` | Erreurs `DataAnnotations` (dictionnaire `ValidationProblemDetails`) jamais lues par le front (attend `errors` tableau + `message`) → « Requete invalide » au lieu du message reel ; champ `statusCode` fantome ; 12 DTO sans annotation | `InvalidModelStateResponseFactory` produisant la forme du middleware |
| 🟡 | `client-domain-metadata.ts:50` + wording back | Libelles de phase + messages d'erreur dupliques en TS (le back les expose deja/gouverne) ; seuils 70/40 de score en dur cote front | Consommer `PhaseLabel`, exposer une bande de score depuis la politique |
| 🟡 | `TradingPredictionCompatibilityViewModel.cs:5` + 4 autres | 5 ViewModels orphelins (30 props inutilisees, profil AutoMapper mort, doublon `AdminUserUpdate`/`AdminUserUpsert`) | Supprimer les 8 fichiers |

### 4.10 Tests / CI
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `SignalOutcomeEvaluationJobLogicTests.cs:52` + 3 fichiers | ~1400 lignes de tests fantomes : `EvaluateStaticAsync`/`UpsertCandlesAsync`/`ResolveTrackedAssetIdsAsync` **recopient** la logique de prod sans jamais l'appeler. Le clone a deja diverge (scan directionnel vs boucle naive) : les tests valident une logique qui n'existe plus. `SmokeBuildChangeTests` asserte sur du LINQ local | Extraire des classes pures injectees, reecrire les tests dessus, supprimer le smoke |
| 🟠 | `RecommendationPolicyService.cs:30` + `TradingRecommendationService.cs` | Le coeur metier (verdict Buy/Sell/Hold, `ResolveRecommendationKind`, seuil 0.60) n'a **aucun test** ; seulement mocke. 251 tests ailleurs | Table de decision parametree, assertion sur `SituationCode`/`RecommendationAction` |
| 🟠 | `RectangleContinuation...cs` + 3 continuations | Les 4 patterns cibles V1 n'ont **aucun test de detection** ; seuls les 4 retournements non-cibles en ont (904 lignes) | Porter le patron `DoubleBottomPatternTests` sur les 4 continuations |
| 🟠 | `Directory.Build.props:16` | S1144 (membre inutilise), S125 (code commente), S1450 neutralisees globalement sur les 7 projets ; justifiees par un commentaire « aucun defaut » que `BuildJob` (methode morte) contredit. `ci-cd.md:18` affirme « any analyzer warning fails » | Suppressions locales `#pragma`, retirer S1144/S125/S1450 au moins pour Tests |
| 🟡 | `staging/prod-pipeline.yml:38` | 6 tests exclus en permanence par `--filter !~` ; justification fausse (3 classes mono-test, pas de « voisin sain ») → gate vert sur neutralisation watchlist + wording deterministe | Seeder les donnees manquantes, retirer le filtre ou `[Fact(Skip)]` traçable |
| 🟡 | `staging-pipeline.yml` vs `prod-pipeline.yml` | 2 pipelines back identiques a 2 lignes pres (102 dupliquees), idem front → toute correction de gate a appliquer 4x | Template Azure DevOps unique (`extends`) |
| 🟡 | `TestInfrastructure.cs:54` + 20 fichiers | Bootstrap DbContext in-memory recopie 20x alors qu'un helper existe (1 seul appelant) ; fabriques `BuildUser`/`BuildAsset` copiees | Helper parametre unique (attention : seed `EnsureCreated` + nom de base) |
| 🟡 | `ClientFinanceMilestone3ApiFeatureTests.cs:13` + 8 fichiers | Tests de plumbing : passe-plats de controller thin, verification d'enregistrements DI, assertion sur LINQ local ou EF InMemory | Supprimer smoke/persistence-model, reduire les passe-plats a 1 cas |

### 4.11 Provider marche (Yahoo)
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `YahooFinanceMarketDataProvider.cs:718-727` + `AdminInstrumentSeedService.cs:104` + `AssetEnrichmentJob.cs:147-149` | **[Corrige apres relecture]** `Asset.Country` recoit 2 vocabulaires selon l'origine de l'actif : code ISO `"FR"` pour les ~40 actifs du seed CAC 40, nom complet `"France"` (table `NormalizeCountry`) pour tout actif ajoute hors seed (recherche libre puis watchlist). `AssetEnrichmentJob.cs:147` ne remplit `Country` que si `string.IsNullOrEmpty(asset.Country)` : ce n'est **pas** un ecrasement inconditionnel des valeurs seedees (elles restent ISO et ne sont jamais touchees), mais une coexistence durable des deux formats selon le chemin d'ajout de l'actif. `Instrument.CountryCode` (`AnalysisSnapshotPersistenceService.cs:176`) recopie `asset.Country` tel quel, donc herite du meme melange ; facettes screener dupliquees, filtre `Contains` rate une forme selon l'actif | Stocker ISO-2 dans `CountryCode` a l'ecriture (traduire la sortie Yahoo vers ISO au lieu de l'inverse), traduire au rendu uniquement, supprimer `NormalizeCountry` du provider |
| 🟠 | `YahooFinanceMarketDataProvider.cs:254,314,393` | Bloc quoteSummary (URL+envoi+erreur+validation, ~22 lignes) duplique 3x a l'identique | Helper `FetchQuoteSummaryResultAsync(symbol, modules, label, ct)` |
| 🟠 | `MarketFundamentalData.cs:7` + `MarketAssetProfileData.cs:7` | 13 champs de profil identiques dans 2 DTO ; mapping recopie (l.285 vs l.349). `Sector` sert de cle de bucket scoring → correction unilaterale change le scoring | Composition `MarketFundamentalData { Profile; ... }`, mapping unique |
| 🟠 | `YahooFinanceMarketDataProvider.cs:65,384` + `EtfReadingViewModel.cs:25` | Chaine ETF morte de bout en bout (interface + methode 57 l. + DTO + ViewModel + profil AutoMapper + DI) — dormant documente (`Doc/v1` A-05) mais trompeur | Supprimer ou brancher explicitement |

### 4.12 Moteur de patterns
| Sev | Fichier:ligne | Probleme | Correction |
|---|---|---|---|
| 🟠 | `ReversalPatternDefinitionBase.cs` + `ContinuationPatternDefinitionBase.cs` | Deux bases jumelles : ~240 lignes identiques dont `BuildNoMarketDataArtifact` (83 l. verbatim), `BuildRiskRewardRatio`, `BuildConfidenceLabel` (paliers 0.80/0.60/0.35) | Base commune `PatternDefinitionBase<TState>` |
| 🟠 | `InverseHeadAndShouldersReversalPatternDefinition.cs:111,139` | Critere de profondeur de tete = tautologie : `depthRatio = (avgShoulder - headLow) / figureHeight` avec `figureHeight = avgShoulder - headLow` → toujours 1, ne rejette jamais. Le miroir H&S filtre correctement → detection inverse plus permissive, portant la fiabilite 0.71 | Aligner sur H&S (neckline d'abord) |
| 🟠 | `DoubleTopReversalPatternDefinition.cs:81,96,122,220` | Tout le score additif est mort (plafond 0.70 < `Math.Max(...,0.80)`) → confidence constante ; bonus volume ignore (present cote double bottom) ; cas non-detecte 0.15 vs 0.50. Confidence sert de cle de tri | Adopter le switch par phase du double bottom |
| 🟠 | `DoubleTopReversalPatternDefinition.cs:186` | `intermediateRebound = (high1 - neckline) / figureHeight` mesure le 1er sommet, pas le creux intermediaire → ~1, seuil 10% jamais atteint. (Le double bottom a le meme defaut structurel : les deux filtres sont inertes) | `ComputeIntermediateLow`, seuil effectif |
| 🟠 | `RectangleContinuation...cs:238` + `SymmetricalTriangle...cs:236` | `ResolveDirectionalTrend` + enum `DirectionalTrend` dupliques a l'identique ; magic number `8` en dur (le multiple ATR voisin est centralise) | Deplacer dans `PatternTechnicals`, externaliser `PriorTrendMinCandles` |
| 🟠 | `PatternThresholds.cs:106` + `DoubleTop/Bottom` | `DoubleMinSeparationAtrMultiple` documente « multiple d'ATR » mais utilise comme nombre de bougies (`(int)Math.Ceiling`), jamais multiplie par ATR ; comparaison `<` vs `<=` divergente entre figures miroir | Renommer `DoubleMinSeparationBars` (int), harmoniser le comparateur |
| 🟡 | `ReversalPatternAnalysisState.cs:8` vs `Continuation...cs:8` | 18 props identiques, seule difference `NecklinePrice`/`ReferencePrice` (meme champ de sortie) ; `HeadIndex` ecrit jamais lu ; 5 index mutuellement exclusifs | Fusionner ou base commune |
| 🟡 | `PatternThresholds.cs:3` | Se presente comme la centralisation des seuils mais toutes les tailles de fenetre (22/12, 24/20) restent en dur et dupliquees entre figures miroir | Remonter les fenetres et planchers dans le fichier |
| 🟡 | `AnalysisPatternRegistry.cs:83,100,111` | `NormalizePatternId` reimplemente `PatternIds.Normalize` ; bloc de filtrage des patterns actifs ecrit 2x a 15 lignes | Deleguer a `PatternIds.Normalize`, extraire `ResolveEnabledDefinitions()` |

---

## 5. Etat de la documentation

La documentation est **activement trompeuse sur 4 points de contrat**, ce qui est pire qu'une doc absente : un nouvel arrivant qui la suit echoue ou tire de fausses conclusions.

- **README.md** : l'ordre de lecture recommande (`:47-49`) pointe 3 fichiers inexistants (`Doc/product/*` archives sous `_legacy/`) ; l'onboarding casse a l'etape 2. Compte de controllers faux. `REMEDIATION-SUMMARY.md` reference 6x et absent.
- **AGENTS.md** (contrat liant, 833 lignes, globalement rigoureux) : `:469` designe comme « carte canonique de l'autorite documentaire » un fichier archive — la notion d'autorite documentaire pointe dans le vide. `:441` a raison de flaguer `/api/Trading/predict/{symbol}` comme retiree, mais la route existe toujours en code (`TradingController.cs:17`). Point d'attention : AGENTS.md **ne parle jamais** de `IsDeleted`, `PagedListViewModel` ni de pagination — ces regles viennent de `.claude/CLAUDE.md`, et `PagedListViewModel<T>` **n'existe pas** dans le repo (le type reel est `PagedResultViewModel<T>`, utilise 1 fois).
- **Documentation/ (wiki)** : `deployment.md:20` ment sur l'application des migrations au boot (le code les applique partout sauf Testing) ; `local-setup.md`/`secrets-management.md` decrivent un `appsettings.json` commite inexistant et une regle inapplicable ; `data-model.md`/`pitfalls.md` presentent le soft-delete comme absolu et omettent `AssetTransaction` ; `api-endpoints.md` donne 30 controllers (reel 32) et se contredit avec son propre index ; `.claude/CLAUDE.md:114` (injecte a chaque agent) pointe 3 chemins morts.
- **Doc/v1/** : `06_ecarts_doc_code.md` declare l'ecart RGPD A-10 « ouvert, CRITIQUE » alors qu'il est implemente et tournant (`AnalyticsRetentionJob`) — le registre d'ecarts, cense etre « le seul endroit honnete », publie un faux blocage legal.

**Actions doc, par ordre** : (1) `git rm` les zips et corriger `.gitignore` narratif ; (2) repointer README/AGENTS/CLAUDE vers `Doc/v1/` et supprimer les 6 refs `REMEDIATION-SUMMARY.md` ; (3) reecrire `deployment.md` sur le comportement reel du boot ; (4) retirer A-10 du registre ; (5) generer `api-endpoints.md` depuis le code ou renvoyer a Swagger ; (6) check de liens morts markdown en CI.

---

## 6. Plan de remediation sequence

### Vague 1 — Quick wins (< 1 jour chacun, sans dependance)
| Item | Effort | Note |
|---|---|---|
| `ORG-01` supprimer les 2 zips + rotationner secrets dev | S | Independant, urgent |
| `SEC-02`/`CORR-07` supprimer `BaseService.cs:56` + champ mort | S | Retire une requete SQL bloquante par service instancie |
| `C2`/`ORG-04`/`DB-11` supprimer `Datas/Common` mort + `// Je t'aime ChatGPT` | S | Aucun appelant |
| `MKT-04` supprimer la chaine ETF morte | S | Aucun appelant |
| `API-11` supprimer 5 ViewModels orphelins + doublon admin | S | Aucun appelant |
| `LAYER-10`/`ORG-10` retirer DI/packages morts (`IPathService`, `IEtfProfileProvider`, `BCrypt` x2), declarer `Polly` | S | — |
| `ORG-03` `git mv` fichiers avec espace | S | — |
| `C4`/`DOC-04`/`DOC-03` corriger commentaires/docs faux (BulkowskiReliability, DOUBLE_TOP, A-10) | S | — |
| Documentation : liens morts README/AGENTS/CLAUDE (voir §5) | M | Debloque l'onboarding |

### Vague 2 — Structurel (apres V1)
| Item | Effort | Ordre / dependance |
|---|---|---|
| `DB-04` FK `Restrict` + soft-delete `UserAsset` | M | Perte de donnees — prioritaire dans cette vague |
| `DB-03`/`CORR-04` isoler upsert bougies + transaction | M | Corrige P3 |
| `DB-05` `HasQueryFilter` + `ISoftDeletable` | M | Apres avoir cartographie les seeders non filtres |
| `DB-01` dedup analyse cote SQL | M | — |
| `API-01` deserialiser le RawPayload d'audit correctement | S | Bug silencieux 100% |
| `API-04` remonter `minScore` dans le contrat serveur | M | Front |
| `API-05` retirer les flags de politique du DTO client | S | — |
| `API-08` normaliser la forme d'erreur 400 (`InvalidModelStateResponseFactory`) | M | — |
| `SEC-01`/`SEC-04`/`SEC-05` durcir auth (KDF, revocation, rate-limit palier) | M-M-M | Independants |
| `CI-05` retirer S1144/S125/S1450 des Tests + `AssertConfigurationIsValid` | S | Rend V3 detectable |

### Vague 3 — Fond (dette structurelle, apres V2)
| Item | Effort | Ordre / dependance |
|---|---|---|
| `DUP-01`/`CORR-09`/`LAYER-03` unifier RSI/MACD/EMA | M | Prealable aux tests d'indicateurs |
| `CORR-02` composant FIFO unique de prix de revient | L | Verite metier ; `TaxCostBasis` explicite |
| `MKT-01` un seul vocabulaire de pays (`CountryCode` ISO) | M | Migration de normalisation |
| `PAT-01`/`PAT-02` base commune `PatternDefinitionBase<TState>` | L | **Prealable** a la mutualisation des filtres |
| `PAT-03`/`PAT-04`/`PAT-05`/`PAT-09` corriger les criteres inertes/morts | M | Apres PAT-01 (methode de profondeur partagee) |
| `PAT-06`/`PAT-07`/`PAT-11` centraliser seuils/normalisation | S-M | — |
| `LAYER-06`/`ORG-06` migrer `Analysis/` vers les 9 capabilities | L | Capability par capability |
| `LAYER-01`/`DUP-04` desengager `BaseService` (injection explicite) | L | Big-bang interdit ; regle « pas de nouveau descendant » d'abord |
| `TEST-01`/`TEST-02`/`TEST-03` reecrire les tests fantomes + couvrir recommandation & patterns cibles | L | Apres extraction des classes pures (PAT-01, LAYER-06) |
| `DUP-05`/`DUP-06` unifier pagination + seed | M | — |

**Chaine de dependances critique** : `PAT-01/02` (base commune) → `PAT-03/04/05` (corrections de filtres) → `TEST-03` (tests des 4 patterns cibles). Et : `LAYER-06` (frontieres capability) + extraction de classes pures → `TEST-01/02`. Ne pas attaquer les tests avant d'avoir rendu le code testable.

---

## 7. Ce qui va bien

Points d'architecture reellement solides, verifies :

- **Le graphe de dependances entre projets est propre et acyclique** : `Common` est une feuille sans aucune reference, la direction `Common → Patterns/Datas → ViewModels → Services → API → Tests` est respectee (`.csproj` lus). Pas de projet fantome, pas de cycle.
- **AGENTS.md est un vrai contrat de travail** (833 lignes), precis, avec une classification de preuve (`PROVEN`/`DECIDED`/`PROPOSED`/`DEROGATION`) et des regles nommees et exigibles. La plupart des findings sont des ecarts **par rapport a ce contrat**, ce qui prouve que le contrat est utile.
- **Le pipeline de resilience marche est bien fait** : `AddHttpClient<YahooFinanceMarketDataProvider>` avec retry exponentiel + jitter, circuit breaker et timeout pilotes par `MarketDataOptions` (`ProgramServiceExtensions.cs:51-81`) — c'est de l'ingenierie correcte.
- **Le modele EF est riche et coherent** : index unique la ou il faut (`Symbol`, `TokenHash`, `(AssetId,Interval,TimestampUtc)`, `(AnalysisRunId)` sur les 1-1), contrainte CHECK sur `Quantity >= 0`, precisions decimales explicites (18,8), FK `Restrict` sur `AssetTransaction → Portfolio` (le bon choix — c'est la FK `Cascade` sur `UserAsset` qui est le probleme).
- **La securite de base est presente** : JWT avec toutes les validations actives (`RequireHttpsMetadata`, `ClockSkew` 20s), CORS fail-fast si liste vide, `SecurityHeadersMiddleware`, HSTS, PBKDF2 Identity, lockout apres 5 essais. Les manques sont des durcissements, pas des trous beants.
- **Le determinisme du moteur d'analyse est une vraie intention produit** : versionnement (`ModelVersion`, `FundamentalScoringPolicyVersion`, `RecommendationWordingVersion`), separation code de situation / wording, registre PEA comme source de verite. L'appareil de gouvernance existe — le probleme est qu'il est parfois contourne (wording en dur, flag `CoveragePenaltySupported` non lu), pas absent.
- **Le no-market-data / degraded mode est explicitement modelise** (`DegradedModeState`, `FallbackPatternMarketDataProvider`) : l'architecture anticipe l'indisponibilite du provider.
