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
from pipeline.pipeline_double_top.config_double_top import (
    LOOKBACK,       # nombre de jours de recul pour positionner un sommet
    LOOKBETWEEN,    # nombre de jours max entre deux sommets
    LOOKAHEAD,      # nombre de jours d'avance pour la fenêtre de labelisation
    TOTAL_WINDOW,   # taille minimale de la fenêtre de données requise
    ENABLE_AUTO_PEAKS,  # booléan, si on active la détection automatique de pics
    AUTO_PEAK_PARAMS,   # paramètres pour la fonction find_peaks
    MANUAL_PEAKS,       # pics manuels définis par l'utilisateur par ticker
)


import time
import structlog

LOGGER = structlog.get_logger(__name__)  # logger structuré pour ce module


def fetch_data(
    ticker: str,
    end_date: 'Optional[str]' = None
) -> 'Tuple[Optional[pd.Series], Optional[pd.Series]]':
    t0 = time.time()
    _log = logger.bind(ticker=ticker)  # on "binde" le ticker dans le contexte
    try:
        def _grab(tk: str) -> pd.DataFrame:
            return yf.Ticker(tk).history(start="1990-01-01", end=end_date)

        _log.info("fetch_start", end_date=end_date)
        df = _grab(ticker)
        if df.empty and "." in ticker:
            alt = ticker.replace(".", "-")
            _log.warning("fetch_retry_alt_ticker", alt=alt)
            df = _grab(alt)

        if df.empty or len(df) < TOTAL_WINDOW:
            _log.error("fetch_not_enough_data", n_rows=int(len(df)))
            return None, None

        prices = df["Close"].dropna().tz_localize(None)
        vols = df["Volume"].ffill().reindex(prices.index)

        df_loc = df.reindex(prices.index)
        df_loc = df_loc.assign(Close=prices, Volume=vols)
        df_loc["high"] = df["High"].reindex(prices.index)
        df_loc["low"] = df["Low"].reindex(prices.index)

        elapsed = time.time() - t0
        _log.info("fetch_done", n_rows=int(len(df_loc)), elapsed_sec=round(elapsed, 3))
        return df_loc["Close"], df_loc["Volume"]

    except Exception as e:
        _log.exception("fetch_failed", error=str(e))
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
    t0 = time.time()
    _log = logger  # pas de ticker ici, mais on pourrait binder si appelé depuis fetch
    _log.info("compute_indicators_start", n_rows=int(len(prices)))

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
    # Éviter une double condition redondante qui pouvait conduire à un code difficile à lire.
    # Lorsque max_vol est nul, on renvoie 0 pour éviter une division par zéro.
    df["vol_norm"] = df["vol"] / max_vol if max_vol else 0.0

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
        res = am.fit(disp="off")
        df["garch_vol"] = res.conditional_volatility.reindex(df.index).ffill()
        _log.info("garch_ok")
    except Exception as e:
        _log.warning("garch_failed", error=str(e))
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
    out = df.fillna(0)
    elapsed = time.time() - t0
    _log.info("compute_indicators_done", n_features=int(out.shape[1]), elapsed_sec=round(elapsed, 3))
    return out


def compute_labels(
    prices: pd.Series,
    ticker: str
) -> np.ndarray:
    """
    Génère un vecteur de labels 0/1 aligné aux dates de `prices`.
    Un '1' est posé au niveau du deuxième sommet (i2) d'un double top validé.

    Règles :
      - pics automatiques (find_peaks) + pics manuels (MANUAL_PEAKS)
      - filtrage des paires (i1, i2) avec is_double_top(...)
      - label = 1 à i2 (sinon 0)
    """
    t0 = time.time()
    _log = logger.bind(ticker=ticker)
    _log.info("compute_labels_start", n_rows=int(len(prices)))

    # Sécurité: on veut une Series avec index de dates
    ps = prices.copy()
    ps.index = pd.to_datetime(ps.index)

    labels = np.zeros(len(ps), dtype=int)

    # 1) pics automatiques
    auto_idx: list[int] = []
    try:
        # NOTE: find_peaks travaille sur un array numpy; on applique les params config
        from pipeline.pipeline_double_top.config_double_top import ENABLE_AUTO_PEAKS, AUTO_PEAK_PARAMS
        if ENABLE_AUTO_PEAKS:
            peaks, _ = find_peaks(ps.values, **AUTO_PEAK_PARAMS)
            auto_idx = list(map(int, peaks))
    except Exception as e:
        _log.warning("auto_peaks_failed", error=str(e))
        auto_idx = []

    # 2) pics manuels
    manual_pairs_idx: list[tuple[int, int]] = []
    try:
        from pipeline.pipeline_double_top.config_double_top import MANUAL_PEAKS
        for d1, d2 in MANUAL_PEAKS.get(ticker, []):
            try:
                i1 = ps.index.get_loc(pd.to_datetime(d1))
                i2 = ps.index.get_loc(pd.to_datetime(d2))
                manual_pairs_idx.append((int(i1), int(i2)))
            except Exception:
                continue
    except Exception:
        pass

    # 3) construire des paires (i1, i2) à partir des pics auto (+ vérifier l'écart max LOOKBETWEEN)
    auto_pairs_idx: list[tuple[int, int]] = []
    try:
        from pipeline.pipeline_double_top.config_double_top import LOOKBETWEEN
        for i1 in auto_idx:
            for i2 in auto_idx:
                if 0 < (i2 - i1) <= int(LOOKBETWEEN):
                    auto_pairs_idx.append((i1, i2))
    except Exception:
        # fallback si pas de LOOKBETWEEN
        for i1 in auto_idx:
            for i2 in auto_idx:
                if i2 > i1:
                    auto_pairs_idx.append((i1, i2))

    # 4) concaténer auto + manuels (les manuels passeront aussi par le filtre)
    all_pairs = auto_pairs_idx + manual_pairs_idx

    # 5) filtrer avec nos critères chartistes/tempo
    valid_pairs: list[tuple[int, int]] = []
    for (i1, i2) in all_pairs:
        if is_double_top(ps, i1, i2):
            valid_pairs.append((i1, i2))

    # 6) labelliser au deuxième sommet i2
    for (_, i2) in valid_pairs:
        if 0 <= i2 < len(labels):
            labels[i2] = 1

    elapsed = time.time() - t0
    _log.info("compute_labels_done", n_pos=int(labels.sum()), elapsed_sec=round(elapsed, 3))
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


