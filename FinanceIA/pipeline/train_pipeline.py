# train_pipeline.py

import os
import joblib
import pandas as pd
import numpy as np
import yfinance as yf
import structlog
from sklearn.model_selection import train_test_split, GridSearchCV
from sklearn.metrics import classification_report, confusion_matrix, mean_squared_error, r2_score
from xgboost import XGBClassifier, XGBRegressor
from sklearn.neural_network import MLPClassifier, MLPRegressor
from patterns.continuation_patterns import detect_flag, detect_pennant
from patterns.reversal_patterns import detect_double_top, detect_double_bottom, detect_head_and_shoulders
from patterns.bilateral_patterns import detect_symmetrical_triangle

# Logger setup
log = structlog.get_logger("myapp.train_pipeline")

# ---------------- Parameters ----------------
# Define parameters for data fetching and model training
TICKER = "AAPL"  # Stock ticker symbol
START_DATE = "2018-01-01"  # Start date for data fetching
END_DATE = "2023-12-31"  # End date for data fetching
RESULTS_DIR = "models"  # Directory to save models
FUTURE_DAYS = 5  # Horizon for return calculation
RSI_PERIOD = 14  # Period for RSI calculation
MACD_FAST = 12  # Fast period for MACD
MACD_SLOW = 26  # Slow period for MACD
MACD_SIGNAL = 9  # Signal period for MACD
TEST_SIZE = 0.2  # Test size for train-test split
RANDOM_STATE = 42  # Random state for reproducibility

# ---------------- Technical Indicators ----------------
def compute_rsi(series: pd.Series, period: int = RSI_PERIOD) -> pd.Series:
    """
    Compute the Relative Strength Index (RSI) for a given series.

    Args:
        series (pd.Series): The price series to compute RSI for.
        period (int): The period for RSI calculation.

    Returns:
        pd.Series: The RSI values.
    """
    log.debug("Computing RSI", period=period)
    delta = series.diff()  # Compute price differences
    gain = delta.clip(lower=0)  # Gain is positive delta
    loss = -delta.clip(upper=0)  # Loss is negative delta
    avg_gain = gain.ewm(alpha=1/period, min_periods=period).mean()  # Exponential moving average of gain
    avg_loss = loss.ewm(alpha=1/period, min_periods=period).mean()  # Exponential moving average of loss
    rs = avg_gain / avg_loss  # Relative strength
    rsi = 100 - (100 / (1 + rs))  # RSI formula
    log.debug("RSI computed", rsi_head=rsi.head(3).tolist())
    return rsi


def compute_macd(series: pd.Series, fast: int = MACD_FAST, slow: int = MACD_SLOW, signal: int = MACD_SIGNAL):
    """
    Compute the Moving Average Convergence Divergence (MACD) for a given series.

    Args:
        series (pd.Series): The price series to compute MACD for.
        fast (int): The fast period for MACD.
        slow (int): The slow period for MACD.
        signal (int): The signal period for MACD.

    Returns:
        tuple: MACD line, signal line, and histogram.
    """
    log.debug("Computing MACD", fast=fast, slow=slow, signal=signal)
    ema_fast = series.ewm(span=fast, adjust=False).mean()  # Fast exponential moving average
    ema_slow = series.ewm(span=slow, adjust=False).mean()  # Slow exponential moving average
    macd_line = ema_fast - ema_slow  # MACD line
    signal_line = macd_line.ewm(span=signal, adjust=False).mean()  # Signal line
    hist = macd_line - signal_line  # Histogram
    log.debug("MACD computed", macd_head=macd_line.head(3).tolist(), signal_head=signal_line.head(3).tolist())
    return macd_line, signal_line, hist

# ---------------- Data Fetching ----------------
def fetch_data(ticker: str, start: str, end: str) -> pd.DataFrame:
    """
    Fetch historical data for a given ticker and date range.

    Args:
        ticker (str): The stock ticker symbol.
        start (str): The start date for data fetching.
        end (str): The end date for data fetching.

    Returns:
        pd.DataFrame: The fetched historical data.
    """
    log.info("Fetching data", ticker=ticker, start=start, end=end)
    df = yf.Ticker(ticker).history(start=start, end=end, auto_adjust=False)  # Fetch data using yfinance
    df = df[["Open", "High", "Low", "Close", "Volume"]]  # Select relevant columns
    df.dropna(inplace=True)  # Drop rows with missing values
    log.debug("Data fetched", rows=len(df))
    return df

