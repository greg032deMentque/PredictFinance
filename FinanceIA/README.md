# FinanceIA MVP: Double Top binaire

Moteur IA de PredictFinance pour detecter un pattern `Double Top`, mesurer sa qualite, puis exposer une prediction exploitable par l'API.

## Role du bloc

`FinanceIA` fait 4 choses:

1. Charger des donnees boursieres (`yfinance`).
2. Transformer ces donnees en features techniques.
3. Entrainer et evaluer un modele binaire.
4. Predire sur une valeur et retourner un JSON standard.

## Comment l'entrainement fonctionne

Le pipeline est volontairement simple:

1. Build dataset multi-tickers.
2. Split temporel strict en `train` / `validation` / `test`.
3. Entrainement LightGBM sur `train`.
4. Choix automatique du meilleur threshold sur `validation` (max F1).
5. Evaluation finale sur `test`.
6. Sauvegarde des artefacts.

Important: relancer l'entrainement plusieurs fois sur les memes donnees ne rend pas le modele "magiquement" meilleur.  
Le gain vient surtout de:

- plus de donnees pertinentes,
- meilleurs labels/features,
- meilleure robustesse de l'evaluation.

## Comment savoir si les resultats sont bons

Verifier en priorite:

- `f1` (equilibre precision/recall),
- `precision` (qualite des signaux positifs),
- `recall` (combien de patterns reels sont detectes),
- `roc_auc` (separation globale),
- `confusion_matrix` (types d'erreurs).

Les metriques de train sont dans `artifacts/double_top/metrics.json`.

## Documentation du code (par module)

- `src/finance_ia/config.py`: dataclasses de config + mapping config pattern.
- `src/finance_ia/data/yahoo.py`: telechargement OHLCV.
- `src/finance_ia/features/indicators.py`: calcul des indicateurs techniques.
- `src/finance_ia/features/double_top.py`: labelisation du pattern.
- `src/finance_ia/dataset/build_dataset.py`: assemblage dataset + split temporel.
- `src/finance_ia/model/train.py`: entrainement + selection threshold + metriques.
- `src/finance_ia/model/evaluate.py`: calcul metriques + recherche threshold F1.
- `src/finance_ia/model/validate.py`: evaluation d'un modele deja entraine sur une plage de dates.
- `src/finance_ia/model/predict.py`: inference et agregation des probabilites.
- `src/finance_ia/model/simulate.py`: simulation d'investissement basee sur la prediction IA.
- `src/finance_ia/io/artifacts.py`: lecture/ecriture des artefacts.
- `src/finance_ia/cli/train.py`: commande train.
- `src/finance_ia/cli/evaluate.py`: commande evaluation.
- `src/finance_ia/cli/predict.py`: commande prediction.
- `src/finance_ia/cli/simulate.py`: commande simulation.
- `main.py`: wrapper `train|evaluate|predict|simulate`.

## Installation (Windows PowerShell)

Depuis `FinanceIA/`:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -e .
pip install -e .[dev]
```

## Utilisation simple (recommandee)

### 1) Entrainement

```powershell
python -m finance_ia.cli.train --output-dir artifacts/double_top --tickers AAPL MSFT NVDA AMZN GOOGL META JPM XOM --start 2018-01-01 --end 2025-12-31 --val-size 0.15 --test-size 0.2
```

### 2) Evaluation hors train (modele deja entraine)

```powershell
python -m finance_ia.cli.evaluate --ticker AAPL --model-dir artifacts/double_top --start 2025-01-01 --end 2025-12-31
```

Evaluation avec comparaison de plusieurs seuils:

```powershell
python -m finance_ia.cli.evaluate --ticker AAPL --model-dir artifacts/double_top --start 2025-01-01 --end 2025-12-31 --threshold-grid 0.10,0.15,0.20,0.30,0.40,0.50
```

Evaluation avec verdict metier automatique et log:

```powershell
python -m finance_ia.cli.evaluate --ticker AAPL --model-dir artifacts/double_top --start 2025-01-01 --end 2025-12-31 --threshold-grid 0.05,0.10,0.15,0.20,0.30,0.40,0.50 --min-precision 0.10 --min-recall 0.05 --min-f1 0.08 --min-roc-auc 0.60 --min-rows 200 --min-positive-samples 5
```

Le rapport JSON est sauvegarde automatiquement dans un dossier dedie par execution:

- `artifacts/evaluation/evaluation_<GUID>_<YYYYMMDD>_<N>/evaluation_<TICKER>.json`
- `report_file` dans la sortie pointe vers ce fichier.
- `N` repart a `1` chaque nouvelle journee.

Pour voir les derniers runs rapidement (PowerShell):

```powershell
Get-ChildItem artifacts/evaluation -Directory | Sort-Object Name -Descending | Select-Object -First 10
```

### 3) Prediction operationnelle

```powershell
python -m finance_ia.cli.predict --ticker AAPL --model-dir artifacts/double_top --period 6mo
```

### 4) Simulation operationnelle (consommee par l'API .NET)

```powershell
python -m finance_ia.cli.simulate --ticker AAPL --model-dir artifacts/double_top --period 6mo --pattern DOUBLE_TOP --investment-amount 1000 --horizon-days 30 --sell-threshold 0.65 --buy-threshold 0.20
```

Sortie principale:

- `estimated_return_pct`
- `estimated_return_amount`
- `estimated_final_amount`
- `recommendation`
- `confidence`
- `assumption`

## Tests

```powershell
.\.venv\Scripts\python.exe -m pytest -q
```

## Lecture rapide des resultats

Si `f1=0`, `precision=0` et `recall=0` sur la periode evaluee, le modele n'est pas exploitable au seuil choisi.  
Dans ce cas:

1. regarder `threshold_analysis` pour trouver un seuil plus adapte (`best_threshold_by_f1`),
2. elargir la periode / ajouter des tickers pour augmenter les cas positifs,
3. garder un statut metier `NoGo` tant que les metriques restent insuffisantes.

Nouveaux champs importants:

- `verdict.status`: `GO` ou `NO_GO`.
- `verdict.reason`: raison principale du statut.
- `verdict.checks`: check detaille par metrique et seuil.
- `evaluation_thresholds`: seuils utilises pour le verdict.
- `field_guide`: aide de lecture rapide des champs du JSON.

## Si le verdict reste NO_GO

Sur une seule annee, certains tickers ont tres peu de positifs (patterns rares).  
Dans ce cas, evaluer sur une fenetre plus large (2-3 ans) pour stabiliser les metriques:

```powershell
python -m finance_ia.cli.evaluate --ticker AAPL --model-dir artifacts/double_top --start 2023-01-01 --end 2025-12-31 --threshold-grid 0.05,0.10,0.15,0.20,0.30,0.40,0.50
```

Batch multi-tickers:

```powershell
$tickers = @("AAPL","MSFT","NVDA","AMZN","GOOGL","META","JPM","XOM","TSLA")
foreach ($t in $tickers) {
  python -m finance_ia.cli.evaluate --ticker $t --model-dir artifacts/double_top --start 2023-01-01 --end 2025-12-31 --threshold-grid 0.05,0.10,0.15,0.20,0.30,0.40,0.50
}
```

Equivalent avec script pret a l'emploi:

```powershell
.\run_evaluate_batch.ps1
```

Version Python (recommandee, portable):

```powershell
python .\run_evaluate_batch.py
```

Le script batch cree automatiquement un dossier parent par lancement:

- `artifacts/evaluation/evaluation_<GUID>_<YYYYMMDD>_<N>/`
- puis un fichier par ticker evalue (`evaluation_<TICKER>.json`).

Avec options:

```powershell
.\run_evaluate_batch.ps1 -Start 2023-01-01 -End 2025-12-31 -MinPrecision 0.08 -NoReport
python .\run_evaluate_batch.py --start 2023-01-01 --end 2025-12-31 --min-precision 0.08 --no-report
```

Mode pilote (plus permissif) possible si besoin metier:

```powershell
python -m finance_ia.cli.evaluate --ticker AAPL --model-dir artifacts/double_top --start 2023-01-01 --end 2025-12-31 --min-precision 0.08
```

## Integration API .NET

L'API peut appeler:

- `python main.py train ...`
- `python main.py evaluate ...`
- `python main.py predict ...`
- `python main.py simulate ...`
