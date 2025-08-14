# features.py
import structlog
import pandas as pd
from pipeline.pipeline_indicators import compute_rsi, compute_macd
from patterns.continuation_patterns import detect_flag, detect_pennant
from patterns.reversal_patterns import detect_double_top, detect_double_bottom, detect_head_and_shoulders
from patterns.bilateral_patterns import detect_symmetrical_triangle


log = structlog.get_logger("myapp.features")


def build_features_and_labels(df: pd.DataFrame) -> pd.DataFrame:
    """
    Construit les indicateurs, détecte les patterns et crée les labels.
    """
    log.info("Building features and labels")
    data = df.copy()
    # Moyennes et momentum
    data["SMA_20"] = data["Close"].rolling(20).mean()
    data["SMA_50"] = data["Close"].rolling(50).mean()
    data["EMA_20"] = data["Close"].ewm(span=20, adjust=False).mean()
    data["Momentum"] = data["Close"] - data["Close"].shift(5)
    # RSI
    data["RSI"] = compute_rsi(data["Close"])
    # MACD
    data["MACD"], data["MACD_signal"], data["MACD_hist"] = compute_macd(data["Close"])
    # Rendement futur
    from pipeline.pipeline_config import FUTURE_DAYS
    data[f"Return_{FUTURE_DAYS}d"] = data["Close"].shift(-FUTURE_DAYS) / data["Close"] - 1
    # Détection de patterns
    log.debug("Detecting patterns", length=len(data))
    patterns = {
        "flag": detect_flag(data),
        "pennant": detect_pennant(data),
        "double_top": [(d, d) for d in detect_double_top(data)],
        "double_bottom": [(d, d) for d in detect_double_bottom(data)],
        "head_shoulders": detect_head_and_shoulders(data),
        "sym_triangle": detect_symmetrical_triangle(data)
    }
    log.debug("Patterns detected", counts={k: len(v) for k, v in patterns.items()})
    data["pattern_label"] = "none"
    # Attribution des labels
    for name, occ in patterns.items():
        if name == "head_shoulders":
            for l, head, r in occ:
                data.at[head, "pattern_label"] = name
        else:
            for s, e in occ:
                data.loc[s:e, "pattern_label"] = name
    data.dropna(inplace=True)
    log.debug("Final dataset prepared", rows=len(data))
    return data