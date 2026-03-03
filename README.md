# PredictFinance

Ce projet contient une refonte IA simple et maintenable pour la prediction de patterns boursiers.

Le chemin actif est `FinanceIA` et le MVP se concentre uniquement sur un cas:

- pattern `Double Top`
- cible binaire (`target=1` sur le second pic)
- un seul modele (`LightGBMClassifier`)

## Structure utile

- `FinanceIA/src/finance_ia/`: pipeline IA modulaire (data, features, dataset, model, io, cli)
- `FinanceIA/tests/`: tests unitaires + smoke tests
- `FinanceIA/main.py`: wrapper CLI minimal (`train` / `predict`)
- `FinanceIA/API/`: hors perimetre de cette refonte

## Demarrage rapide (Windows PowerShell)

Depuis la racine du repo:

```powershell
cd .\FinanceIA
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -e .
pip install -e .[dev]
```

`Activate.ps1` active l'environnement virtuel: les commandes Python utilisent alors les dependances locales du projet.

## Commandes principales

### 1) Entrainement

```powershell
python -m finance_ia.cli.train --output-dir artifacts/double_top --tickers AAPL MSFT NVDA AMZN GOOGL META JPM XOM --start 2018-01-01 --end 2025-12-31
```

Cette commande:

1. telecharge les donnees OHLCV Yahoo Finance
2. calcule les features techniques
3. detecte/labelise les `Double Top`
4. construit le dataset multi-tickers
5. applique un split temporel (train ancien, test recent)
6. entraine LightGBM (`class_weight='balanced'`)
7. evalue le modele et sauvegarde les artefacts

Objets generes dans `--output-dir`:

- `model.joblib`: modele entraine
- `feature_columns.json`: ordre exact des features attendues en inference
- `metrics.json`: metriques de test (`roc_auc`, `precision`, `recall`, `f1`, confusion matrix, etc.)
- `train_config.json`: configuration complete d'entrainement (tickers, periode, hyperparametres)

### 2) Prediction

```powershell
python -m finance_ia.cli.predict --ticker AAPL --model-dir artifacts/double_top --period 6mo
```

Cette commande:

1. charge le modele et les colonnes de features depuis `--model-dir`
2. telecharge les dernieres donnees du ticker
3. recalcule les features
4. produit les probabilites du modele sur les fenetres valides
5. retourne un JSON de synthese sur stdout

Sortie JSON:

- `ticker`: ticker demande
- `as_of`: date de la derniere fenetre evaluee
- `mean_prob`: probabilite moyenne sur la periode
- `max_prob`: probabilite maximale observee
- `last_prob`: probabilite sur la derniere fenetre
- `n_windows`: nombre de fenetres utilisees

### 3) Wrapper simple

```powershell
python main.py train --output-dir artifacts/double_top
python main.py predict --ticker AAPL --model-dir artifacts/double_top
```

### 4) Tests

```powershell
pytest tests -q
```

Les tests couvrent detection `Double Top`, indicateurs, dataset, entrainement, prediction CLI, et anti-fuite temporelle.

## Limites connues du MVP

- un seul pattern (`Double Top`) et une seule cible binaire
- pas d'optimisation hyperparametres avancee
- pas de tracking MLflow
- pas de refonte FastAPI dans cette iteration
