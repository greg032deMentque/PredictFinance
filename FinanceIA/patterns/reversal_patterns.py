# reversal_patterns.py

import pandas as pd
import structlog
from typing import List, Tuple

# Logger setup
log = structlog.get_logger("myapp.reversal_patterns")

def detect_double_top(df: pd.DataFrame, window: int = 20, tol: float = 0.01) -> List[pd.Timestamp]:
    """
    Repère les « double top » : deux sommets à peu près égaux séparés par un creux.
    - window : nombre de jours sur lequel chercher le motif
    - tol    : tolérance relative pour égalité des sommets

    Retourne la date du deuxième sommet pour chaque motif.
    """
    log.debug("detect_double_top called", window=window, tol=tol)
    tops = []
    for i in range(window, len(df)):
        segment = df["Close"].iloc[i-window:i]
        # sommets
        peak_vals = segment.nlargest(2)
        peak1, peak2 = peak_vals.index[1], peak_vals.index[0]
        price1 = df["Close"].loc[peak1]
        price2 = df["Close"].loc[peak2]
        trough = segment.min()
        # tolérance pour sommets
        if abs(price1 - price2) / price1 < tol:
            # vérifier que le creux est nettement plus bas
            if trough < min(price1, price2) * (1 - tol*2):
                tops.append(peak2)
                log.info("Double top detected", date=peak2, price1=price1, price2=price2, trough=trough)
    log.debug("detect_double_top finished", count=len(tops))
    return tops


def detect_double_bottom(df: pd.DataFrame, window: int = 20, tol: float = 0.01) -> List[pd.Timestamp]:
    """
    Repère les « double bottom » : inverse du double top.
    """
    log.debug("detect_double_bottom called", window=window, tol=tol)
    bottoms = []
    for i in range(window, len(df)):
        segment = df["Close"].iloc[i-window:i]
        # creux
        low_vals = segment.nsmallest(2)
        low1, low2 = low_vals.index[0], low_vals.index[1]
        p1, p2 = df["Close"].loc[low1], df["Close"].loc[low2]
        peak = segment.max()
        # tolérance pour creux
        if abs(p1 - p2) / p1 < tol:
            if peak > max(p1, p2) * (1 + tol*2):
                bottoms.append(low2)
                log.info("Double bottom detected", date=low2, price1=p1, price2=p2, peak=peak)
    log.debug("detect_double_bottom finished", count=len(bottoms))
    return bottoms


def detect_head_and_shoulders(df: pd.DataFrame, window: int = 30) -> List[Tuple[pd.Timestamp, pd.Timestamp, pd.Timestamp]]:
    """
    Repère les têtes et épaules :
    trois sommets, le central plus élevé.
    - window : fenêtre d'analyse

    Retourne une liste de triplets (épaule gauche, tête, épaule droite).
    """
    log.debug("detect_head_and_shoulders called", window=window)
    patterns = []
    for i in range(window, len(df)):
        seg = df["Close"].iloc[i-window:i]
        peaks = seg.nlargest(3)
        # indices triés chronologiquement
        idxs = sorted(peaks.index)
        p_vals = [df["Close"].loc[idx] for idx in idxs]
        # tête au milieu et plus haute
        if p_vals[1] == max(p_vals):
            patterns.append((idxs[0], idxs[1], idxs[2]))
            log.info(
                "Head and Shoulders detected",
                left_shoulder=idxs[0], head=idxs[1], right_shoulder=idxs[2]
            )
    log.debug("detect_head_and_shoulders finished", count=len(patterns))
    return patterns
