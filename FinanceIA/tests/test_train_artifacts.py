from __future__ import annotations

import numpy as np
import pandas as pd

from finance_ia.config import FEATURE_COLUMNS, TrainConfig
from finance_ia.io.artifacts import (
    FEATURE_COLUMNS_FILE,
    METRICS_FILE,
    MODEL_FILE,
    TRAIN_CONFIG_FILE,
    load_feature_columns,
    load_model,
    save_training_artifacts,
)
from finance_ia.model.train import train_model


def _synthetic_training_frame(rows: int = 240) -> pd.DataFrame:
    rng = np.random.default_rng(7)
    date = pd.date_range("2021-01-01", periods=rows, freq="D")
    frame = pd.DataFrame({column: rng.normal(0.0, 1.0, rows) for column in FEATURE_COLUMNS})
    signal = frame["macd"] + frame["ret_1d"] + (0.5 * frame["volume_norm_20"])
    threshold = float(np.quantile(signal, 0.70))
    frame["target"] = (signal > threshold).astype("int64")
    frame["date"] = date
    frame["ticker"] = "AAPL"
    return frame


def test_train_model_and_artifacts(monkeypatch, tmp_path) -> None:
    monkeypatch.setattr(
        "finance_ia.model.train.build_training_frame",
        lambda _config: _synthetic_training_frame(),
    )

    config = TrainConfig(output_dir=tmp_path, tickers=["AAPL"], start="2021-01-01", end="2022-01-01")
    result = train_model(config)
    save_training_artifacts(result, config)

    assert (tmp_path / MODEL_FILE).exists()
    assert (tmp_path / FEATURE_COLUMNS_FILE).exists()
    assert (tmp_path / METRICS_FILE).exists()
    assert (tmp_path / TRAIN_CONFIG_FILE).exists()

    loaded_model = load_model(tmp_path)
    loaded_columns = load_feature_columns(tmp_path)

    assert loaded_columns == FEATURE_COLUMNS
    sample_x = _synthetic_training_frame(rows=10)[FEATURE_COLUMNS]
    proba = loaded_model.predict_proba(sample_x)[:, 1]
    assert len(proba) == 10
    assert 0.0 <= float(proba.min()) <= 1.0
    assert 0.0 <= float(proba.max()) <= 1.0

    assert "f1" in result.metrics
    assert "precision" in result.metrics
    assert "recall" in result.metrics
