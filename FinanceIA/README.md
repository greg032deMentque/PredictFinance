# FinanceIA MVP: Double Top binaire

Refonte IA propre et minimaliste pour predire un evenement de type `Double Top`.

## Objectif

- cible unique: `target=1` au second pic d'un Double Top detecte par regles
- features techniques simples sans fuite temporelle
- modele unique: `LightGBMClassifier`
- split strictement temporel (pas de split aleatoire)

## Arborescence

- `src/finance_ia/config.py`: config centrale + dataclasses (`TrainConfig`, `PatternConfig`)
- `src/finance_ia/data/yahoo.py`: telechargement OHLCV via yfinance
- `src/finance_ia/features/indicators.py`: indicateurs (retours, SMA, EMA, RSI, MACD, volatilite, volume)
- `src/finance_ia/features/double_top.py`: detection/labelisation Double Top
- `src/finance_ia/dataset/build_dataset.py`: assemblage dataset multi-tickers + split temporel
- `src/finance_ia/model/train.py`: entrainement
- `src/finance_ia/model/evaluate.py`: metriques de classification
- `src/finance_ia/model/predict.py`: inference ticker
- `src/finance_ia/io/artifacts.py`: sauvegarde/chargement artefacts
- `src/finance_ia/cli/train.py`: CLI entrainement
- `src/finance_ia/cli/predict.py`: CLI prediction
- `main.py`: wrapper minimal `train|predict`

## Installation (Windows PowerShell)

Depuis `FinanceIA/`:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -e .
pip install -e .[dev]
```

`Activate.ps1` active l'environnement virtuel local et isole les dependances du projet.

## Commandes

### Entrainement

```powershell
python -m finance_ia.cli.train --output-dir artifacts/double_top --tickers AAPL MSFT NVDA AMZN GOOGL META JPM XOM --start 2018-01-01 --end 2025-12-31
```

Ce que fait la commande:

1. telecharge les prix journaliers OHLCV
2. calcule les indicateurs techniques
3. labelise le `Double Top` (second pic)
4. concatene les donnees de tous les tickers
5. supprime les lignes invalides (`NaN` warmup)
6. separe train/test dans l'ordre chronologique
7. entraine LightGBM (`class_weight='balanced'`)
8. calcule les metriques de test
9. sauvegarde les artefacts

Artefacts generes dans `artifacts/double_top/`:

- `model.joblib`: modele LightGBM entraine
- `feature_columns.json`: colonnes et ordre des features attendues
- `metrics.json`: qualite du modele sur le jeu de test
- `train_config.json`: config complete utilisee a l'entrainement

### Prediction

```powershell
python -m finance_ia.cli.predict --ticker AAPL --model-dir artifacts/double_top --period 6mo
```

Ce que fait la commande:

1. charge `model.joblib` et `feature_columns.json`
2. recupere les donnees recentes du ticker
3. recalcule les features
4. infere les probabilites sur chaque fenetre valide
5. agrege et renvoie un JSON

Sortie JSON:

- `schema_version`: version du contrat de sortie
- `pattern`: pattern score (`double_top`)
- `ticker`: ticker demande
- `as_of`: date de la derniere observation utilisee
- `mean_prob`: moyenne des probabilites
- `max_prob`: maximum des probabilites
- `last_prob`: probabilite la plus recente
- `n_windows`: nombre de lignes d'inference

### Wrapper `main.py`

```powershell
python main.py train --output-dir artifacts/double_top
python main.py predict --ticker AAPL --model-dir artifacts/double_top
```

## Tests

```powershell
pytest tests -q
```

Couverts:

- detection `Double Top`
- calcul des indicateurs
- coherence `X/y` dataset
- persistance/chargement artefacts
- prediction JSON
- anti-fuite temporelle
- smoke CLI train + predict

## Limites MVP

- pattern unique (`Double Top`) et label binaire
- pas d'Optuna/CNN/stacking/MLflow
- scope API FastAPI exclu de cette iteration
