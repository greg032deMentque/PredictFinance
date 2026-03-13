from __future__ import annotations

from finance_ia.config import PatternConfig
from finance_ia.features.double_top import analyze_double_top_phase
from finance_ia.patterns.base import DecisionSignal, PatternAssessment


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

    def build_decision(self, assessment: PatternAssessment) -> DecisionSignal:
        if assessment.phase in {"neckline_break_confirmed", "pullback_after_break"} and assessment.probability >= 0.55:
            return DecisionSignal(
                action="sell",
                actionable=True,
                confidence=max(0.55, assessment.probability),
                reason="Double Top bearish structure confirmed after neckline break.",
                horizon_days=20,
            )

        if assessment.phase == "invalidated":
            return DecisionSignal(
                action="hold",
                actionable=False,
                confidence=max(0.35, 1.0 - assessment.probability),
                reason="The bearish Double Top structure has been invalidated.",
                horizon_days=10,
            )

        if assessment.phase == "second_peak_candidate":
            return DecisionSignal(
                action="hold",
                actionable=False,
                confidence=min(0.9, assessment.probability),
                reason="Second peak candidate detected, but neckline break is still missing.",
                horizon_days=10,
            )

        return DecisionSignal(
            action="hold",
            actionable=False,
            confidence=min(0.75, max(0.15, assessment.probability)),
            reason="Pattern context is not strong enough yet for an actionable signal.",
            horizon_days=10,
        )
