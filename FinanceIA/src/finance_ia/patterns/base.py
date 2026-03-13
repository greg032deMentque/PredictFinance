from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

import pandas as pd


@dataclass(slots=True)
class PatternAssessment:
    pattern: str
    phase: str
    probability: float
    confidence: float
    current_price: float
    neckline_price: float | None = None
    target_price: float | None = None
    invalidation_price: float | None = None
    first_peak_at: str | None = None
    second_peak_at: str | None = None
    is_primary: bool = True

    def to_dict(self) -> dict[str, object]:
        return {
            "pattern": self.pattern,
            "phase": self.phase,
            "probability": self.probability,
            "confidence": self.confidence,
            "current_price": self.current_price,
            "neckline_price": self.neckline_price,
            "target_price": self.target_price,
            "invalidation_price": self.invalidation_price,
            "first_peak_at": self.first_peak_at,
            "second_peak_at": self.second_peak_at,
            "is_primary": self.is_primary,
        }


@dataclass(slots=True)
class DecisionSignal:
    action: str
    actionable: bool
    confidence: float
    reason: str
    horizon_days: int

    def to_dict(self) -> dict[str, object]:
        return {
            "action": self.action,
            "actionable": self.actionable,
            "confidence": self.confidence,
            "reason": self.reason,
            "horizon_days": self.horizon_days,
        }


class RuntimePatternAnalyzer(Protocol):
    pattern_name: str

    def build_assessment(self, frame: pd.DataFrame, last_probability: float) -> PatternAssessment:
        ...

    def build_decision(self, assessment: PatternAssessment) -> DecisionSignal:
        ...
