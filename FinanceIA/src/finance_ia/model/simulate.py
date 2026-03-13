from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from finance_ia.model.predict import predict_ticker


@dataclass(slots=True)
class SimulationResult:
    ticker: str
    pattern: str
    phase: str
    as_of: str
    investment_amount: float
    horizon_days: int
    estimated_return_pct: float
    estimated_return_amount: float
    estimated_final_amount: float
    recommendation: str
    confidence: float
    assumption: str
    last_prob: float
    mean_prob: float
    max_prob: float
    n_windows: int
    current_price: float
    target_price: float | None
    invalidation_price: float | None
    actionable: bool

    def to_dict(self) -> dict[str, object]:
        return {
            "schema_version": "2.0",
            "ticker": self.ticker,
            "pattern": self.pattern,
            "phase": self.phase,
            "as_of": self.as_of,
            "investment_amount": self.investment_amount,
            "horizon_days": self.horizon_days,
            "estimated_return_pct": self.estimated_return_pct,
            "estimated_return_amount": self.estimated_return_amount,
            "estimated_final_amount": self.estimated_final_amount,
            "recommendation": self.recommendation,
            "confidence": self.confidence,
            "assumption": self.assumption,
            "last_prob": self.last_prob,
            "mean_prob": self.mean_prob,
            "max_prob": self.max_prob,
            "n_windows": self.n_windows,
            "current_price": self.current_price,
            "target_price": self.target_price,
            "invalidation_price": self.invalidation_price,
            "actionable": self.actionable,
        }


def _normalize_pattern(pattern: str) -> str:
    normalized = (pattern or "").strip().upper()
    if normalized in {"DOUBLE_TOP", "DOUBLE TOP"}:
        return "DOUBLE_TOP"
    raise ValueError("Only DOUBLE_TOP pattern is supported")


def _estimate_return_pct(
    *,
    current_price: float,
    target_price: float | None,
    recommendation: str,
    actionable: bool,
) -> float:
    if not actionable or recommendation != "sell" or current_price <= 0 or target_price is None:
        return 0.0

    return float((target_price / current_price) - 1.0)


def simulate_ticker(
    *,
    ticker: str,
    model_dir: Path,
    period: str = "6mo",
    pattern: str = "DOUBLE_TOP",
    investment_amount: float,
    horizon_days: int,
    sell_threshold: float = 0.65,
    buy_threshold: float = 0.20,
) -> SimulationResult:
    del sell_threshold, buy_threshold

    if investment_amount <= 0:
        raise ValueError("investment_amount must be strictly positive")

    safe_horizon_days = max(1, min(int(horizon_days), 365))
    safe_pattern = _normalize_pattern(pattern)

    prediction = predict_ticker(ticker=ticker, model_dir=model_dir, period=period, pattern=safe_pattern)
    assessment = prediction.assessments[0]
    estimated_return_pct = _estimate_return_pct(
        current_price=assessment.current_price,
        target_price=assessment.target_price,
        recommendation=prediction.decision_signal.action,
        actionable=prediction.decision_signal.actionable,
    )
    estimated_return_amount = float(investment_amount) * estimated_return_pct
    estimated_final_amount = float(investment_amount) + estimated_return_amount

    return SimulationResult(
        ticker=prediction.ticker,
        pattern=safe_pattern,
        phase=prediction.phase,
        as_of=prediction.as_of,
        investment_amount=float(round(investment_amount, 2)),
        horizon_days=safe_horizon_days,
        estimated_return_pct=float(estimated_return_pct),
        estimated_return_amount=float(round(estimated_return_amount, 2)),
        estimated_final_amount=float(round(estimated_final_amount, 2)),
        recommendation=prediction.decision_signal.action,
        confidence=float(prediction.decision_signal.confidence),
        assumption="Simulation uses detected pattern structure, target price and invalidation level.",
        last_prob=float(prediction.last_prob),
        mean_prob=float(prediction.mean_prob),
        max_prob=float(prediction.max_prob),
        n_windows=int(prediction.n_windows),
        current_price=float(assessment.current_price),
        target_price=assessment.target_price,
        invalidation_price=assessment.invalidation_price,
        actionable=prediction.decision_signal.actionable,
    )
