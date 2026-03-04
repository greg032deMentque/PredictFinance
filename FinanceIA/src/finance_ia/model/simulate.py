from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from finance_ia.model.predict import predict_ticker


@dataclass(slots=True)
class SimulationResult:
    ticker: str
    pattern: str
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

    def to_dict(self) -> dict[str, object]:
        return {
            "schema_version": "1.0",
            "ticker": self.ticker,
            "pattern": self.pattern,
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
        }


def _clamp_probability(value: float) -> float:
    if value < 0.0:
        return 0.0
    if value > 1.0:
        return 1.0
    return value


def _normalize_pattern(pattern: str) -> str:
    normalized = (pattern or "").strip().upper()
    if normalized in {"DOUBLE_TOP", "DOUBLE TOP"}:
        return "DOUBLE_TOP"
    raise ValueError("Only DOUBLE_TOP pattern is supported")


def _build_action(last_prob: float, sell_threshold: float, buy_threshold: float) -> tuple[str, float, float]:
    safe_sell = _clamp_probability(sell_threshold)
    safe_buy = _clamp_probability(buy_threshold)
    if safe_buy > safe_sell:
        safe_buy, safe_sell = safe_sell, safe_buy

    if last_prob >= safe_sell:
        return "sell", _clamp_probability(last_prob), -0.08

    if last_prob <= safe_buy:
        return "buy", _clamp_probability(1.0 - last_prob), 0.12

    confidence = _clamp_probability(1.0 - abs(last_prob - 0.5) * 2.0)
    return "hold", confidence, 0.02


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
    if investment_amount <= 0:
        raise ValueError("investment_amount must be strictly positive")

    safe_horizon_days = max(1, min(int(horizon_days), 365))
    safe_pattern = _normalize_pattern(pattern)

    prediction = predict_ticker(ticker=ticker, model_dir=model_dir, period=period)
    recommendation, confidence, trend_factor = _build_action(
        last_prob=prediction.last_prob,
        sell_threshold=sell_threshold,
        buy_threshold=buy_threshold,
    )

    horizon_factor = safe_horizon_days / 30.0
    estimated_return_pct = trend_factor * confidence * horizon_factor
    estimated_return_amount = float(investment_amount) * estimated_return_pct
    estimated_final_amount = float(investment_amount) + estimated_return_amount

    return SimulationResult(
        ticker=prediction.ticker,
        pattern=safe_pattern,
        as_of=prediction.as_of,
        investment_amount=float(round(investment_amount, 2)),
        horizon_days=safe_horizon_days,
        estimated_return_pct=float(estimated_return_pct),
        estimated_return_amount=float(round(estimated_return_amount, 2)),
        estimated_final_amount=float(round(estimated_final_amount, 2)),
        recommendation=recommendation,
        confidence=float(confidence),
        assumption="Simulation based on IA confidence and simplified market profile.",
        last_prob=float(prediction.last_prob),
        mean_prob=float(prediction.mean_prob),
        max_prob=float(prediction.max_prob),
        n_windows=int(prediction.n_windows),
    )
