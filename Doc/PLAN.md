# PredictFinance - Plan MVP solide (termine)

Date de cloture: 2026-03-11

## Objectif

Clore le plan MVP "solide" en validant un socle coherent sur 3 blocs:
- FinanceAPI (.NET 10)
- FinanceFront (Angular)
- FinanceIA (Python)

## Livrables termines

### 1) Domaine et persistance
- [x] Extension du domaine finance avec support explicite des analyses persistantes:
  - `AnalysisBatch`
  - `AnalysisRun`
  - `PatternAssessment`
  - `DecisionSignal`
  - `ModelSnapshot`
  - `AssetQuoteSnapshot`
  - `AssetCandleSnapshot`
- [x] Extension du modele `Asset` pour les types d'instruments (stock/etf).
- [x] Enregistrement EF Core centralise dans le `FinanceDbContext`.

Fichiers clefs:
- `FinanceAPI/BackPredictFinance.Datas/Entities/*.cs`
- `FinanceAPI/BackPredictFinance.Datas/Context/FinanceDbContext.cs`
- `FinanceAPI/BackPredictFinance.Datas/Context/ModelBuilderConfigurationExtensions.cs`

### 2) Contrats API et couche service
- [x] Ajout des nouveaux view models client pour profil et chart d'actif.
- [x] Evolution du mapping de resultat d'analyse avec phase, confiance, statut modele, signal actionnable.
- [x] Consolidation des contrats Python cote API (predict/simulate/quality gate).

Fichiers clefs:
- `FinanceAPI/BackPredictFinance.ViewModels/ClientFinanceViewModels/*.cs`
- `FinanceAPI/BackPredictFinance.Services/PythonServices/Models/*.cs`
- `FinanceAPI/BackPredictFinance.Services/TickerService.cs`

### 3) IA modulaire et output metier
- [x] Ajout d'une structure patterns/runtime cote IA.
- [x] Enrichissement de la detection Double Top avec phases metier.
- [x] Adaptation des modeles predict/simulate pour des sorties exploitables cote produit.
- [x] Couverture tests IA alignee (runtime + guardrails + smoke).

Fichiers clefs:
- `FinanceIA/src/finance_ia/patterns/*`
- `FinanceIA/src/finance_ia/runtime/*`
- `FinanceIA/src/finance_ia/features/double_top.py`
- `FinanceIA/src/finance_ia/model/predict.py`
- `FinanceIA/src/finance_ia/model/simulate.py`
- `FinanceIA/tests/*`

### 4) Front client/admin
- [x] Adaptation des ecrans admin/client pour les nouveaux champs d'analyse et de simulation.
- [x] Evolution du service Angular de finance client vers les nouveaux contrats.

Fichiers clefs:
- `FinanceFront/src/app/components/admin/admin-analyse-finance/*`
- `FinanceFront/src/app/components/client/user-finance/*`
- `FinanceFront/src/app/services/client-finance.service.ts`

### 5) CI/CD IA
- [x] Pipeline Azure aligne sur Python 3.12 (coherent avec `pyproject.toml`).
- [x] Ajout d'un stage `Quality Gate` bloquant avant build/package.
- [x] Packaging nettoye (venv non archive).
- [x] Deploy durci (fallback `python3` si `python3.12` absent).

Fichier clef:
- `FinanceIA/azure-pipelines.yml`

## Validation effectuee

- [x] `dotnet build FinanceAPI/BackPredictFinance.sln -nologo` reussi.
- [x] `dotnet test FinanceAPI/BackPredictFinance.Tests/BackPredictFinance.Tests.csproj --no-build` execute (0 test detecte).
- [x] `npx tsc -p tsconfig.app.json --noEmit` reussi.
- [x] `npx tsc -p tsconfig.spec.json --noEmit` reussi.
- [x] `FinanceIA: .venv\\Scripts\\python.exe -m pytest tests -q -p no:cacheprovider` reussi (20 passed).

## Backlog post-plan (non bloquant MVP)

1. Migrations EF Core dediees aux nouvelles entites d'analyse.
2. Secret management prod (JWT, credentials serveur) hors fichiers versionnes.
3. CORS whitelist stricte par environnement et revue rate limiting.
4. Remediation dependance vulnerable `MimeKit 4.14.0`.
5. Ajout de vrais tests API (auth refresh rotation, isolation multi-user, roles).
