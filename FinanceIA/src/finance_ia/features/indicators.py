from __future__ import annotations

import numpy as np
import pandas as pd


def _compute_rsi(close: pd.Series, period: int = 14) -> pd.Series:
    delta = close.diff()
    gain = delta.clip(lower=0)
    loss = -delta.clip(upper=0)

    avg_gain = gain.ewm(alpha=1.0 / period, min_periods=period, adjust=False).mean()
    avg_loss = loss.ewm(alpha=1.0 / period, min_periods=period, adjust=False).mean()

    rs = avg_gain / avg_loss.replace(0.0, np.nan)
    rsi = 100 - (100 / (1 + rs))

    # Degenerate windows should still return a valid RSI value:
    # only gains -> 100, only losses -> 0, no movement -> 50.
    no_losses = avg_loss == 0.0
    no_gains = avg_gain == 0.0
    flat = no_losses & no_gains

    rsi = rsi.mask(no_losses, 100.0)
    rsi = rsi.mask(no_gains, 0.0)
    rsi = rsi.mask(flat, 50.0)
    return rsi


def add_indicators(frame: pd.DataFrame) -> pd.DataFrame:
    """Add simple indicators without future leakage."""
    data = frame.copy()

    close = data["Close"]
    volume = data["Volume"]

    data["ret_1d"] = close.pct_change()
    data["ret_5d"] = close.pct_change(5)

    data["sma_10"] = close.rolling(10).mean()
    data["sma_20"] = close.rolling(20).mean()
    data["ema_10"] = close.ewm(span=10, adjust=False).mean()
    data["ema_20"] = close.ewm(span=20, adjust=False).mean()

    data["rsi_14"] = _compute_rsi(close, period=14)

    ema_12 = close.ewm(span=12, adjust=False).mean()
    ema_26 = close.ewm(span=26, adjust=False).mean()
    data["macd"] = ema_12 - ema_26
    data["macd_signal"] = data["macd"].ewm(span=9, adjust=False).mean()
    data["macd_hist"] = data["macd"] - data["macd_signal"]

    data["volatility_10"] = data["ret_1d"].rolling(10).std()

    volume_ma_20 = volume.rolling(20).mean().replace(0.0, np.nan)
    data["volume_norm_20"] = volume / volume_ma_20

    return data
