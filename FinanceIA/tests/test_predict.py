from __future__ import annotations

import numpy as np
import pandas as pd
from pathlib import Path

from finance_ia.config import FEATURE_COLUMNS
from finance_ia.model.predict import PredictResult, predict_ticker


class DummyModel:
    def predict_proba(self, x_frame: pd.DataFrame) -> np.ndarray:
        score = x_frame.iloc[:, 0].rank(pct=True).to_numpy()
        return np.column_stack([1.0 - score, score])


def test_predict_ticker_returns_json_contract(monkeypatch, sample_ohlcv) -> None:
    columns = FEATURE_COLUMNS[:4]

    monkeypatch.setattr("finance_ia.model.predict.load_model", lambda _model_dir: DummyModel())
    monkeypatch.setattr("finance_ia.model.predict.load_feature_columns", lambda _model_dir: columns)
    monkeypatch.setattr("finance_ia.model.predict.fetch_ohlcv", lambda **_kwargs: sample_ohlcv)

    result = predict_ticker(ticker="AAPL", model_dir=Path("unused"), period="6mo")
    assert isinstance(result, PredictResult)

    payload = result.to_dict()
    assert payload["ticker"] == "AAPL"
    assert payload["n_windows"] > 0
    assert 0.0 <= payload["mean_prob"] <= 1.0
    assert 0.0 <= payload["max_prob"] <= 1.0
    assert 0.0 <= payload["last_prob"] <= 1.0
