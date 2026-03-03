# FinanceIA MVP: Double Top binaire

Moteur IA de PredictFinance pour detecter un pattern `Double Top` et produire une recommandation exploitable par l'API.

## Role du bloc

`FinanceIA` est responsable de:

- preparer les donnees boursieres,
- calculer les features techniques,
- entrainer le modele,
- produire une prediction JSON standardisee pour l'API .NET.

## Documentation du code (partie par partie)

### 1) Configuration

- `src/finance_ia/config.py`
  - centralise les dataclasses de configuration (`TrainConfig`, `PatternConfig`),
  - definit les parametres de train/predict (periode, tickers, seuils, etc.).

### 2) Acces donnees

- `src/finance_ia/data/yahoo.py`
  - telecharge les series OHLCV via `yfinance`,
  - normalise les colonnes pour le pipeline.

### 3) Feature engineering

- `src/finance_ia/features/indicators.py`
  - calcule les indicateurs techniques (returns, SMA/EMA, RSI, MACD, volatilite, volume).

- `src/finance_ia/features/double_top.py`
  - detecte/labelise le pattern `Double Top`.
  - produit la cible binaire (`target=1` sur le second pic).

### 4) Dataset

- `src/finance_ia/dataset/build_dataset.py`
  - assemble les donnees multi-tickers,
  - nettoie les lignes invalides,
  - applique un split temporel strict (pas de fuite futur).

### 5) Model

- `src/finance_ia/model/train.py`
  - entraine `LightGBMClassifier`.

- `src/finance_ia/model/evaluate.py`
  - calcule les metriques (ROC-AUC, precision, recall, F1, confusion matrix).

- `src/finance_ia/model/predict.py`
  - charge les artefacts,
  - prepare les features d'inference,
  - calcule les probabilites de pattern.

### 6) Artefacts

- `src/finance_ia/io/artifacts.py`
  - sauvegarde/charge `model.joblib`, `feature_columns.json`, `metrics.json`, `train_config.json`.

### 7) CLI

- `src/finance_ia/cli/train.py`
  - point d'entree entrainement.

- `src/finance_ia/cli/predict.py`
  - point d'entree prediction (JSON stdout pour l'API).

- `main.py`
  - wrapper simplifie (`train` / `predict`).

## Installation (Windows PowerShell)

Depuis `FinanceIA/`:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -e .
pip install -e .[dev]
```

## Commandes

### Entrainement

```powershell
python -m finance_ia.cli.train --output-dir artifacts/double_top --tickers AAPL MSFT NVDA AMZN GOOGL META JPM XOM --start 2018-01-01 --end 2025-12-31
```

### Prediction

```powershell
python -m finance_ia.cli.predict --ticker AAPL --model-dir artifacts/double_top --period 6mo
```

Sortie JSON principale:

- `ticker`
- `as_of`
- `mean_prob`
- `max_prob`
- `last_prob`
- `n_windows`

## Tests

```powershell
.\.venv\Scripts\python.exe -m pytest -q
```

Statut actuel: `12 passed`.

## Integration avec l'API .NET

L'API appelle la CLI Python pour:

- `GET /api/Trading/predict/{symbol}`
- `POST /api/Trading/predict`

Le front recoit ensuite:

- probabilites (`last/mean/max`),
- action (`buy`, `hold`, `sell`),
- justification textuelle.
