from __future__ import annotations

import json
from pathlib import Path

from finance_ia.cli import evaluate as evaluate_cli
from finance_ia.cli import predict as predict_cli
from finance_ia.cli import simulate as simulate_cli
from finance_ia.cli import train as train_cli
from finance_ia.model.predict import PredictResult
from finance_ia.model.simulate import SimulationResult
from finance_ia.model.train import TrainResult
from finance_ia.model.validate import ValidationResult
from finance_ia.patterns.base import PatternAssessment


def test_train_cli_smoke(monkeypatch, capsys, tmp_path) -> None:
    result = TrainResult(
        model=object(),  # type: ignore[arg-type]
        feature_columns=["ret_1d"],
        metrics={"f1": 0.7},
        train_rows=100,
        test_rows=20,
        train_positive_ratio=0.3,
        test_positive_ratio=0.25,
    )
    state: dict[str, bool] = {"saved": False}

    monkeypatch.setattr(train_cli, "train_model", lambda _config: result)
    monkeypatch.setattr(
        train_cli,
        "save_training_artifacts",
        lambda _result, _config: state.update(saved=True),
    )

    exit_code = train_cli.main(
        [
            "--output-dir",
            str(tmp_path),
            "--tickers",
            "AAPL",
            "MSFT",
            "--start",
            "2020-01-01",
            "--end",
            "2021-01-01",
        ]
    )

    assert exit_code == 0
    assert state["saved"]
    stdout = capsys.readouterr().out
    payload = json.loads(stdout)
    assert payload["output_dir"] == str(tmp_path)


def test_predict_cli_smoke(monkeypatch, capsys, tmp_path) -> None:
    monkeypatch.setattr(
        predict_cli,
        "predict_ticker",
        lambda **_kwargs: PredictResult(
            ticker="AAPL",
            pattern="DOUBLE_TOP",
            phase="second_peak_candidate",
            as_of="2025-12-31",
            current_price=123.4,
            mean_prob=0.42,
            max_prob=0.81,
            last_prob=0.33,
            n_windows=120,
            assessments=[
                PatternAssessment(
                    pattern="DOUBLE_TOP",
                    phase="second_peak_candidate",
                    probability=0.33,
                    confidence=0.33,
                    current_price=123.4,
                )
            ],
        ),
    )

    exit_code = predict_cli.main(["--ticker", "AAPL", "--model-dir", str(tmp_path), "--period", "6mo"])
    assert exit_code == 0

    stdout = capsys.readouterr().out
    payload = json.loads(stdout)
    assert payload["ticker"] == "AAPL"
    assert payload["n_windows"] == 120
    assert payload["phase"] == "second_peak_candidate"


def test_evaluate_cli_smoke(monkeypatch, capsys, tmp_path) -> None:
    monkeypatch.setattr(
        evaluate_cli,
        "validate_model_on_ticker",
        lambda **_kwargs: ValidationResult(
            ticker="AAPL",
            start="2025-01-01",
            end="2025-12-31",
            rows=100,
            positive_rate=0.12,
            metrics={"f1": 0.61},
            threshold_analysis=[
                {
                    "threshold": 0.2,
                    "precision": 0.4,
                    "recall": 0.8,
                    "f1": 0.53,
                    "positive_rate_pred": 0.1,
                    "confusion_matrix": [[80, 10], [2, 8]],
                }
            ],
            best_threshold_by_f1=0.2,
        ),
    )

    exit_code = evaluate_cli.main(
        [
            "--ticker",
            "AAPL",
            "--model-dir",
            str(tmp_path),
            "--start",
            "2025-01-01",
            "--end",
            "2025-12-31",
        ]
    )
    assert exit_code == 0

    stdout = capsys.readouterr().out
    payload = json.loads(stdout)
    assert payload["ticker"] == "AAPL"
    assert payload["rows"] == 100
    assert payload["best_threshold_by_f1"] == 0.2
    report_file = Path(payload["report_file"])
    assert report_file.name == "evaluation_AAPL.json"
    assert report_file.parent.name.startswith("evaluation_")
    assert report_file.exists()


def test_simulate_cli_smoke(monkeypatch, capsys, tmp_path) -> None:
    monkeypatch.setattr(
        simulate_cli,
        "simulate_ticker",
        lambda **_kwargs: SimulationResult(
            ticker="AAPL",
            pattern="DOUBLE_TOP",
            phase="neckline_break_confirmed",
            as_of="2025-12-31",
            investment_amount=1000.0,
            horizon_days=30,
            estimated_return_pct=-0.12,
            estimated_return_amount=-120.0,
            estimated_final_amount=880.0,
            assumption="Simulation projects a neutral scenario if the detected pattern continues toward its target.",
            last_prob=0.1,
            mean_prob=0.2,
            max_prob=0.3,
            n_windows=100,
            current_price=100.0,
            target_price=88.0,
            invalidation_price=103.0,
        ),
    )

    exit_code = simulate_cli.main(
        [
            "--ticker",
            "AAPL",
            "--model-dir",
            str(tmp_path),
            "--investment-amount",
            "1000",
            "--horizon-days",
            "30",
        ]
    )
    assert exit_code == 0

    stdout = capsys.readouterr().out
    payload = json.loads(stdout)
    assert payload["ticker"] == "AAPL"
    assert "recommendation" not in payload
    assert payload["estimated_final_amount"] == 880.0