# ---------------- Feature Engineering & Labeling ----------------
def build_features_and_labels(df: pd.DataFrame) -> pd.DataFrame:
    """
    Build features and labels for the given DataFrame.

    Args:
        df (pd.DataFrame): The input DataFrame containing historical data.

    Returns:
        pd.DataFrame: The DataFrame with added features and labels.
    """
    log.info("Building features and labels")
    data = df.copy()
    # Standard indicators
    data["SMA_20"] = data["Close"].rolling(20).mean()  # 20-day simple moving average
    data["SMA_50"] = data["Close"].rolling(50).mean()  # 50-day simple moving average
    data["EMA_20"] = data["Close"].ewm(span=20, adjust=False).mean()  # 20-day exponential moving average
    data["Momentum"] = data["Close"] - data["Close"].shift(5)  # Momentum over 5 days
    # RSI
    data["RSI"] = compute_rsi(data["Close"], period=RSI_PERIOD)  # Relative Strength Index
    # MACD components
    data["MACD"], data["MACD_signal"], data["MACD_hist"] = compute_macd(
        data["Close"], fast=MACD_FAST, slow=MACD_SLOW, signal=MACD_SIGNAL
    )  # MACD components
    # Future return for regression
    data[f"Return_{FUTURE_DAYS}d"] = data["Close"].shift(-FUTURE_DAYS) / data["Close"] - 1  # Future return over FUTURE_DAYS
    # Detect patterns
    log.debug("Detecting patterns on data length", length=len(data))
    patterns = {
        "flag": detect_flag(data),  # Detect flag pattern
        "pennant": detect_pennant(data),  # Detect pennant pattern
        "double_top": [(d, d) for d in detect_double_top(data)],  # Detect double top pattern
        "double_bottom": [(d, d) for d in detect_double_bottom(data)],  # Detect double bottom pattern
        "head_shoulders": detect_head_and_shoulders(data),  # Detect head and shoulders pattern
        "sym_triangle": detect_symmetrical_triangle(data)  # Detect symmetrical triangle pattern
    }
    log.debug("Patterns detected", counts={k: len(v) for k, v in patterns.items()})
    # Initialize multiclass label ("none" or pattern name)
    data["pattern_label"] = "none"
    # Assign labels; if multiple patterns overlap, priority based on dict order
    for name, occ in patterns.items():
        if name == "head_shoulders":
            # Mark only the head date
            for l, head, r in occ:
                data.at[head, "pattern_label"] = name
        else:
            for start, end in occ:
                data.loc[start:end, "pattern_label"] = name
    # Drop NaNs
    data.dropna(inplace=True)  # Drop rows with missing values
    log.debug("Final dataset prepared", rows=len(data))
    return data

# ---------------- Dataset Preparation ----------------
def prepare_datasets(df: pd.DataFrame, predict: str = "classification"):  # type: ignore
    """
    Prepare datasets for training and testing.

    Args:
        df (pd.DataFrame): The input DataFrame containing features and labels.
        predict (str): The type of prediction task ("classification" or "regression").

    Returns:
        tuple: Training and testing datasets.
    """
    log.info("Preparing datasets", predict=predict)
    features = ["SMA_20", "SMA_50", "EMA_20", "Momentum", "Volume", "RSI", "MACD", "MACD_signal", "MACD_hist"]
    X = df[features]
    if predict == "classification":
        y = df["pattern_label"]  # Labels for classification
    else:
        y = df[f"Return_{FUTURE_DAYS}d"]  # Labels for regression
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=TEST_SIZE, shuffle=False
    )  # Split into training and testing sets
    log.debug("Datasets split", X_train_rows=len(X_train), X_test_rows=len(X_test))
    return X_train, X_test, y_train, y_test

# ---------------- Training & Evaluation ----------------
def train_classification(X_train, X_test, y_train, y_test):
    """
    Train and evaluate classification models.

    Args:
        X_train (pd.DataFrame): Training features.
        X_test (pd.DataFrame): Testing features.
        y_train (pd.Series): Training labels.
        y_test (pd.Series): Testing labels.

    Returns:
        dict: Trained classification models.
    """
    log.info("Training classification models")
    # XGBoost Classifier
    xgb = XGBClassifier(random_state=RANDOM_STATE, use_label_encoder=False, eval_metric="mlogloss")
    params_xgb = {"n_estimators": [50, 100], "max_depth": [3, 6]}
    grid_xgb = GridSearchCV(xgb, params_xgb, cv=3, scoring="accuracy", n_jobs=-1)
    grid_xgb.fit(X_train, y_train)
    best_xgb = grid_xgb.best_estimator_
    log.debug("Best XGB classifier", params=grid_xgb.best_params_)

    # MLP Classifier
    mlp = MLPClassifier(random_state=RANDOM_STATE, max_iter=300)
    params_mlp = {"hidden_layer_sizes": [(50,), (100,)], "alpha": [0.0001, 0.001]}
    grid_mlp = GridSearchCV(mlp, params_mlp, cv=3, scoring="accuracy", n_jobs=-1)
    grid_mlp.fit(X_train, y_train)
    best_mlp = grid_mlp.best_estimator_
    log.debug("Best MLP classifier", params=grid_mlp.best_params_)

    # Evaluate models
    for name, model in [("XGBoost", best_xgb), ("MLP", best_mlp)]:
        preds = model.predict(X_test)
        log.info("Classification report", model=name)
        print(f"=== {name} Classification ===")
        print(classification_report(y_test, preds))
        print(confusion_matrix(y_test, preds))

    return {"xgb": best_xgb, "mlp": best_mlp}


