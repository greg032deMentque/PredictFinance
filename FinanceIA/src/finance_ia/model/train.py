from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import pandas as pd
from lightgbm import LGBMClassifier

from finance_ia.config import FEATURE_COLUMNS, TrainConfig
from finance_ia.dataset.build_dataset import build_training_frame, temporal_train_val_test_split
from finance_ia.model.evaluate import evaluate_binary_classifier, find_best_threshold_for_f1


@dataclass(slots=True)
class TrainResult:
    model: LGBMClassifier
    feature_columns: list[str]
    metrics: dict[str, object]
    train_rows: int
    test_rows: int
    train_positive_ratio: float
    test_positive_ratio: float
    val_rows: int = 0
    val_positive_ratio: float = 0.0


def _prepare_xy(frame: pd.DataFrame) -> tuple[pd.DataFrame, pd.Series]:
    x_frame = frame[FEATURE_COLUMNS]
    y_series = frame["target"].astype("int64")
    return x_frame, y_series


def train_model(config: TrainConfig) -> TrainResult:
    """Train a single LightGBM binary classifier with temporal train/val/test split."""
    dataset = build_training_frame(config)
    train_frame, val_frame, test_frame = temporal_train_val_test_split(
        dataset,
        val_size=config.val_size,
        test_size=config.test_size,
    )

    x_train, y_train = _prepare_xy(train_frame)
    x_val, y_val = _prepare_xy(val_frame)
    x_test, y_test = _prepare_xy(test_frame)

    if y_train.nunique() < 2:
        raise ValueError("Training target has a single class. Expand the date range or ticker set.")

    params: dict[str, Any] = dict(config.model_params)
    params.setdefault("class_weight", "balanced")
    params.setdefault("random_state", config.random_state)

    model = LGBMClassifier(**params)
    model.fit(x_train, y_train)

    val_proba = model.predict_proba(x_val)[:, 1]
    if y_val.nunique() < 2:
        selected_threshold = float(config.classification_threshold)
    else:
        selected_threshold = find_best_threshold_for_f1(y_val.to_numpy(), val_proba)
    val_metrics = evaluate_binary_classifier(
        y_true=y_val.to_numpy(),
        y_proba=val_proba,
        threshold=selected_threshold,
    )

    test_proba = model.predict_proba(x_test)[:, 1]
    test_metrics = evaluate_binary_classifier(
        y_true=y_test.to_numpy(),
        y_proba=test_proba,
        threshold=selected_threshold,
    )

    metrics: dict[str, object] = dict(test_metrics)
    metrics["selected_threshold"] = selected_threshold
    metrics["validation"] = val_metrics
    metrics["test"] = test_metrics
    metrics["periods"] = {
        "train_start": str(train_frame["date"].min().date()),
        "train_end": str(train_frame["date"].max().date()),
        "val_start": str(val_frame["date"].min().date()),
        "val_end": str(val_frame["date"].max().date()),
        "test_start": str(test_frame["date"].min().date()),
        "test_end": str(test_frame["date"].max().date()),
    }

    return TrainResult(
        model=model,
        feature_columns=list(FEATURE_COLUMNS),
        metrics=metrics,
        train_rows=int(len(train_frame)),
        val_rows=int(len(val_frame)),
        test_rows=int(len(test_frame)),
        train_positive_ratio=float(y_train.mean()),
        val_positive_ratio=float(y_val.mean()),
        test_positive_ratio=float(y_test.mean()),
    )
