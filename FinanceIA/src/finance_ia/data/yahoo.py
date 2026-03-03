from __future__ import annotations

import logging
from typing import Any

import pandas as pd
import yfinance as yf

LOGGER = logging.getLogger(__name__)
REQUIRED_COLUMNS = ["Open", "High", "Low", "Close", "Volume"]


def _normalize_ohlcv_frame(frame: pd.DataFrame) -> pd.DataFrame:
    if frame.empty:
        raise ValueError("Received an empty OHLCV frame")

    missing = [column for column in REQUIRED_COLUMNS if column not in frame.columns]
    if missing:
        raise ValueError(f"Missing required columns: {missing}")

    data = frame[REQUIRED_COLUMNS].copy()
    data = data.dropna()
    if data.empty:
        raise ValueError("OHLCV frame is empty after dropping NaN rows")

    data.index = pd.to_datetime(data.index).tz_localize(None)
    return data


def fetch_ohlcv(
    ticker: str,
    *,
    start: str | None = None,
    end: str | None = None,
    period: str | None = None,
    interval: str = "1d",
) -> pd.DataFrame:
    """Fetch a normalized OHLCV frame from Yahoo Finance."""
    if start and period:
        raise ValueError("Use start/end or period, not both")

    ticker = ticker.strip().upper()
    if not ticker:
        raise ValueError("Ticker must not be empty")

    history_kwargs: dict[str, Any] = {
        "interval": interval,
        "auto_adjust": False,
        "actions": False,
    }

    if start:
        history_kwargs["start"] = start
        history_kwargs["end"] = end
    else:
        history_kwargs["period"] = period or "1y"

    frame = yf.Ticker(ticker).history(**history_kwargs)
    return _normalize_ohlcv_frame(frame)


def fetch_many_ohlcv(
    tickers: list[str],
    *,
    start: str,
    end: str,
    interval: str,
) -> dict[str, pd.DataFrame]:
    """Fetch OHLCV frames for many tickers and skip invalid ones."""
    output: dict[str, pd.DataFrame] = {}
    for ticker in tickers:
        try:
            output[ticker] = fetch_ohlcv(ticker, start=start, end=end, interval=interval)
        except Exception as error:  # pragma: no cover - defensive logging
            LOGGER.warning("Failed to fetch ticker %s: %s", ticker, error)
    return output
