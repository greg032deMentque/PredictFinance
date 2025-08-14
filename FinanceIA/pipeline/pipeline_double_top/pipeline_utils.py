from __future__ import annotations
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
pipeline_utils.py

Module de pipeline pour récupérer des données financières, calculer des indicateurs techniques,
et générer des labels pour un modèle d'IA.

L'objectif de cette version commentée est d'expliquer chaque étape pour un débutant en Python et en IA.
"""

# structlog : bibliothèque pour logger des événements structurés
import structlog
# On récupère un logger pour ce module
logger = structlog.get_logger(__name__)

# pandas pour la manipulation de données en DataFrame/Series
import pandas as pd
from typing import Optional, Tuple
# numpy pour les calculs numériques
import numpy as np
# yfinance pour récupérer des données financières depuis Yahoo Finance
import yfinance as yf
# find_peaks de scipy pour détecter automatiquement les pics dans une série
from scipy.signal import find_peaks
# arch_model pour modéliser la volatilité (GARCH)
from arch import arch_model

# Import des constantes de configuration pour le double top
from .config_double_top import (
    LOOKBACK,       # nombre de jours de recul pour positionner un sommet
    LOOKBETWEEN,    # nombre de jours max entre deux sommets
    LOOKAHEAD,      # nombre de jours d'avance pour la fenêtre de labelisation
    TOTAL_WINDOW,   # taille minimale de la fenêtre de données requise
    ENABLE_AUTO_PEAKS,  # booléan, si on active la détection automatique de pics
    AUTO_PEAK_PARAMS,   # paramètres pour la fonction find_peaks
    MANUAL_PEAKS,       # pics manuels définis par l'utilisateur par ticker
)


def fetch_data(
    ticker: str,
    end_date: 'Optional[str]' = None
) -> 'Tuple[Optional[pd.Series], Optional[pd.Series]]':
    """
    Récupère les séries historiques "Close" (prix de clôture) et "Volume" pour un ticker donné.
    Les données couvrent de 1990-01-01 jusqu'à end_date (optionnel).

    Args:
        ticker (str): Symbole boursier (ex. "AAPL" ou "BRK.B").
        end_date (str|None): Date de fin au format YYYY-MM-DD. None = aujourd'hui.

    Returns:
        Tuple[pd.Series, pd.Series]: deux pandas Series (prix, volume),
        ou (None, None) en cas d'erreur ou manque de données.
    """
    try:
        # Fonction interne pour récupérer l'historique via yfinance
        def _grab(tk: str) -> pd.DataFrame:
            # history renvoie un DataFrame avec index date et colonnes [Open, High, Low, Close, Volume, ...]
            return yf.Ticker(tk).history(start="1990-01-01", end=end_date)

        # Première tentative avec le ticker tel quel
        df = _grab(ticker)
        # Si vide et que le ticker contient un point (ex. BRK.B), on remplace par un tiret
        if df.empty and "." in ticker:
            alt = ticker.replace(".", "-")
            logger.info(f"Retrying {ticker} as {alt}")
            df = _grab(alt)

        # On vérifie qu'on a assez de données (TOTAL_WINDOW jours)
        if df.empty or len(df) < TOTAL_WINDOW:
            logger.error(f"Pas assez de données pour {ticker} (len={len(df)})")
            return None, None

        # Extraire la colonne Close et retirer les valeurs NaN
        prices = df["Close"].dropna().tz_localize(None)
        # Extraire le volume, forward-fill pour combler les trous et aligner l'index
        vols = df["Volume"].ffill().reindex(prices.index)

        # Reconstruire un DataFrame minimal avec Close et Volume
        df_loc = df.reindex(prices.index)
        df_loc = df_loc.assign(Close=prices, Volume=vols)
        # Ajouter High et Low pour les chandeliers
        df_loc["high"] = df["High"].reindex(prices.index)
        df_loc["low"] = df["Low"].reindex(prices.index)

        # On renvoie deux Series : prix de clôture et volume
        return df_loc["Close"], df_loc["Volume"]

    except Exception as e:
        # Logger en cas d'erreur inattendue
        logger.error(f"fetch_data {ticker}: {e}")
        return None, None



def compute_all_indicators(
    prices: pd.Series,
    vols: pd.Series
) -> pd.DataFrame:
    """
    Calcule un ensemble complet d'indicateurs techniques à partir des prix et volumes.

    Args:
        prices (pd.Series): Série des prix de clôture.
        vols (pd.Series): Série des volumes correspondants.

    Returns:
        pd.DataFrame: DataFrame où chaque colonne est un indicateur technique.
    """
    # Initialisation du DataFrame de travail
    df = pd.DataFrame({"price": prices, "vol": vols})

    # 1. Indicateurs classiques (RSI, MACD, Bollinger, ATR...)
    # ------------------------------------------------------
    # Calcul du RSI (Relative Strength Index)
    delta = df["price"].diff()          # variation quotidienne
    up = delta.clip(lower=0)              # gains uniquement
    down = -delta.clip(upper=0)           # pertes uniquement
    df["rsi"] = 100 - (100 / (1 + up.rolling(14).mean() / down.rolling(14).mean()))

    # MACD = difference entre moyennes mobiles exponentielles 12 et 26 jours
    e12 = df["price"].ewm(span=12, adjust=False).mean()
    e26 = df["price"].ewm(span=26, adjust=False).mean()
    df["macd"] = e12 - e26

    # Bollinger Bands (20 jours +/− 2 écarts-types)
    m20 = df["price"].rolling(20).mean()
    s20 = df["price"].rolling(20).std()
    df["bb_up"] = m20 + 2 * s20
    df["bb_dn"] = m20 - 2 * s20

    # Average True Range (approximation de la volatilité intrajournalière)
    df["atr"] = df["price"].diff().abs().rolling(14).mean()

    # Williams %R (momentum sur 14 jours)
    hh14 = df["price"].rolling(14).max()
    ll14 = df["price"].rolling(14).min()
    df["willr"] = (hh14 - df["price"]) / (hh14 - ll14) * -100

    # Volume normalisé (0 à 1)
    max_vol = df["vol"].max()
    df["vol_norm"] = df["vol"] / max_vol if max_vol else 0.0 if max_vol else 0.0

    # Pentes (première et deuxième dérivée) pour capter la vitesse de variation
    df["slope1"] = df["price"].diff()
    df["slope2"] = df["price"].diff().diff()

    # 2. Volatility Features
    # ----------------------
    # Volatilité réalisée annualisée sur fenêtre de 20 jours
    df["realized_vol"] = df["price"].pct_change().rolling(20).std() * np.sqrt(252)
    # Volatilité réalisée sur différentes fenêtres
    for w in [5, 10, 20, 60]:
        df[f"realized_vol_{w}"] = (
            df["price"].pct_change().rolling(w).std() * np.sqrt(252)
        )

    # Modèle GARCH(1,1) pour estimer la volatilité conditionnelle
    try:
        returns_pct = df["price"].pct_change().dropna() * 100
        am = arch_model(returns_pct, vol="Garch", p=1, q=1)
        res = am.fit(disp="off")  # disp="off" supprime la sortie console
        # On remplit la volatilité GARCH dans le DataFrame
        df["garch_vol"] = res.conditional_volatility.reindex(df.index).ffill()
    except Exception as e:
        logger.warning("garch_failed", error=str(e))
        df["garch_vol"] = 0.0
    # 3. Statistiques de chandeliers (candlesticks)
    # ---------------------------------------------
    # Corps de la bougie = |prix clôture - prix ouverture précédente|
    df["body"] = (df["price"] - df["price"].shift(1)).abs()
    highs = df["price"].rolling(2).max()
    lows = df["price"].rolling(2).min()
    # Mèche haute et mèche basse
    df["upper_wick"] = highs - df["price"].shift(1)
    df["lower_wick"] = df["price"].shift(1) - lows

    # 4. Statistiques avancées de forme de distribution
    # ------------------------------------------------
    ret = df["price"].pct_change().fillna(0)
    df["ret_skew_20"] = ret.rolling(20).skew()   # asymétrie
    df["ret_kurt_20"] = ret.rolling(20).kurt()   # kurtose

    # 5. Autres caractéristiques de chandelier
    # ----------------------------------------
    df["candle_range"] = highs - lows
    # Ratio corps/boule pour détecter la taille du corps vs l'ombre
    den = df["candle_range"].replace(0, np.nan)
    df["candle_body_ratio"] = df["body"] / den.replace(0, np.nan)

    # Remplacer les NaN par 0 pour avoir un DataFrame complet
    return df.fillna(0)


def compute_labels(
    prices: pd.Series,
    ticker: str
) -> np.ndarray:
    """
    Génère des labels binaires (0 ou 1) indiquant le centre d'un pattern "double top".

    Args:
        prices (pd.Series): Série des prix de clôture.
        ticker (str): Symbole boursier pour récupérer les pics manuels.

    Returns:
        np.ndarray: vecteur d'entiers (0/1) de la même longueur que prices.
    """
    # Conversion des dates de l'index en datetime
    dates = pd.to_datetime(prices.index)
    peaks: list[tuple[int, int]] = []  # liste de couples (index_pic1, index_pic2)

    # Détection automatique des pics si activée
    if ENABLE_AUTO_PEAKS:
        auto_idx, _ = find_peaks(prices.values, **AUTO_PEAK_PARAMS)
        # On cherche toutes les paires de pics dans la fenêtre LOOKBETWEEN
        for i1 in auto_idx:
            for i2 in auto_idx:
                if 0 < (i2 - i1) <= LOOKBETWEEN:
                    peaks.append((i1, i2))
    # Ajout des pics manuels définis dans la configuration
    for d1, d2 in MANUAL_PEAKS.get(ticker, []):
        idx1 = dates.get_loc(pd.to_datetime(d1))
        idx2 = dates.get_loc(pd.to_datetime(d2))
        if 0 < (idx2 - idx1) <= LOOKBETWEEN:
            peaks.append((idx1, idx2))

    # Initialisation des labels à 0
    labels = np.zeros(len(prices), dtype=int)
    # On marque le centre du double top (deuxième pic) comme 1 si suffisamment éloigné
    for i1, i2 in peaks:
        center = i2
        if LOOKBACK <= center < len(prices) - LOOKAHEAD:
            labels[center] = 1
    return labels


def detect_peaks(
    prices: pd.Series
) -> pd.DatetimeIndex:
    """
    Fonction utilitaire pour détecter rapidement les pics automatiques.

    Args:
        prices (pd.Series): Série des prix de clôture.

    Returns:
        pd.DatetimeIndex: dates où des pics ont été détectés.
    """
    idx, _ = find_peaks(prices.values, **AUTO_PEAK_PARAMS)
    return prices.index[idx]