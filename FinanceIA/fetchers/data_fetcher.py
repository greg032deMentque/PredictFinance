"""
Module pour récupérer les données boursières avec yfinance.
"""

import logging
from typing import Optional, Union, List

import pandas as pd
import yfinance as yf
import structlog
from pandas import DataFrame

# Configuration du logger

log = structlog.get_logger("myapp.pattern_detector")


def fetch_data(
    tickers: Union[str, List[str]],
    start: Optional[str] = None,
    end:   Optional[str] = None,
    period: Optional[str] = "1y",
    interval: str = "1d",
    auto_adjust: bool = True,
    actions: bool = False,
    threads: bool = True,
    retry: int = 3
) -> DataFrame | None:
    """
    Télécharge l'historique des cours pour un ou plusieurs tickers.

    Args:
        tickers: Symbole(s) boursier(s) (ex. "AAPL" ou ["AAPL", "MSFT"]).
        start: Date de début ("YYYY-MM-DD"). Si fournie, `period` est ignoré.
        end:   Date de fin ("YYYY-MM-DD"). Optionnel.
        period: Période relative ("1y", "6mo", "30d", etc.), ignoré si `start` est défini.
        interval: Fréquence des données ("1d", "1h", "1m", etc.).
        auto_adjust: Si True, ajustement des prix (DPI, splits) automatiquement.
        actions: Si True, inclut également dividendes et splits.
        threads: Si True, télécharge en parallèle (utile pour plusieurs tickers).
        retry: Nombre de tentatives en cas d’échec réseau.

    Returns:
        DataFrame pandas contenant pour chaque date les colonnes
        Open, High, Low, Close, Volume (et éventuellement Dividends, Stock Splits).

    Raises:
        Exception si le téléchargement échoue après `retry` tentatives.
    """
    for attempt in range(1, retry + 1):
        try:
            # Pour un seul ticker, on peut utiliser Ticker.history
            if isinstance(tickers, str):
                log.info("Downloading single ticker", ticker=tickers, period=period, interval=interval)
                df = yf.Ticker(tickers).history(
                    start=start, end=end, period=None if start else period,
                    interval=interval, auto_adjust=auto_adjust, actions=actions
                )
            # Pour plusieurs tickers, on préfère yf.download
            else:
                log.info("Downloading multiple tickers", count=len(tickers), tickers=tickers)
                df = yf.download(
                    tickers,
                    start=start, end=end, period=None if start else period,
                    interval=interval, auto_adjust=auto_adjust,
                    actions=actions, threads=threads
                )
            if df.empty:
                raise ValueError("Le DataFrame retourné est vide.")
            return df

        except Exception as e:
            log.warning(f"Tentative {attempt}/{retry} échouée : {e}")
            if attempt == retry:
                log.error("Échec définitif après plusieurs tentatives.")
                raise
            else:
                # Recommence après un court délai (back-off simple)
                import time; time.sleep(2 ** attempt)
    return None

