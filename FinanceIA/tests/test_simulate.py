from __future__ import annotations

from pathlib import Path

from finance_ia.model.simulate import simulate_ticker


def test_simulate_ticker_basic(monkeypatch) -> None:
    class FakePrediction:
        ticker = "AAPL"
        as_of = "2026-03-04"
        mean_prob = 0.20
        max_prob = 0.40
        last_prob = 0.10
        n_windows = 80

    monkeypatch.setattr("finance_ia.model.simulate.predict_ticker", lambda **_kwargs: FakePrediction())

    result = simulate_ticker(
        ticker="AAPL",
        model_dir=Path("artifacts/double_top"),
        investment_amount=1000.0,
        horizon_days=30,
    )

    assert result.ticker == "AAPL"
    assert result.pattern == "DOUBLE_TOP"
    assert result.recommendation == "buy"
    assert result.estimated_final_amount >= 1000.0
