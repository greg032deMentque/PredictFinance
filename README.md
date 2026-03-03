# PredictFinance

Projet MVP pour analyser une valeur boursiere avec une IA et recommander une action simple.

## Contexte projet

- On veut creer une IA qui predit si une valeur boursiere suit un pattern en particulier.
- Le user selectionne une valeur, lance l'analyse, puis l'IA estime le signal et propose quoi faire.
- Le code doit rester simple techniquement: lisible, testable, maintenable.
- Chaque bloc (`FinanceFront`, `FinanceAPI`, `FinanceIA`) a son propre README qui explique son role.

## Sequence fonctionnelle cible

1. Connexion utilisateur.
2. Arrivee sur la vue dashboard.
3. Selection d'une valeur boursiere.
4. Analyse par l'IA + prediction + action recommandee.
5. Affichage du resultat sur le front.

## Architecture

- `FinanceFront`: interface Angular (login + dashboard + affichage prediction).
- `FinanceAPI`: API .NET (auth JWT + refresh token + orchestration vers l'IA Python).
- `FinanceIA`: moteur Python (entrainement + prediction de pattern Double Top).

## Demarrage rapide

### 1) IA (obligatoire pour les predictions)

```powershell
cd .\FinanceIA
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e .
pip install -e .[dev]
```

### 2) API

```powershell
cd ..\FinanceAPI
dotnet restore
dotnet build
dotnet run --project BackPredictFinance.API
```

API locale par defaut: `http://localhost:5298`.
Pour forcer HTTPS: `dotnet run --project BackPredictFinance.API --launch-profile https`.

### 3) Front

```powershell
cd ..\FinanceFront
npm install
npm start
```

Front local: `http://localhost:4200`.

## Qualite de code / SonarQube

Le projet suit des principes compatibles SonarQube:

- responsabilites claires par classe et par couche,
- gestion explicite des erreurs,
- noms explicites,
- duplication reduite,
- tests executes sur le module IA (`12 passed`) et compilation front/API validee.

## Readme par bloc

- `FinanceFront/README.md`
- `FinanceAPI/README.md`
- `FinanceIA/README.md`
