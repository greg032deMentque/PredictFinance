from __future__ import annotations

import pandas as pd

from finance_ia.config import PatternConfig
from finance_ia.features.double_top import (
    analyze_double_top_phase,
    build_double_top_labels,
    detect_double_top_indices,
)


def _series(values: list[float]) -> pd.Series:
    index = pd.date_range("2024-01-01", periods=len(values), freq="D")
    return pd.Series(values, index=index, dtype="float64")


def _config() -> PatternConfig:
    return PatternConfig(
        peak_window=1,
        min_peak_distance=2,
        max_peak_distance=10,
        peak_tolerance_pct=0.03,
        valley_drop_pct=0.04,
        pretrend_lookback=3,
        min_pretrend_pct=0.05,
    )


def test_detect_double_top_positive_case() -> None:
    close = _series([100, 104, 108, 112, 118, 120, 116, 110, 119, 121, 117, 113])
    indices = detect_double_top_indices(close, _config())
    assert 9 in indices


def test_detect_double_top_negative_case_height_mismatch() -> None:
    close = _series([100, 104, 108, 112, 118, 120, 116, 110, 125, 132, 117, 113])
    indices = detect_double_top_indices(close, _config())
    assert indices == []


def test_build_double_top_labels_marks_second_peak() -> None:
    values = [100, 104, 108, 112, 118, 120, 116, 110, 119, 122, 117, 113]
    index = pd.date_range("2024-01-01", periods=len(values), freq="D")
    frame = pd.DataFrame({"Close": values}, index=index)

    labels = build_double_top_labels(frame, _config())
    assert labels.sum() == 1
    assert labels.iloc[9] == 1


def test_analyze_double_top_phase_reports_neckline_break() -> None:
    close = _series([100, 104, 108, 112, 118, 120, 116, 110, 119, 121, 117, 100])
    result = analyze_double_top_phase(close, _config())

    assert result.phase == "neckline_break_confirmed"
    assert result.neckline_price is not None
    assert result.target_price is not None


def test_analyze_double_top_phase_reports_invalidation() -> None:
    close = _series([100, 104, 108, 112, 118, 120, 116, 110, 119, 121, 117, 125])
    result = analyze_double_top_phase(close, _config())

    assert result.phase == "invalidated"
    assert result.invalidation_price is not None