def train_regression(X_train, X_test, y_train, y_test):
    """
    Train and evaluate regression models.

    Args:
        X_train (pd.DataFrame): Training features.
        X_test (pd.DataFrame): Testing features.
        y_train (pd.Series): Training labels.
        y_test (pd.Series): Testing labels.

    Returns:
        dict: Trained regression models.
    """
    log.info("Training regression models")
    # XGBoost Regressor
    xgb_r = XGBRegressor(random_state=RANDOM_STATE)
    params_xgb_r = {"n_estimators": [50, 100], "max_depth": [3, 6]}
    grid_xgb_r = GridSearchCV(xgb_r, params_xgb_r, cv=3, scoring="neg_mean_squared_error", n_jobs=-1)
    grid_xgb_r.fit(X_train, y_train)
    best_xgb_r = grid_xgb_r.best_estimator_
    log.debug("Best XGB regressor", params=grid_xgb_r.best_params_)

    # MLP Regressor
    mlp_r = MLPRegressor(random_state=RANDOM_STATE, max_iter=300)
    params_mlp_r = {"hidden_layer_sizes": [(50,), (100,)], "alpha": [0.0001, 0.001]}
    grid_mlp_r = GridSearchCV(mlp_r, params_mlp_r, cv=3, scoring="neg_mean_squared_error", n_jobs=-1)
    grid_mlp_r.fit(X_train, y_train)
    best_mlp_r = grid_mlp_r.best_estimator_
    log.debug("Best MLP regressor", params=grid_mlp_r.best_params_)

    # Evaluate models
    for name, model in [("XGBoostReg", best_xgb_r), ("MLPReg", best_mlp_r)]:
        preds = model.predict(X_test)
        log.info("Regression evaluation", model=name)
        print(f"=== {name} Regression ===")
        print("MSE:", mean_squared_error(y_test, preds))
        print("R2 :", r2_score(y_test, preds))

    return {"xgb_r": best_xgb_r, "mlp_r": best_mlp_r}

# ---------------- Main Pipeline ----------------

def main():
    """
    Main pipeline for fetching data, building features, training models, and saving results.
    """
    log.info("Pipeline started", ticker=TICKER, start=START_DATE, end=END_DATE)
    print(f"Fetching data for {TICKER} from {START_DATE} to {END_DATE}…")
    df = fetch_data(TICKER, START_DATE, END_DATE)
    log.info("Data fetching completed", rows=len(df))

    print("Building features and labels…")
    data = build_features_and_labels(df)
    log.info("Feature building completed", rows=len(data))

    # Classification
    print("Training classification models…")
    X_train, X_test, y_train, y_test = prepare_datasets(data, predict="classification")
    clf_models = train_classification(X_train, X_test, y_train, y_test)
    log.info("Classification training completed")

    # Save classifiers
    os.makedirs(RESULTS_DIR, exist_ok=True)
    joblib.dump(clf_models, os.path.join(RESULTS_DIR, "classifiers.pkl"))
    log.info("Classifiers saved", path=os.path.join(RESULTS_DIR, "classifiers.pkl"))

    # Regression
    print("Training regression models…")
    X_train_r, X_test_r, y_train_r, y_test_r = prepare_datasets(data, predict="regression")
    reg_models = train_regression(X_train_r, X_test_r, y_train_r, y_test_r)
    log.info("Regression training completed")

    # Save regressors
    joblib.dump(reg_models, os.path.join(RESULTS_DIR, "regressors.pkl"))
    log.info("Regressors saved", path=os.path.join(RESULTS_DIR, "regressors.pkl"))

    print(f"Models saved in {RESULTS_DIR}/")
    log.info("Pipeline finished successfully")

if __name__ == "__main__":
    main()
