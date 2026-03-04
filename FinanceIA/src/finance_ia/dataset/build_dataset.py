from __future__ import annotations

import pandas as pd

from finance_ia.config import FEATURE_COLUMNS, TrainConfig
from finance_ia.data.yahoo import fetch_ohlcv
from finance_ia.features.double_top import build_double_top_labels
from finance_ia.features.indicators import add_indicators


def build_training_frame(config: TrainConfig) -> pd.DataFrame:
    """Build a multi-ticker tabular dataset with binary targets."""
    rows: list[pd.DataFrame] = []

    for ticker in config.tickers:
        raw = fetch_ohlcv(ticker, start=config.start, end=config.end, interval=config.interval)
        enriched = add_indicators(raw)
        labels = build_double_top_labels(enriched, config.pattern)

        ticker_frame = enriched.copy()
        ticker_frame["target"] = labels
        ticker_frame["date"] = pd.to_datetime(ticker_frame.index).tz_localize(None)
        ticker_frame["ticker"] = ticker

        ticker_frame = ticker_frame.dropna(subset=FEATURE_COLUMNS)
        if ticker_frame.empty:
            continue

        rows.append(ticker_frame[FEATURE_COLUMNS + ["target", "date", "ticker"]])

    if not rows:
        raise ValueError("No training rows were generated")

    dataset = pd.concat(rows, axis=0, ignore_index=True)
    dataset = dataset.sort_values(["date", "ticker"], kind="mergesort").reset_index(drop=True)
    return dataset


def temporal_train_test_split(frame: pd.DataFrame, test_size: float) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Split the dataset by chronology without shuffling."""
    if frame.empty:
        raise ValueError("Cannot split an empty frame")
    if len(frame) < 2:
        raise ValueError("Need at least 2 rows for train/test split")

    split_index = int(len(frame) * (1.0 - test_size))
    split_index = max(1, min(split_index, len(frame) - 1))

    train_frame = frame.iloc[:split_index].copy()
    test_frame = frame.iloc[split_index:].copy()
    return train_frame, test_frame


def temporal_train_val_test_split(
    frame: pd.DataFrame,
    *,
    val_size: float,
    test_size: float,
) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    """Split chronology into train/validation/test without shuffling."""
    if frame.empty:
        raise ValueError("Cannot split an empty frame")
    if len(frame) < 3:
        raise ValueError("Need at least 3 rows for train/validation/test split")
    if not (0.05 <= val_size <= 0.3):
        raise ValueError("val_size must be between 0.05 and 0.3")
    if not (0.05 <= test_size <= 0.5):
        raise ValueError("test_size must be between 0.05 and 0.5")
    if (val_size + test_size) >= 0.7:
        raise ValueError("val_size + test_size must remain below 0.7")

    total_rows = len(frame)
    train_end = int(total_rows * (1.0 - val_size - test_size))
    val_end = int(total_rows * (1.0 - test_size))

    train_end = max(1, min(train_end, total_rows - 2))
    val_end = max(train_end + 1, min(val_end, total_rows - 1))

    train_frame = frame.iloc[:train_end].copy()
    val_frame = frame.iloc[train_end:val_end].copy()
    test_frame = frame.iloc[val_end:].copy()
    return train_frame, val_frame, test_frame