def is_double_top(
    prices: pd.Series,
    i1: int,
    i2: int,
    *,
    tolerance_pct: float = 0.02,       # tolérance d’égalité des deux sommets (±2%)
    min_valley_drop_pct: float = 0.04, # profondeur min. du creux (4% par défaut)
    min_separation: int = 3,           # jours min entre les deux sommets
    max_separation: int | None = None, # jours max (par défaut: LOOKBETWEEN)
    min_up_pct: float = 0.05,          # tendance haussière préalable min. (5% sur LOOKBACK)
    confirm_break: bool = False,       # demander la confirmation de cassure ?
    confirm_lookahead: int = 5         # nb de jours pour confirmer la cassure du creux
) -> bool:
    """
    Valide si (i1, i2) forment un pattern 'double top' sur la série de PRIX.

    Critères :
      - sommets proches (tolérance en %) ;
      - creux intermédiaire marqué (drop min. en %) ;
      - séparation temporelle entre sommets (min/max jours) ;
      - tendance haussière préalable (sur LOOKBACK jours) ;
      - (optionnel) cassure de la 'neckline' dans les X jours après i2.
    """
    # Sécurité : bornes d’indices
    n = len(prices)
    if not (0 <= i1 < i2 < n):
        return False

    # Paramètre par défaut: max_separation = LOOKBETWEEN si non fourni
    if max_separation is None:
        try:
            from pipeline.pipeline_double_top.config_double_top import LOOKBETWEEN
            max_separation = int(LOOKBETWEEN)
        except Exception:
            max_separation = 20  # fallback

    # 1) Condition temporelle: min/max jours entre les sommets
    sep = i2 - i1
    if sep < int(min_separation) or sep > int(max_separation):
        return False

    # 2) Valeurs des sommets
    p1 = float(prices.iloc[i1])
    p2 = float(prices.iloc[i2])
    if p1 <= 0 or p2 <= 0:
        return False

    # 3) Sommets "proches" en prix (tolérance relative)
    top_ref = max(p1, p2)
    if abs(p1 - p2) / top_ref > float(tolerance_pct):
        return False

    # 4) Creux (neckline) entre i1 et i2
    #    On prend le minimum de prix sur l’intervalle (i1 .. i2)
    valley = float(prices.iloc[i1:i2+1].min())
    # profondeur relative du creux vs le sommet le plus haut
    valley_drop = (top_ref - valley) / top_ref if top_ref > 0 else 0.0
    if valley_drop < float(min_valley_drop_pct):
        return False

    # 5) Tendance haussière préalable sur LOOKBACK jours avant i1
    try:
        from pipeline.pipeline_double_top.config_double_top import LOOKBACK
        lb = int(LOOKBACK)
    except Exception:
        lb = 10  # fallback
    i0 = max(0, i1 - lb)
    pre = float(prices.iloc[i0])
    if pre <= 0:  # sécurité
        return False
    up_move = (p1 / pre) - 1.0
    if up_move < float(min_up_pct):
        return False

    # 6) (Optionnel) Confirmation: cassure sous le creux dans les N jours après i2
    if confirm_break:
        j1 = i2 + 1
        j2 = min(n, i2 + 1 + int(confirm_lookahead))
        # on considère qu’une cassure stricte sous le creux confirme (peut être adouci)
        after = prices.iloc[j1:j2]
        if after.empty or not (after < valley).any():
            return False

    return True
