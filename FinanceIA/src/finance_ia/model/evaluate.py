from __future__ import annotations

import numpy as np
from sklearn.metrics import confusion_matrix, f1_score, precision_score, recall_score, roc_auc_score


def evaluate_binary_classifier(
    y_true: np.ndarray,
    y_proba: np.ndarray,
    *,
    threshold: float = 0.5,
) -> dict[str, object]:
    """Compute classification metrics for binary probabilities."""
    y_pred = (y_proba >= threshold).astype(int)

    roc_auc: float | None
    unique_targets = np.unique(y_true)
    if len(unique_targets) < 2:
        roc_auc = None
    else:
        roc_auc = float(roc_auc_score(y_true, y_proba))

    metrics: dict[str, object] = {
        "threshold": threshold,
        "roc_auc": roc_auc,
        "precision": float(precision_score(y_true, y_pred, zero_division=0)),
        "recall": float(recall_score(y_true, y_pred, zero_division=0)),
        "f1": float(f1_score(y_true, y_pred, zero_division=0)),
        "confusion_matrix": confusion_matrix(y_true, y_pred).tolist(),
        "positive_rate_true": float(np.mean(y_true)),
        "positive_rate_pred": float(np.mean(y_pred)),
    }
    return metrics


def find_best_threshold_for_f1(
    y_true: np.ndarray,
    y_proba: np.ndarray,
    *,
    min_threshold: float = 0.1,
    max_threshold: float = 0.9,
    step: float = 0.05,
) -> float:
    """Search the threshold that maximizes F1 score on validation data."""
    best_threshold = 0.5
    best_f1 = -1.0
    threshold = min_threshold

    while threshold <= (max_threshold + 1e-9):
        metrics = evaluate_binary_classifier(y_true, y_proba, threshold=float(threshold))
        current_f1 = float(metrics["f1"])
        if current_f1 > best_f1:
            best_f1 = current_f1
            best_threshold = float(threshold)
        threshold += step

    return best_threshold
