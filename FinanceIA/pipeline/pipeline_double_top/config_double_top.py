#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
config_double_top.py

Fichier de configuration pour l'analyse du pattern "Double Top".
Centralise tous les réglages (fenêtres temporelles, détection de pics,
validation, versioning) pour faciliter l'expérimentation et la traçabilité.
Chaque section est commentée pour un débutant en Python.
"""

# --------------------------------------------------------
# Imports nécessaires
# --------------------------------------------------------
import os                # pour gérer les chemins de fichiers
from datetime import datetime  # pour générer automatiquement des timestamps
from pathlib import Path

# --------------------------------------------------------
# Versioning et chemins de sortie
# --------------------------------------------------------
# BASE_DIR : répertoire racine du projet (un niveau au-dessus)
BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
# MODEL_DIR : répertoire où seront sauvegardés les modèles et scalers
MODEL_DIR = Path("/var/www/predictFinance/ModelsIA")
MODEL_DIR.mkdir(parents=True, exist_ok=True)

# Création du répertoire s'il n'existe pas
os.makedirs(MODEL_DIR, exist_ok=True)

# VERSION_TAG : horodatage au format YYYYMMDD_HHMM pour versionner les sorties
VERSION_TAG = datetime.now().strftime("%Y%m%d_%H%M")
# Noms de fichiers pour scaler et différents modèles (LightGBM, CNN, stacking)
SCALER_FILE = os.path.join(MODEL_DIR, f"scaler_{VERSION_TAG}.joblib")
GBM_PERSIST_FILE = MODEL_DIR / "double_top_lightgbm.joblib"
CNN_PERSIST_FILE = MODEL_DIR / "double_top_cnn.keras"

STACK_FILE  = os.path.join(MODEL_DIR, f"stacked_model_{VERSION_TAG}.pkl")

# --------------------------------------------------------
# Fenêtres temporelles pour le pattern Double Top
# --------------------------------------------------------
# LOOKBACK : nombre de jours avant le premier pic pour la fenêtre d'observation
LOOKBACK    = 10
# LOOKBETWEEN : écart maximal (en jours) autorisé entre les deux pics
LOOKBETWEEN = 20
# LOOKAHEAD : nombre de jours après le deuxième pic pour labelliser l'événement
LOOKAHEAD   = 10
# TOTAL_WINDOW : taille totale de la fenêtre = LOOKBACK + LOOKBETWEEN + LOOKAHEAD + 1
TOTAL_WINDOW = LOOKBACK + LOOKBETWEEN + LOOKAHEAD + 1

# === Paramètres d'entraînement ===
EPOCHS = 10
BATCH_SIZE = 32

# --------------------------------------------------------
# Ratio négatifs / positifs pour l'équilibrage des classes
# --------------------------------------------------------
# NEG_RATIO : nombre de fenêtres négatives pour chaque exemple positif
NEG_RATIO = 2

# --------------------------------------------------------
# Pics manuels (définis par l'utilisateur) pour validation
# --------------------------------------------------------
# MANUAL_PEAKS : dictionnaire de listes de tuples (date_pic1, date_pic2)
#  Chaque paire indique un double top validé manuellement pour ce ticker.
MANUAL_PEAKS = {
    'AAPL': [("2012-09-10","2012-09-21"), ("2015-05-25","2015-06-08"), ("2022-01-04","2022-01-28")],
    'MSFT': [("1999-03-18","1999-03-29"), ("2001-01-29","2001-02-08"), ("2018-10-02","2018-10-29")],
    'GOOG': [("2019-04-01","2019-05-01"), ("2022-01-03","2022-01-26")],
    'AMZN': [("2018-09-04","2018-10-04"), ("2021-07-08","2021-08-16")],
    'TSLA': [("2021-01-26","2021-02-16"), ("2022-11-04","2022-12-14")],
    'NVDA': [("2023-07-07","2023-08-10"), ("2024-03-18","2024-05-02")],
    'FB':   [("2018-07-26","2018-08-13"), ("2021-09-07","2021-10-04")],
    'JPM':  [("2018-02-21","2018-03-09"), ("2023-01-13","2023-02-06")],
    'BAC':  [("2018-01-26","2018-02-12"), ("2023-05-04","2023-05-24")],
    'WMT':  [("2019-02-19","2019-03-01"), ("2022-07-01","2022-07-27")],
    'XOM':  [("2018-04-23","2018-05-21"), ("2022-03-07","2022-03-29")],
    'PFE':  [("2020-09-02","2020-09-18"), ("2021-01-15","2021-02-03")],
    'ORCL': [("2018-10-11","2018-11-30"), ("2022-09-15","2022-10-28")],
    'INTC': [("2020-05-04","2020-05-20"), ("2021-03-31","2021-04-13")],
    'CSCO': [("2021-02-18","2021-03-12"), ("2022-09-19","2022-10-04")],
}


# --------------------------------------------------------
# Détection automatique de pics (optionnelle)
# --------------------------------------------------------
# ENABLE_AUTO_PEAKS : True pour compléter ou remplacer MANUAL_PEAKS
ENABLE_AUTO_PEAKS = True
# Paramètres passés à scipy.signal.find_peaks
AUTO_PEAK_PARAMS = {
    # distance minimale entre pics en nombre de jours
    "distance": LOOKBETWEEN,
    # prominence relative minimale (ex. 0.03 = 3% de différence)
    "prominence": 0.03,
}

# --------------------------------------------------------
# Paramètres de backtest et validation temporelle
# --------------------------------------------------------
# TS_SPLITS : nombre de splits pour TimeSeriesSplit (cross-validation)
TS_SPLITS    = 5
# TS_TEST_SIZE : taille de la portion test (None = calcul automatique)
TS_TEST_SIZE = None
# Période de backtest (dates au format YYYY-MM-DD)
BACKTEST_START = "2018-01-01"
BACKTEST_END   = "2020-12-31"

# --------------------------------------------------------
# Grilles de paramètres pour optimisation (Optuna / GridSearch)
# --------------------------------------------------------
PARAM_GRID_WINDOW = {
    # valeurs possibles pour LOOKBACK, LOOKBETWEEN et LOOKAHEAD
    "LOOKBACK":    [5, 10, 20],
    "LOOKBETWEEN": [10, 20, 30],
    "LOOKAHEAD":   [5, 10, 20],
}
PARAM_GRID_NEG = {
    # valeurs possibles pour le ratio de négatifs
    "NEG_RATIO": [1, 2, 3, 4],
}
