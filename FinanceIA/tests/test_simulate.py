from __future__ import annotations

from pathlib import Path

from finance_ia.model.simulate import simulate_ticker


def test_simulate_ticker_uses_pattern_target(monkeypatch) -> None:
    class FakeAssessment:
        pattern = "DOUBLE_TOP"
        phase = "neckline_break_confirmed"
        probability = 0.72
        confidence = 0.72
        current_price = 100.0
        neckline_price = 95.0
        target_price = 88.0
        invalidation_price = 103.0

    class FakeDecision:
        action = "sell"
        actionable = True
        confidence = 0.8
        reason = "Break confirmed"
        horizon_days = 20

    class FakePrediction:
        ticker = "AAPL"
        pattern = "DOUBLE_TOP"
        phase = "neckline_break_confirmed"
        as_of = "2026-03-04"
        current_price = 100.0
        mean_prob = 0.52
        max_prob = 0.81
        last_prob = 0.72
        n_windows = 80
        assessments = [FakeAssessment()]
        decision_signal = FakeDecision()

    monkeypatch.setattr("finance_ia.model.simulate.predict_ticker", lambda **_kwargs: FakePrediction())

    result = simulate_ticker(
        ticker="AAPL",
        model_dir=Path("artifacts/double_top"),
        investment_amount=1000.0,
        horizon_days=30,
    )

    assert result.ticker == "AAPL"
    assert result.pattern == "DOUBLE_TOP"
    assert result.phase == "neckline_break_confirmed"
    assert result.recommendation == "sell"
    assert result.estimated_return_pct == -0.12
    assert result.estimated_final_amount == 880.0
