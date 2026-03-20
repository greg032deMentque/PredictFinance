from __future__ import annotations

import numpy as np
import pandas as pd

from finance_ia.config import FEATURE_COLUMNS, TrainConfig
from finance_ia.dataset.build_dataset import build_training_frame, temporal_train_test_split


def _make_ticker_frame(ticker: str) -> pd.DataFrame:
    index = pd.date_range("2019-01-01", periods=180, freq="D")
    x = np.linspace(0.0, 16.0, len(index))
    trend = np.linspace(90.0, 130.0, len(index))
    close = trend + 3.0 * np.sin(x)

    # Inject a simple and clear double top pattern.
    close[80] = close[79] + 6.0
    close[90] = close[89] - 5.5
    close[100] = close[99] + 6.2

    open_price = close + 0.2
    high = close + 0.8
    low = close - 0.8
    volume = np.full(len(index), 5_000 + len(ticker))

    return pd.DataFrame(
        {"Open": open_price, "High": high, "Low": low, "Close": close, "Volume": volume},
        index=index,
    )


def test_build_training_frame_aligns_schema(monkeypatch) -> None:
    monkeypatch.setattr(
        "finance_ia.dataset.build_dataset.fetch_many_ohlcv",
        lambda tickers, **_: {ticker: _make_ticker_frame(ticker) for ticker in tickers},
    )

    config = TrainConfig(tickers=["AAPL", "MSFT"], start="2019-01-01", end="2020-01-01")
    frame = build_training_frame(config)

    assert not frame.empty
    assert set(frame["target"].unique()).issubset({0, 1})
    assert frame["date"].is_monotonic_increasing
    for column in FEATURE_COLUMNS:
        assert column in frame.columns


def test_build_training_frame_skips_missing_ticker_frames(monkeypatch) -> None:
    monkeypatch.setattr(
        "finance_ia.dataset.build_dataset.fetch_many_ohlcv",
        lambda tickers, **_: {"AAPL": _make_ticker_frame("AAPL")},
    )

    config = TrainConfig(tickers=["AAPL", "MSFT"], start="2019-01-01", end="2020-01-01")
    frame = build_training_frame(config)

    assert not frame.empty
    assert set(frame["ticker"].unique()) == {"AAPL"}


def test_temporal_train_test_split_is_chronological() -> None:
    frame = pd.DataFrame({"date": pd.date_range("2023-01-01", periods=20, freq="D"), "target": [0] * 20})
    train_frame, test_frame = temporal_train_test_split(frame, test_size=0.2)

    assert len(train_frame) + len(test_frame) == len(frame)
    assert train_frame["date"].max() < test_frame["date"].min()
