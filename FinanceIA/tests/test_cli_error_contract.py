from __future__ import annotations

import json

from finance_ia.cli import predict as predict_cli
from finance_ia.cli import simulate as simulate_cli


def test_predict_cli_returns_json_error_when_model_is_missing(monkeypatch, capsys, tmp_path) -> None:
    monkeypatch.setenv("FINANCE_IA_LOG_DIR", str(tmp_path / "logs"))

    exit_code = predict_cli.main(
        [
            "--ticker",
            "AAPL",
            "--model-dir",
            str(tmp_path / "missing-model"),
            "--period",
            "6mo",
            "--pattern",
            "DOUBLE_TOP",
        ]
    )

    assert exit_code == 1
    captured = capsys.readouterr()
    assert captured.out == ""

    payload = json.loads(captured.err)
    assert payload["schema_version"] == "1.0"
    assert payload["source"] == "finance_ia.cli"
    assert payload["operation"] == "predict"
    assert payload["error_code"] == "artifact_missing"
    assert payload["error_type"] == "FileNotFoundError"
    assert payload["ticker"] == "AAPL"
    assert payload["pattern"] == "DOUBLE_TOP"
    assert payload["user_message"] == "Le modele IA est indisponible pour le moment."


def test_simulate_cli_returns_json_error_for_invalid_input(monkeypatch, capsys, tmp_path) -> None:
    monkeypatch.setenv("FINANCE_IA_LOG_DIR", str(tmp_path / "logs"))

    exit_code = simulate_cli.main(
        [
            "--ticker",
            "AAPL",
            "--model-dir",
            str(tmp_path / "unused"),
            "--period",
            "6mo",
            "--pattern",
            "DOUBLE_TOP",
            "--investment-amount",
            "0",
            "--horizon-days",
            "30",
        ]
    )

    assert exit_code == 1
    captured = capsys.readouterr()
    assert captured.out == ""

    payload = json.loads(captured.err)
    assert payload["schema_version"] == "1.0"
    assert payload["source"] == "finance_ia.cli"
    assert payload["operation"] == "simulate"
    assert payload["error_code"] == "invalid_input"
    assert payload["error_type"] == "ValueError"
    assert payload["ticker"] == "AAPL"
    assert payload["pattern"] == "DOUBLE_TOP"
    assert payload["user_message"] == "La requete envoyee au moteur IA est invalide."
