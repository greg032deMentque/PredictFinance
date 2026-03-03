from __future__ import annotations

from finance_ia.config import FEATURE_COLUMNS
from finance_ia.features.indicators import add_indicators


def test_add_indicators_contains_expected_columns(sample_ohlcv) -> None:
    frame = add_indicators(sample_ohlcv)
    for column in FEATURE_COLUMNS:
        assert column in frame.columns


def test_add_indicators_no_nan_after_warmup(sample_ohlcv) -> None:
    frame = add_indicators(sample_ohlcv)
    warmup_free = frame.iloc[40:]
    assert warmup_free[FEATURE_COLUMNS].isna().sum().sum() == 0
