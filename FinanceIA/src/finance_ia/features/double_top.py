from __future__ import annotations

import pandas as pd

from finance_ia.config import PatternConfig


def find_local_peak_indices(close: pd.Series, window: int = 3) -> list[int]:
    """Return local peak indices detected with a symmetric window."""
    values = close.to_numpy()
    peaks: list[int] = []

    if len(values) < (2 * window + 1):
        return peaks

    for index in range(window, len(values) - window):
        neighborhood = values[index - window : index + window + 1]
        center = values[index]
        if center == neighborhood.max() and (neighborhood == center).sum() == 1:
            peaks.append(index)

    return peaks


def _has_pre_uptrend(close: pd.Series, peak_index: int, config: PatternConfig) -> bool:
    start_index = peak_index - config.pretrend_lookback
    if start_index < 0:
        return False

    start_price = float(close.iloc[start_index])
    peak_price = float(close.iloc[peak_index])
    if start_price <= 0:
        return False

    move = (peak_price / start_price) - 1.0
    return move >= config.min_pretrend_pct


def _is_double_top_pair(close: pd.Series, first_peak: int, second_peak: int, config: PatternConfig) -> bool:
    distance = second_peak - first_peak
    if distance < config.min_peak_distance or distance > config.max_peak_distance:
        return False

    first_price = float(close.iloc[first_peak])
    second_price = float(close.iloc[second_peak])
    top_reference = max(first_price, second_price)
    if top_reference <= 0:
        return False

    diff_ratio = abs(first_price - second_price) / top_reference
    if diff_ratio > config.peak_tolerance_pct:
        return False

    valley_price = float(close.iloc[first_peak : second_peak + 1].min())
    valley_drop = (top_reference - valley_price) / top_reference
    if valley_drop < config.valley_drop_pct:
        return False

    if not _has_pre_uptrend(close, first_peak, config):
        return False

    return True


def detect_double_top_indices(close: pd.Series, config: PatternConfig) -> list[int]:
    """Return second-peak indices for all detected double top patterns."""
    peaks = find_local_peak_indices(close, window=config.peak_window)
    detected: set[int] = set()

    for left_index, first_peak in enumerate(peaks):
        for second_peak in peaks[left_index + 1 :]:
            if (second_peak - first_peak) > config.max_peak_distance:
                break
            if _is_double_top_pair(close, first_peak, second_peak, config):
                detected.add(second_peak)

    return sorted(detected)


def build_double_top_labels(frame: pd.DataFrame, config: PatternConfig) -> pd.Series:
    """Build binary labels where 1 marks the second peak of a double top."""
    close = frame["Close"]
    labels = pd.Series(0, index=frame.index, dtype="int64")

    for position in detect_double_top_indices(close, config=config):
        labels.iloc[position] = 1

    return labels
