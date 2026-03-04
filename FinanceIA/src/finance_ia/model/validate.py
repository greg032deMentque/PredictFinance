from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

import pandas as pd

from finance_ia.config import PatternConfig, pattern_config_from_dict
from finance_ia.data.yahoo import fetch_ohlcv
from finance_ia.features.double_top import build_double_top_labels
from finance_ia.features.indicators import add_indicators
from finance_ia.io.artifacts import load_feature_columns, load_metrics, load_model, load_train_config
from finance_ia.model.evaluate import evaluate_binary_classifier


@dataclass(slots=True)
class EvaluationThresholds:
    min_precision: float = 0.10
    min_recall: float = 0.05
    min_f1: float = 0.08
    min_roc_auc: float = 0.60
    min_rows: int = 200
    min_positive_samples: int = 5


@dataclass(slots=True)
class EvaluationCheck:
    name: str
    value: float
    threshold: float
    passed: bool

    def to_dict(self) -> dict[str, object]:
        return {
            "name": self.name,
            "value": self.value,
            "threshold": self.threshold,
            "passed": self.passed,
        }


@dataclass(slots=True)
class EvaluationVerdict:
    status: str = "NO_GO"
    reason: str = "Model quality checks failed"
    checks: list[EvaluationCheck] = field(default_factory=list)

    def to_dict(self) -> dict[str, object]:
        return {
            "status": self.status,
            "reason": self.reason,
            "checks": [check.to_dict() for check in self.checks],
        }


@dataclass(slots=True)
class ValidationResult:
    ticker: str
    start: str
    end: str
    rows: int
    positive_rate: float
    metrics: dict[str, object]
    threshold_analysis: list[dict[str, object]] = field(default_factory=list)
    best_threshold_by_f1: float = 0.5
    verdict: EvaluationVerdict = field(default_factory=EvaluationVerdict)

    def to_dict(self) -> dict[str, object]:
        return {
            "ticker": self.ticker,
            "start": self.start,
            "end": self.end,
            "rows": self.rows,
            "positive_rate": self.positive_rate,
            "metrics": self.metrics,
            "threshold_analysis": self.threshold_analysis,
            "best_threshold_by_f1": self.best_threshold_by_f1,
            "verdict": self.verdict.to_dict(),
            "field_guide": {
                "rows": "Number of evaluated windows after feature engineering",
                "positive_rate": "Share of true positives in evaluation set",
                "metrics.precision": "Among predicted positives, how many are correct",
                "metrics.recall": "Among true positives, how many are detected",
                "metrics.f1": "Balance between precision and recall",
                "metrics.roc_auc": "Ranking quality independent of threshold",
                "threshold_analysis": "Metrics for each tested threshold",
                "best_threshold_by_f1": "Threshold with highest F1 on current evaluation set",
                "verdict": "Automatic GO/NO_GO decision based on configured minimum thresholds",
            },
        }


def _load_pattern_config(model_dir: Path) -> PatternConfig:
    try:
        config_payload = load_train_config(model_dir)
        pattern_payload = config_payload.get("pattern")
        if isinstance(pattern_payload, dict):
            return pattern_config_from_dict(pattern_payload)
    except FileNotFoundError:
        pass
    return PatternConfig()


def _load_default_threshold(model_dir: Path) -> float:
    try:
        metrics_payload = load_metrics(model_dir)
        threshold = metrics_payload.get("selected_threshold", metrics_payload.get("threshold", 0.5))
        return float(threshold)
    except (FileNotFoundError, ValueError, TypeError):
        return 0.5


def _compute_best_threshold_analysis(
    y_true: pd.Series,
    probabilities,
    threshold_grid: list[float],
) -> tuple[list[dict[str, object]], float]:
    threshold_analysis: list[dict[str, object]] = []
    best_threshold_by_f1 = float(threshold_grid[0])
    best_f1 = -1.0

    for current_threshold in threshold_grid:
        threshold_metrics = evaluate_binary_classifier(
            y_true.to_numpy(),
            probabilities,
            threshold=float(current_threshold),
        )
        current_f1 = float(threshold_metrics["f1"])
        if current_f1 > best_f1:
            best_f1 = current_f1
            best_threshold_by_f1 = float(current_threshold)

        threshold_analysis.append(
            {
                "threshold": float(current_threshold),
                "precision": threshold_metrics["precision"],
                "recall": threshold_metrics["recall"],
                "f1": threshold_metrics["f1"],
                "positive_rate_pred": threshold_metrics["positive_rate_pred"],
                "confusion_matrix": threshold_metrics["confusion_matrix"],
            }
        )

    return threshold_analysis, best_threshold_by_f1


