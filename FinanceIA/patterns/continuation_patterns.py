# continuation_patterns.py

import pandas as pd
import structlog
from typing import List, Tuple

# Logger setup
log = structlog.get_logger("myapp.continuation_patterns")

def detect_flag(df: pd.DataFrame, length: int = 10, tol: float = 0.02) -> List[Tuple[pd.Timestamp, pd.Timestamp]]:
    """
    Repère des drapeaux (flags) : une forte impulsion suivie d'une consolidation plate.
    - length : taille de la fenêtre pour la consolidation
    - tol    : tolérance (en % de variation) pour considérer le canal horizontal

    Retourne une liste de tuples (start, end) d'index où apparaît chaque flag.
    """
    log.debug("detect_flag called", length=length, tol=tol)
    flags: List[Tuple[pd.Timestamp, pd.Timestamp]] = []
    for i in range(length, len(df) - length):
        # 1) impulsion : forte hausse sur les `length` jours précédents
        price_then = df["Close"].iloc[i - length]
        price_now  = df["Close"].iloc[i]
        impulse = (price_now - price_then) / price_then
        if impulse < 0.05:
            continue  # pas assez d'impulsion

        # 2) consolidation : dans la fenêtre suivante, le prix reste dans [1±tol]
        window = df["Close"].iloc[i : i + length]
        pct_move = (window.max() - window.min()) / window.min()
        if pct_move < tol:
            start, end = df.index[i], df.index[i + length - 1]
            flags.append((start, end))
            log.info(
                "Flag detected",
                start=start,
                end=end,
                impulse=impulse,
                pct_move=pct_move
            )
    log.debug("detect_flag finished", count=len(flags))
    return flags


def detect_pennant(df: pd.DataFrame, length: int = 10) -> List[Tuple[pd.Timestamp, pd.Timestamp]]:
    """
    Repère des pennants : impulsion suivie d'un triangle convergent.
    - length : taille de la fenêtre pour recherche du pennant

    Retourne une liste de tuples (start, end) d'index pour chaque pennant détecté.
    """
    log.debug("detect_pennant called", length=length)
    pennants: List[Tuple[pd.Timestamp, pd.Timestamp]] = []
    for i in range(length, len(df) - length):
        # 1) impulsion : même logique que pour les flags
        price_then = df["Close"].iloc[i - length]
        price_now  = df["Close"].iloc[i]
        impulse = (price_now - price_then) / price_then
        if impulse < 0.05:
            continue

        # 2) triangle convergent : on mesure la pente haute et basse
        window = df.iloc[i : i + length]
        highs = window["High"].values
        lows  = window["Low"].values
        # pente descendante des hauts
        slope_h = (highs[-1] - highs[0]) / length
        # pente montante des bas
        slope_l = (lows[-1] - lows[0]) / length
        if slope_h < 0 and slope_l > 0:
            start, end = df.index[i], df.index[i + length - 1]
            pennants.append((start, end))
            log.info(
                "Pennant detected",
                start=start,
                end=end,
                impulse=impulse,
                slope_h=slope_h,
                slope_l=slope_l
            )
    log.debug("detect_pennant finished", count=len(pennants))
    return pennants
