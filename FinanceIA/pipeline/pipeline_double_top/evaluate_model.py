#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
evaluate_model.py

Script pour construire le jeu de test, évaluer un modèle de classification
et afficher des métriques, des courbes ROC/PR, un diagramme de calibration
et des indicateurs financiers (Sharpe ratio, drawdown).

Chaque bloc de code est commenté pour faciliter la compréhension
d’un débutant en Python et en apprentissage automatique.
"""

# Import de matplotlib pour tracer les graphiques
import matplotlib.pyplot as plt
# numpy pour les calculs numériques sur les tableaux
import numpy as np
# pandas pour manipuler facilement les dates et séries temporelles
import pandas as pd

# sklearn.calibration fournit la fonction calibration_curve
# pour tracer un diagramme de fiabilité (reliability diagram)
from sklearn.calibration import calibration_curve

# PrecisionRecallDisplay et RocCurveDisplay génèrent
# automatiquement les courbes PR et ROC à partir de prédictions
from sklearn.metrics import PrecisionRecallDisplay, RocCurveDisplay

# Import des métriques standard pour l’évaluation de classification
from sklearn.metrics import (
    roc_auc_score,    # Aire sous la courbe ROC
    precision_score,  # Précision = TP / (TP + FP)
    recall_score,     # Rappel = TP / (TP + FN)
    f1_score,         # F1-score, moyenne harmonique précision/rappel
    classification_report  # Rapport détaillé (précision, rappel, f1 par classe)
)

# Import des constantes de configuration utilisées pour fenêtrage
from .config_double_top import TOTAL_WINDOW, LOOKBACK
# Import des fonctions utilitaires pour récupérer données et générer labels
from .pipeline_utils import fetch_data, compute_all_indicators, compute_labels


def build_test_set(
    tickers: list[str],
    start_date: str,
    end_date: str
) -> tuple[np.ndarray, np.ndarray]:
    """
    Construit X_test et y_test à partir de plusieurs tickers.

    Pour chaque ticker, on :
      1. Récupère prix et volumes (fetch_data)
      2. Filtre la période demandée
      3. Calcule les indicateurs techniques (compute_all_indicators)
      4. Génère les labels double-top (compute_labels)
      5. Glisse une fenêtre de longueur TOTAL_WINDOW pour former X et y

    Args:
        tickers: liste de symboles boursiers
        start_date: date de début (YYYY-MM-DD)
        end_date: date de fin (YYYY-MM-DD)

    Returns:
        X_test: tableau numpy de forme (n_samples, features)
        y_test: vecteur d’étiquettes 0/1
    """
    feats: list[np.ndarray] = []   # liste pour stocker les fenêtres d’indicateurs
    labels: list[int] = []         # liste pour stocker les labels associés

    for tk in tickers:
        # 1) Récupération des données
        prices, vols = fetch_data(tk, end_date=end_date)
        if prices is None:
            # Si échec de fetch_data, on passe ce ticker
            continue

        # 2) Filtrage temporel
        mask = (prices.index >= pd.to_datetime(start_date)) & (
               prices.index <= pd.to_datetime(end_date))
        ps = prices.loc[mask].values  # extraire les prix en numpy array
        vs = vols.loc[mask].values    # extraire les volumes
        if len(ps) < TOTAL_WINDOW:
            # Si trop peu de données, on ignore ce ticker
            continue

        # 3) Calcul des indicateurs et 4) génération des labels
        ind_df = compute_all_indicators(pd.Series(ps), pd.Series(vs))
        lab = compute_labels(pd.Series(ps), tk)

        # 5) Fenêtrage : pour chaque position possible,
        # on prend TOTAL_WINDOW valeurs précédées de LOOKBACK
        for i in range(LOOKBACK, len(ps) - TOTAL_WINDOW + 1):
            # Sélection de la fenêtre de données
            window = ind_df.iloc[
                i - LOOKBACK : i - LOOKBACK + TOTAL_WINDOW
            ].values.flatten()
            feats.append(window)      # ajout de la fenêtre dans X
            labels.append(int(lab[i]))  # ajout du label correspondant

    # Conversion des listes en tableaux numpy
    return np.array(feats), np.array(labels)


def plot_curves(y_test, y_proba):
    """
    Trace les courbes ROC et Precision-Recall pour évaluer
    la qualité des probabilités prédites.

    Args:
        y_test: vrai vecteur de labels
        y_proba: probabilités de la classe positive
    """
    # Affichage de la courbe ROC (sensibilité vs spécificité)
    RocCurveDisplay.from_predictions(y_test, y_proba)
    # Affichage de la courbe Precision-Recall (précision vs rappel)
    PrecisionRecallDisplay.from_predictions(y_test, y_proba)


def reliability_diagram(y_test, y_proba, n_bins=10):
    """
    Affiche le diagramme de fiabilité pour vérifier si
    les probabilités prédites sont bien calibrées.

    Args:
        y_test: vrai vecteur de labels
        y_proba: probabilités prédites
        n_bins: nombre de catégories de probabilité
    """
    # calibration_curve renvoie prob_true = fréquence observée,
    # prob_pred = moyenne des probabilités prédites par bin
    prob_true, prob_pred = calibration_curve(y_test, y_proba, n_bins=n_bins)
    plt.figure()  # nouvelle figure pour le tracé
    plt.plot(prob_pred, prob_true, marker="o", label="Calibration")
    plt.plot([0, 1], [0, 1], "--", label="Parfaite")
    plt.xlabel("Probabilité prédite")
    plt.ylabel("Probabilité observée")
    plt.title("Reliability Diagram")
    plt.legend()
    plt.show()


def simulate_pnl(prices: np.ndarray, y_proba: np.ndarray, threshold=0.5) -> np.ndarray:
    """
    Simule un P&L cumulatif basé sur une stratégie :
    on prend une position short lorsque la probabilité > threshold.

    Args:
        prices: série de prix (numpy)
        y_proba: probabilités de signal
        threshold: seuil pour ouvrir la position

    Returns:
        pnl cumulé (numpy)
    """
    # positions = 1 si probabilité > seuil, sinon 0
    positions = (y_proba > threshold).astype(int)
    # rendements journaliers = variation relative des prix
    returns = np.diff(prices) / prices[:-1]
    # P&L = position * -retour (short = gains si prix baisse)
    pnl = positions[:-1] * -returns
    # somme cumulative pour obtenir la série de P&L
    return np.cumsum(pnl)


def sharpe_ratio(pnl: np.ndarray) -> float:
    """
    Calcule le Sharpe ratio annualisé sur une série de P&L.

    Args:
        pnl: P&L cumulatif

    Returns:
        Sharpe ratio annualisé
    """
    # mean / std * sqrt(252 jours de trading)
    return pnl.mean() / pnl.std() * np.sqrt(252)


def max_drawdown(pnl: np.ndarray) -> float:
    """
    Calcule le drawdown maximum sur la série de P&L.

    Args:
        pnl: P&L cumulatif

    Returns:
        maximum drawdown (en proportion)
    """
    # cum_max = maximum historique à chaque pas
    cum_max = np.maximum.accumulate(pnl)
    # drawdown = (pic - valley) / pic
    return np.max((cum_max - pnl) / cum_max)


def evaluate_model(model, X_test: np.ndarray, y_test: np.ndarray) -> None:
    """
    Exécute l’évaluation complète du modèle :
      - prédiction de classes et probabilités
      - rapport de classification et scores (ROC AUC, précision, rappel, F1)
      - tracé des courbes ROC, PR et diagramme de fiabilité
      - simulation financière et calcul de Sharpe & drawdown

    Args:
        model: modèle entraîné (sklearn-like API)
        X_test: features de test
        y_test: labels de test
    """
    # Prédiction des classes (0/1)
    y_pred = model.predict(X_test)
    # Prédiction des probabilités (colonne de la classe positive)
    y_proba = model.predict_proba(X_test)[:, 1]

    # Affichage du rapport de classification détaillé
    print("\n=== Classification report ===")
    print(classification_report(y_test, y_pred))
    # Calcul et affichage des scores globaux
    print(f"ROC AUC   : {roc_auc_score(y_test, y_proba):.4f}")
    print(f"Precision : {precision_score(y_test, y_pred):.4f}")
    print(f"Recall    : {recall_score(y_test, y_pred):.4f}")
    print(f"F1-score  : {f1_score(y_test, y_pred):.4f}\n")

    # Tracer les courbes ROC et PR
    plot_curves(y_test, y_proba)

    # Afficher le reliability diagram
    reliability_diagram(y_test, y_proba)

    # Simulation P&L et métriques financières
    # NOTE: ici, il faut idéalement passer aussi une série de prix réelle
    # on utilise X_test pour extraire une feature "prix" comme placeholder
    prices_test = X_test[:, -TOTAL_WINDOW]  # suppose que la dernière feature est le prix
    pnl = simulate_pnl(prices_test, y_proba)
    print(f"Sharpe ratio  : {sharpe_ratio(pnl):.2f}")
    print(f"Max drawdown  : {max_drawdown(pnl):.2%}")
