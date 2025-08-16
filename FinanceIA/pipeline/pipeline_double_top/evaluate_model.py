#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
evaluate_model.py

But :
- Construire un jeu de test (X_test, y_test, prices_test) depuis des tickers et une période.
- Évaluer un classifieur binaire (ROC AUC, précision, rappel, F1, rapport complet).
- Tracer les courbes ROC/PR et le diagramme de calibration (fiabilité).
- Simuler un P&L simple (stratégie short si proba > seuil) + Sharpe & Max Drawdown.

Notes pédagogiques (débutant) :
- On garde les index de dates pour toutes les séries → les labels "Double Top"
  reposent sur des positions temporelles cohérentes.
- On renvoie explicitement un vecteur `prices_test` aligné aux fenêtres → plus
  d’hypothèse fragile sur “quelle colonne serait le prix” (ce qui n’était pas robuste).
"""

from __future__ import annotations

# === Imports standards ===
import time
from typing import List, Tuple

# === Librairies scientifiques ===
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt

# === Sklearn: métriques, display & calibration ===
from sklearn.metrics import (
    roc_auc_score,
    precision_score,
    recall_score,
    f1_score,
    classification_report,
    RocCurveDisplay,
    PrecisionRecallDisplay,
)
from sklearn.calibration import calibration_curve

# === Logs structurés ===
import structlog
LOGGER = structlog.get_logger(__name__)

# === Config & utils de ton pipeline Double Top ===
from pipeline.pipeline_double_top.config_double_top import TOTAL_WINDOW, LOOKBACK  # fenêtres
from pipeline.pipeline_double_top.pipeline_utils import (  # données + features + labels
    fetch_data,
    compute_all_indicators,
    compute_labels,
)

# ---------------------------------------------------------------------
# Construction du jeu de test
# ---------------------------------------------------------------------
def build_test_set(
    tickers: List[str],
    start_date: str,
    end_date: str,
) -> Tuple[np.ndarray, np.ndarray, np.ndarray]:
    """
    Construit X_test, y_test, prices_test à partir de plusieurs tickers.

    Étapes (pour chaque ticker) :
      1) Récupérer prix/volumes (fetch_data)
      2) Filtrer sur [start_date, end_date]
      3) Calculer les indicateurs (compute_all_indicators)
      4) Générer les labels (compute_labels)
      5) Fenêtrer pour créer les échantillons (X) + labels (y)
      6) Stocker un prix aligné par fenêtre (prix de clôture de la fin de fenêtre)

    IMPORTANT :
    - On conserve l'index datetime (-> Series Pandas) pour que compute_labels,
      basé sur des pics/indices temporels, reste cohérent. (Correction du code
      qui passait par .values et perdait les dates.)

    Returns
    -------
    X_test : np.ndarray of shape (n_samples, n_features_flat)
        Fenêtres d’indicateurs aplaties (TOTAL_WINDOW * nb_features).
    y_test : np.ndarray of shape (n_samples,)
        Labels binaires 0/1.
    prices_test : np.ndarray of shape (n_samples,)
        Prix de clôture (float) aligné à chaque fenêtre (fin de fenêtre).
        Sert à la simulation P&L.
    """
    feats: List[np.ndarray] = []
    labels: List[int] = []
    prices_track: List[float] = []

    for tk in tickers:
        # 1) Données brutes
        prices, vols = fetch_data(tk, end_date=end_date)
        if prices is None:
            LOGGER.warning("build_test_set_skip_ticker_fetch_failed", ticker=tk)
            continue

        # 2) Filtrage temporel — on GARDERA l'index datetime (pas de .values ici)
        mask = (prices.index >= pd.to_datetime(start_date)) & (prices.index <= pd.to_datetime(end_date))
        ps = prices.loc[mask]  # Series avec DateTimeIndex
        vs = vols.loc[mask]    # idem

        if len(ps) < TOTAL_WINDOW:
            LOGGER.info("build_test_set_skip_ticker_too_short", ticker=tk, n_rows=int(len(ps)))
            continue

        # 3) Indicateurs sur Series indexées (respecte les rolling / alignements)
        ind_df = compute_all_indicators(ps, vs)  # DataFrame (time, features)

        # 4) Labels alignés aux dates
        lab = compute_labels(ps, tk)  # np.ndarray aligné à l’index de ps

        # 5) Fenêtrage : on “glisse” une fenêtre de TOTAL_WINDOW précédée de LOOKBACK
        # Pour chaque i, la fenêtre couvre [i-LOOKBACK ... i-LOOKBACK+TOTAL_WINDOW-1]
        for i in range(LOOKBACK, len(ps) - TOTAL_WINDOW + 1):
            window_df = ind_df.iloc[i - LOOKBACK : i - LOOKBACK + TOTAL_WINDOW]
            feats.append(window_df.values.flatten())          # X (flatten)
            labels.append(int(lab[i]))                        # y (label au centre/position i)

            # 6) Prix aligné : prix de fin de fenêtre (dernier jour de la fenêtre)
            # On suppose que compute_all_indicators a une colonne "price".
            try:
                price_end = float(window_df["price"].iloc[-1])
            except Exception:
                # Si pas de colonne "price" dans ind_df (peu probable), on fallback au ps de la même date
                end_idx = window_df.index[-1]
                price_end = float(ps.loc[end_idx])
            prices_track.append(price_end)

    X_test = np.array(feats, dtype=float)
    y_test = np.array(labels, dtype=int)
    prices_test = np.array(prices_track, dtype=float)

    LOGGER.info(
        "build_test_set_done",
        n_samples=int(len(X_test)),
        n_features=int(X_test.shape[1]) if len(X_test) else 0,
    )
    return X_test, y_test, prices_test


# ---------------------------------------------------------------------
# Visualisations
# ---------------------------------------------------------------------
def plot_curves(y_test: np.ndarray, y_proba: np.ndarray) -> None:
    """
    Trace les courbes ROC et Precision-Recall.
    """
    RocCurveDisplay.from_predictions(y_test, y_proba)
    PrecisionRecallDisplay.from_predictions(y_test, y_proba)
    plt.show()


def reliability_diagram(y_test: np.ndarray, y_proba: np.ndarray, n_bins: int = 10) -> None:
    """
    Diagramme de fiabilité : probabilité prédite vs probabilité observée.
    Si les points sont proches de la diagonale, le modèle est bien calibré.
    """
    # prob_true: fréquence observée dans chaque bin
    # prob_pred: moyenne des proba prédites dans chaque bin
    prob_true, prob_pred = calibration_curve(y_test, y_proba, n_bins=n_bins)
    plt.figure()
    plt.plot(prob_pred, prob_true, marker="o", label="Calibration")
    plt.plot([0, 1], [0, 1], "--", label="Parfaite")
    plt.xlabel("Probabilité prédite")
    plt.ylabel("Probabilité observée")
    plt.title("Reliability Diagram")
    plt.legend()
    plt.show()


# ---------------------------------------------------------------------
# Simulation financière & métriques de risque
# ---------------------------------------------------------------------
def simulate_pnl(
    prices: np.ndarray,
    y_proba: np.ndarray,
    threshold: float = 0.5,
) -> np.ndarray:
    """
    Stratégie jouet : on prend une position SHORT si proba > seuil.

    - positions[t] = 1 si on est short le jour t, 0 sinon
    - rendement[t+1] = (P[t+1] - P[t]) / P[t]
    - P&L[t+1] = position[t] * (-rendement[t+1])  (short gagne si le prix baisse)

    NB : on tronque d’un pas car le premier rendement est entre t=0 et t=1.

    Returns
    -------
    pnl_cum : np.ndarray
        Série cumulée des P&L (même longueur que returns → len(prices)-1 → on recadre plus bas).
    """
    if len(prices) == 0 or len(y_proba) == 0:
        return np.array([], dtype=float)

    # positions alignées aux PROBAS (échantillons)
    pos = (y_proba > threshold).astype(int)

    # rendements quotidiens à partir des PRIX (len = n-1)
    returns = np.diff(prices) / np.maximum(prices[:-1], 1e-12)

    # On aligne : pos[:-1] s’aligne sur returns (qui commence en t=1)
    pnl = pos[:-1] * (-returns)

    # cumul pour visualiser l’évolution
    return np.cumsum(pnl)


def sharpe_ratio(pnl: np.ndarray) -> float:
    """
    Sharpe annualisé simple : mean(pnl) / std(pnl) * sqrt(252).
    On protège contre std=0.
    """
    if pnl.size == 0:
        return 0.0
    vol = float(np.std(pnl))
    if vol <= 1e-12:
        return 0.0
    return float(np.mean(pnl) / vol * np.sqrt(252))


def max_drawdown(pnl: np.ndarray) -> float:
    """
    Max drawdown (MDD) sur une courbe de P&L cumulée.
    Défini comme (pic - creux) / pic, max sur toute la série.
    """
    if pnl.size == 0:
        return 0.0
    cum_max = np.maximum.accumulate(pnl)
    # éviter division par 0 quand cum_max==0 au début
    safe_cum_max = np.where(cum_max <= 1e-12, 1e-12, cum_max)
    dd = (cum_max - pnl) / safe_cum_max
    return float(np.max(dd))


# ---------------------------------------------------------------------
# Évaluation complète
# ---------------------------------------------------------------------
def evaluate_model(
    model,
    X_test: np.ndarray,
    y_test: np.ndarray,
    prices_test: np.ndarray | None = None,
    threshold: float = 0.5,
    plot_curves_flag: bool = True,
) -> dict:
    """
    Évalue le modèle fourni sur (X_test, y_test) et, si `prices_test` est fourni,
    calcule aussi des métriques financières.

    Paramètres
    ----------
    model : classifieur sklearn-like
        Doit exposer predict(X) et predict_proba(X)[:, 1].
    X_test : (n_samples, n_features)
    y_test : (n_samples,)
    prices_test : (n_samples,), optionnel
        Prix aligné à chaque fenêtre (fin de fenêtre) → pour la simulation.
    threshold : float
        Seuil pour déclencher la position short.
    plot_curves_flag : bool
        Si True, trace ROC/PR + diagramme de fiabilité.

    Retour
    ------
    results : dict
        Dictionnaire avec les métriques calculées.
    """
    t0 = time.time()
    results: dict = {}

    try:
        LOGGER.info("eval_start", n_samples=int(X_test.shape[0]), n_features=int(X_test.shape[1]))

        # 1) Prédictions
        y_pred = model.predict(X_test)
        y_proba = model.predict_proba(X_test)[:, 1]

        # 2) Scores de classification
        roc = float(roc_auc_score(y_test, y_proba))
        prec = float(precision_score(y_test, y_pred))
        rec = float(recall_score(y_test, y_pred))
        f1 = float(f1_score(y_test, y_pred))

        LOGGER.info("metrics_summary", roc_auc=roc, precision=prec, recall=rec, f1=f1)
        rep = classification_report(y_test, y_pred)
        LOGGER.info("classification_report", report=rep)

        results.update({
            "roc_auc": roc,
            "precision": prec,
            "recall": rec,
            "f1": f1,
            "classification_report": rep,
        })

        # 3) Visualisations (optionnel)
        if plot_curves_flag:
            LOGGER.info("plotting_curves_start")
            plot_curves(y_test, y_proba)       # ROC + PR
            reliability_diagram(y_test, y_proba)  # Calibration
            LOGGER.info("plotting_curves_done")

        # 4) Simulation financière (optionnelle)
        if prices_test is not None and len(prices_test) == len(y_proba):
            pnl = simulate_pnl(prices_test, y_proba, threshold=threshold)
            sr = sharpe_ratio(pnl)
            mdd = max_drawdown(pnl)
            LOGGER.info("finance_metrics", sharpe_ratio=sr, max_drawdown=mdd)
            results.update({
                "pnl_curve": pnl,           # np.ndarray (utile si tu veux sauvegarder/plotter)
                "sharpe_ratio": sr,
                "max_drawdown": mdd,
            })
        else:
            if prices_test is None:
                LOGGER.info("finance_skipped", reason="no_prices_test_provided")
            else:
                LOGGER.info("finance_skipped", reason="prices_len_mismatch",
                            len_prices=int(len(prices_test)), len_proba=int(len(y_proba)))

        elapsed = time.time() - t0
        LOGGER.info("eval_done", elapsed_sec=round(elapsed, 3))
        return results

    except Exception as e:
        # structlog .exception ajoute la stacktrace selon ta config logging
        LOGGER.exception("eval_failed", error=str(e))
        raise


# ---------------------------------------------------------------------
# Exemple d’utilisation en script (facultatif)
# ---------------------------------------------------------------------
if __name__ == "__main__":
    """
    Exemple rapide pour lancer une évaluation :
    - tu fournis une liste de tickers + dates,
    - tu charges ton modèle LightGBM sauvegardé,
    - tu appelles evaluate_model(...).

    NB: Ajuste le chemin du modèle selon ta config (ex: config_double_top.GBM_PERSIST_FILE).
    """
    import joblib
    from pipeline.pipeline_double_top.config_double_top import GBM_PERSIST_FILE

    tickers_demo = ["AAPL", "MSFT", "NVDA"]
    start_demo = "2022-01-01"
    end_demo = "2024-12-31"

    # (1) Construire le jeu de test
    X_test_demo, y_test_demo, prices_test_demo = build_test_set(tickers_demo, start_demo, end_demo)

    # (2) Charger le modèle entraîné (LightGBM pipeline)
    try:
        model = joblib.load(GBM_PERSIST_FILE)
    except Exception as e:
        LOGGER.error("model_load_failed", path=str(GBM_PERSIST_FILE), error=str(e))
        raise

    # (3) Évaluer
    _ = evaluate_model(
        model,
        X_test_demo,
        y_test_demo,
        prices_test=prices_test_demo,  # important pour la partie P&L
        threshold=0.5,
        plot_curves_flag=True,
    )
