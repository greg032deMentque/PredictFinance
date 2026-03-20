from __future__ import annotations

from finance_ia.config import PatternConfig
from finance_ia.features.double_top import analyze_double_top_phase
from finance_ia.patterns.base import PatternAssessment


class DoubleTopRuntimeAnalyzer:
    pattern_name = "DOUBLE_TOP"

    def __init__(self, config: PatternConfig | None = None) -> None:
        self._config = config or PatternConfig()

    def build_assessment(self, frame, last_probability: float) -> PatternAssessment:
        state = analyze_double_top_phase(frame["Close"], config=self._config)
        confidence = max(0.0, min(1.0, float(last_probability)))

        return PatternAssessment(
            pattern=self.pattern_name,
            phase=state.phase,
            probability=confidence,
            confidence=confidence,
            current_price=state.current_price,
            neckline_price=state.neckline_price,
            target_price=state.target_price,
            invalidation_price=state.invalidation_price,
            first_peak_at=state.first_peak_at,
            second_peak_at=state.second_peak_at,
            is_primary=True,
        )
