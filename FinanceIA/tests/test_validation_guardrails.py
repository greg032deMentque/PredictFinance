from __future__ import annotations

from finance_ia.model.validate import EvaluationThresholds, _build_verdict


def test_quality_gate_rejects_unrealistic_precision() -> None:
    metrics = {
        "precision": 0.07,
        "recall": 0.40,
        "f1": 0.11,
        "roc_auc": 0.77,
        "confusion_matrix": [[90, 30], [4, 6]],
    }

    verdict = _build_verdict(metrics, rows=130, thresholds=EvaluationThresholds())

    assert verdict.status == "NO_GO"
    assert "precision" in verdict.reason


def test_quality_gate_accepts_minimum_realism_bar() -> None:
    metrics = {
        "precision": 0.32,
        "recall": 0.28,
        "f1": 0.30,
        "roc_auc": 0.71,
        "confusion_matrix": [[140, 20], [18, 10]],
    }

    thresholds = EvaluationThresholds(
        min_precision=0.20,
        min_recall=0.20,
        min_f1=0.20,
        min_roc_auc=0.65,
        min_rows=100,
        min_positive_samples=5,
    )
    verdict = _build_verdict(metrics, rows=188, thresholds=thresholds)

    assert verdict.status == "GO"
