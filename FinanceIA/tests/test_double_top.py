from __future__ import annotations

import pandas as pd

from finance_ia.config import PatternConfig
from finance_ia.features.double_top import build_double_top_labels, detect_double_top_indices


def _series(values: list[float]) -> pd.Series:
    index = pd.date_range("2024-01-01", periods=len(values), freq="D")
    return pd.Series(values, index=index, dtype="float64")


def test_detect_double_top_positive_case() -> None:
    close = _series([100, 104, 108, 112, 118, 120, 116, 110, 119, 121, 117, 113])
    config = PatternConfig(
        peak_window=1,
        min_peak_distance=2,
        max_peak_distance=10,
        peak_tolerance_pct=0.03,
        valley_drop_pct=0.04,
        pretrend_lookback=3,
        min_pretrend_pct=0.05,
    )

    indices = detect_double_top_indices(close, config)
    assert 9 in indices


def test_detect_double_top_negative_case_height_mismatch() -> None:
    close = _series([100, 104, 108, 112, 118, 120, 116, 110, 125, 132, 117, 113])
    config = PatternConfig(
        peak_window=1,
        min_peak_distance=2,
        max_peak_distance=10,
        peak_tolerance_pct=0.02,
        valley_drop_pct=0.04,
        pretrend_lookback=3,
        min_pretrend_pct=0.05,
    )

    indices = detect_double_top_indices(close, config)
    assert indices == []


def test_build_double_top_labels_marks_second_peak() -> None:
    values = [100, 104, 108, 112, 118, 120, 116, 110, 119, 122, 117, 113]
    index = pd.date_range("2024-01-01", periods=len(values), freq="D")
    frame = pd.DataFrame({"Close": values}, index=index)
    config = PatternConfig(
        peak_window=1,
        min_peak_distance=2,
        max_peak_distance=10,
        peak_tolerance_pct=0.03,
        valley_drop_pct=0.04,
        pretrend_lookback=3,
        min_pretrend_pct=0.05,
    )

    labels = build_double_top_labels(frame, config)
    assert labels.sum() == 1
    assert labels.iloc[9] == 1
