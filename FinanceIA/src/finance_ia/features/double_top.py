from __future__ import annotations

from dataclasses import dataclass

import pandas as pd

from finance_ia.config import PatternConfig


@dataclass(slots=True)
class DoubleTopPair:
    first_peak_index: int
    valley_index: int
    second_peak_index: int
    first_peak_price: float
    valley_price: float
    second_peak_price: float
    neckline_price: float
    invalidation_price: float
    target_price: float


@dataclass(slots=True)
class DoubleTopPhaseState:
    phase: str
    current_price: float
    neckline_price: float | None = None
    invalidation_price: float | None = None
    target_price: float | None = None
    first_peak_at: str | None = None
    second_peak_at: str | None = None


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


def _build_pair(close: pd.Series, first_peak: int, second_peak: int, config: PatternConfig) -> DoubleTopPair | None:
    if not _is_double_top_pair(close, first_peak, second_peak, config):
        return None

    window = close.iloc[first_peak : second_peak + 1]
    valley_index = int(window.argmin()) + first_peak
    first_price = float(close.iloc[first_peak])
    second_price = float(close.iloc[second_peak])
    valley_price = float(close.iloc[valley_index])
    top_reference = max(first_price, second_price)

    neckline_price = valley_price
    invalidation_price = top_reference * (1.0 + config.peak_tolerance_pct)
    target_price = neckline_price - (top_reference - neckline_price)

    return DoubleTopPair(
        first_peak_index=first_peak,
        valley_index=valley_index,
        second_peak_index=second_peak,
        first_peak_price=first_price,
        valley_price=valley_price,
        second_peak_price=second_price,
        neckline_price=neckline_price,
        invalidation_price=invalidation_price,
        target_price=target_price,
    )


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


def detect_double_top_pairs(close: pd.Series, config: PatternConfig) -> list[DoubleTopPair]:
    peaks = find_local_peak_indices(close, window=config.peak_window)
    detected: list[DoubleTopPair] = []

    for left_index, first_peak in enumerate(peaks):
        for second_peak in peaks[left_index + 1 :]:
            if (second_peak - first_peak) > config.max_peak_distance:
                break

            pair = _build_pair(close, first_peak, second_peak, config)
            if pair is not None:
                detected.append(pair)

    return detected


def analyze_double_top_phase(close: pd.Series, config: PatternConfig) -> DoubleTopPhaseState:
    if close.empty:
        raise ValueError("Close series must not be empty")

    current_price = float(close.iloc[-1])
    peaks = find_local_peak_indices(close, window=config.peak_window)
    pairs = detect_double_top_pairs(close, config=config)

    if pairs:
        latest_pair = max(pairs, key=lambda pair: pair.second_peak_index)
        first_peak_at = close.index[latest_pair.first_peak_index].date().isoformat()
        second_peak_at = close.index[latest_pair.second_peak_index].date().isoformat()

        if current_price > latest_pair.invalidation_price:
            phase = "invalidated"
        elif current_price < latest_pair.neckline_price:
            if latest_pair.second_peak_index < (len(close) - 1):
                pullback_gap = abs(current_price - latest_pair.neckline_price) / max(latest_pair.neckline_price, 1e-6)
                phase = "pullback_after_break" if pullback_gap <= 0.02 else "neckline_break_confirmed"
            else:
                phase = "neckline_break_confirmed"
        else:
            phase = "second_peak_candidate"

        return DoubleTopPhaseState(
            phase=phase,
            current_price=current_price,
            neckline_price=latest_pair.neckline_price,
            invalidation_price=latest_pair.invalidation_price,
            target_price=latest_pair.target_price,
            first_peak_at=first_peak_at,
            second_peak_at=second_peak_at,
        )

    if peaks:
        latest_peak = peaks[-1]
        latest_peak_price = float(close.iloc[latest_peak])
        valley_since_peak = float(close.iloc[latest_peak:].min())
        valley_drop = (latest_peak_price - valley_since_peak) / max(latest_peak_price, 1e-6)
        phase = "valley_confirmed" if valley_drop >= config.valley_drop_pct else "candidate_first_peak"

        return DoubleTopPhaseState(
            phase=phase,
            current_price=current_price,
            invalidation_price=latest_peak_price * (1.0 + config.peak_tolerance_pct),
            first_peak_at=close.index[latest_peak].date().isoformat(),
        )

    return DoubleTopPhaseState(phase="candidate_first_peak", current_price=current_price)


def build_double_top_labels(frame: pd.DataFrame, config: PatternConfig) -> pd.Series:
    """Build binary labels where 1 marks the second peak of a double top."""
    close = frame["Close"]
    labels = pd.Series(0, index=frame.index, dtype="int64")

    for position in detect_double_top_indices(close, config=config):
        labels.iloc[position] = 1

    return labels
