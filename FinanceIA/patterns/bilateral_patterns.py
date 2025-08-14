# bilateral_patterns.py

import pandas as pd
import structlog
from typing import List, Tuple
import numpy as np

# Logger setup
log = structlog.get_logger("myapp.bilateral_patterns")

def detect_symmetrical_triangle(df: pd.DataFrame, length: int = 20, tol: float = 0.001) -> List[Tuple[pd.Timestamp, pd.Timestamp]]:
    """
    Repère un triangle symétrique : hauts décroitants et bas croissants convergeant.
    - length : taille de la fenêtre
    - tol    : tolérance sur les pentes pour les qualifier de convergentes

    Retourne une liste de tuples (début, fin) de chaque triangle.
    """
    log.debug("detect_symmetrical_triangle called", length=length, tol=tol)
    triangles: List[Tuple[pd.Timestamp, pd.Timestamp]] = []
    x = np.arange(length)
    for i in range(length, len(df) - length):
        window = df.iloc[i : i + length]
        highs = window["High"].values
        lows  = window["Low"].values
        # calcul des pentes via régression linéaire simple
        slope_h = np.polyfit(x, highs, 1)[0]
        slope_l = np.polyfit(x, lows, 1)[0]
        # hauts baissent, bas montent
        if slope_h < 0 and slope_l > 0 and abs(slope_h) > tol and abs(slope_l) > tol:
            start, end = df.index[i], df.index[i + length - 1]
            triangles.append((start, end))
            log.info(
                "Symmetrical triangle detected",
                start=start,
                end=end,
                slope_h=slope_h,
                slope_l=slope_l
            )
    log.debug("detect_symmetrical_triangle finished", count=len(triangles))
    return triangles
