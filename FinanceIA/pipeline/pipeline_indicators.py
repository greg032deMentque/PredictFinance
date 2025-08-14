# indicators.py
import pandas as pd
import structlog
from pipeline.pipeline_config import RSI_PERIOD, MACD_FAST, MACD_SLOW, MACD_SIGNAL

log = structlog.get_logger("myapp.indicators")


def compute_rsi(series: pd.Series, period: int = RSI_PERIOD) -> pd.Series:
    """
    Calcule le RSI pour une série de prix.
    """
    log.debug("Computing RSI", period=period)
    delta = series.diff()
    gain = delta.clip(lower=0)
    loss = -delta.clip(upper=0)
    avg_gain = gain.ewm(alpha=1/period, min_periods=period).mean()
    avg_loss = loss.ewm(alpha=1/period, min_periods=period).mean()
    rs = avg_gain / avg_loss
    rsi = 100 - (100 / (1 + rs))
    log.debug("RSI computed", head=rsi.head(3).tolist())
    return rsi


def compute_macd(series: pd.Series, fast: int = MACD_FAST, slow: int = MACD_SLOW, signal: int = MACD_SIGNAL):
    """
    Calcule le MACD pour une série de prix.
    """
    log.debug("Computing MACD", fast=fast, slow=slow, signal=signal)
    ema_fast = series.ewm(span=fast, adjust=False).mean()
    ema_slow = series.ewm(span=slow, adjust=False).mean()
    macd_line = ema_fast - ema_slow
    signal_line = macd_line.ewm(span=signal, adjust=False).mean()
    hist = macd_line - signal_line
    log.debug("MACD computed", head=macd_line.head(3).tolist(), signal_head=signal_line.head(3).tolist())
    return macd_line, signal_line, hist