def _extract_positive_sample_count(metrics: dict[str, object]) -> int:
    confusion_matrix = metrics.get("confusion_matrix")
    if not isinstance(confusion_matrix, list) or len(confusion_matrix) != 2:
        return 0
    if not isinstance(confusion_matrix[1], list) or len(confusion_matrix[1]) != 2:
        return 0
    fn = int(confusion_matrix[1][0])
    tp = int(confusion_matrix[1][1])
    return fn + tp


def _build_verdict(metrics: dict[str, object], rows: int, thresholds: EvaluationThresholds) -> EvaluationVerdict:
    precision = float(metrics.get("precision", 0.0))
    recall = float(metrics.get("recall", 0.0))
    f1 = float(metrics.get("f1", 0.0))
    roc_auc_raw = metrics.get("roc_auc")
    roc_auc = float(roc_auc_raw) if isinstance(roc_auc_raw, float | int) else 0.0
    positive_samples = float(_extract_positive_sample_count(metrics))

    checks = [
        EvaluationCheck("rows", float(rows), float(thresholds.min_rows), rows >= thresholds.min_rows),
        EvaluationCheck(
            "positive_samples",
            positive_samples,
            float(thresholds.min_positive_samples),
            positive_samples >= thresholds.min_positive_samples,
        ),
        EvaluationCheck("precision", precision, thresholds.min_precision, precision >= thresholds.min_precision),
        EvaluationCheck("recall", recall, thresholds.min_recall, recall >= thresholds.min_recall),
        EvaluationCheck("f1", f1, thresholds.min_f1, f1 >= thresholds.min_f1),
        EvaluationCheck("roc_auc", roc_auc, thresholds.min_roc_auc, roc_auc >= thresholds.min_roc_auc),
    ]

    if all(check.passed for check in checks):
        return EvaluationVerdict(status="GO", reason="Model quality checks passed", checks=checks)

    failed_checks = [check.name for check in checks if not check.passed]
    return EvaluationVerdict(
        status="NO_GO",
        reason=f"Model quality checks failed: {', '.join(failed_checks)}",
        checks=checks,
    )


def validate_model_on_ticker(
    *,
    model_dir: Path,
    ticker: str,
    start: str,
    end: str,
    interval: str = "1d",
    threshold: float | None = None,
    threshold_grid: list[float] | None = None,
    evaluation_thresholds: EvaluationThresholds | None = None,
) -> ValidationResult:
    model = load_model(model_dir)
    feature_columns = load_feature_columns(model_dir)
    pattern_config = _load_pattern_config(model_dir)
    effective_threshold = _load_default_threshold(model_dir) if threshold is None else float(threshold)

    raw = fetch_ohlcv(ticker=ticker, start=start, end=end, interval=interval)
    enriched = add_indicators(raw)
    labels = build_double_top_labels(enriched, pattern_config)

    frame = enriched.copy()
    frame["target"] = labels
    frame = frame.dropna(subset=feature_columns)
    if frame.empty:
        raise ValueError("No evaluation rows after feature engineering")

    probabilities = model.predict_proba(frame[feature_columns])[:, 1]
    metrics = evaluate_binary_classifier(frame["target"].to_numpy(), probabilities, threshold=effective_threshold)
    metrics["selected_threshold"] = effective_threshold

    grid = threshold_grid if threshold_grid else [0.1, 0.2, 0.3, 0.4, 0.5]
    threshold_analysis, best_threshold_by_f1 = _compute_best_threshold_analysis(frame["target"], probabilities, grid)

    applied_thresholds = evaluation_thresholds if evaluation_thresholds is not None else EvaluationThresholds()
    verdict = _build_verdict(metrics, int(len(frame)), applied_thresholds)

    return ValidationResult(
        ticker=ticker.strip().upper(),
        start=str(pd.to_datetime(start).date()),
        end=str(pd.to_datetime(end).date()),
        rows=int(len(frame)),
        positive_rate=float(frame["target"].mean()),
        metrics=metrics,
        threshold_analysis=threshold_analysis,
        best_threshold_by_f1=best_threshold_by_f1,
        verdict=verdict,
    )
