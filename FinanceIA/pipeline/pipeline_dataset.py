# dataset.py
import structlog
from sklearn.model_selection import train_test_split
import pandas as pd
from pipeline.pipeline_config import test_size, RANDOM_STATE

log = structlog.get_logger("myapp.dataset")


def prepare_datasets(df: pd.DataFrame, predict: str = "classification"):
    """
    Crée X_train, X_test, y_train, y_test pour classification ou régression.
    """
    log.info("Preparing datasets", predict=predict)
    features = ["SMA_20", "SMA_50", "EMA_20", "Momentum", "Volume", "RSI", "MACD", "MACD_signal", "MACD_hist"]
    X = df[features]
    if predict == "classification":
        y = df["pattern_label"]
    else:
        from pipeline.pipeline_config import FUTURE_DAYS
        y = df[f"Return_{FUTURE_DAYS}d"]
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=test_size, shuffle=False, random_state=RANDOM_STATE
    )
    log.debug("Datasets split", train_rows=len(X_train), test_rows=len(X_test))
    return X_train, X_test, y_train, y_test