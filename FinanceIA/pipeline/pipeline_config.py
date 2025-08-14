# config.py
"""
Paramètres de configuration pour le pipeline d'entraînement.
"""

TICKER = "AAPL"  # Symbole du titre
START_DATE = "2018-01-01"  # Date de début pour la récupération des données
END_DATE = "2023-12-31"  # Date de fin pour la récupération des données
RESULTS_DIR = "models"  # Répertoire de sauvegarde des modèles

# Horizons et indicateurs
FUTURE_DAYS = 5  # Horizon de calcul du rendement futur
RSI_PERIOD = 14  # Période pour le RSI
MACD_FAST = 12  # Période rapide pour le MACD
MACD_SLOW = 26  # Période lente pour le MACD
MACD_SIGNAL = 9  # Période du signal MACD

# Pour le découpage train/test
test_size = 0.2  # Fraction du jeu de test
RANDOM_STATE = 42  # Graîne pour la reproductibilité