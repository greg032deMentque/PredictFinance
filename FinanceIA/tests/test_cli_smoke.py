from __future__ import annotations

import json

from finance_ia.cli import predict as predict_cli
from finance_ia.cli import train as train_cli
from finance_ia.model.predict import PredictResult
from finance_ia.model.train import TrainResult


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
            as_of="2025-12-31",
            mean_prob=0.42,
            max_prob=0.81,
            last_prob=0.33,
            n_windows=120,
        ),
    )

    exit_code = predict_cli.main(["--ticker", "AAPL", "--model-dir", str(tmp_path), "--period", "6mo"])
    assert exit_code == 0

    stdout = capsys.readouterr().out
    payload = json.loads(stdout)
    assert payload["ticker"] == "AAPL"
    assert payload["n_windows"] == 120
