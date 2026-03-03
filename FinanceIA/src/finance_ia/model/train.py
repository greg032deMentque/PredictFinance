from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import pandas as pd
from lightgbm import LGBMClassifier

from finance_ia.config import FEATURE_COLUMNS, TrainConfig
from finance_ia.dataset.build_dataset import build_training_frame, temporal_train_test_split
from finance_ia.model.evaluate import evaluate_binary_classifier


@dataclass(slots=True)
class TrainResult:
    model: LGBMClassifier
    feature_columns: list[str]
    metrics: dict[str, object]
    train_rows: int
    test_rows: int
    train_positive_ratio: float
    test_positive_ratio: float


def _prepare_xy(frame: pd.DataFrame) -> tuple[pd.DataFrame, pd.Series]:
    x_frame = frame[FEATURE_COLUMNS]
    y_series = frame["target"].astype("int64")
    return x_frame, y_series


def train_model(config: TrainConfig) -> TrainResult:
    """Train a single LightGBM binary classifier with temporal split."""
    dataset = build_training_frame(config)
    train_frame, test_frame = temporal_train_test_split(dataset, test_size=config.test_size)

    x_train, y_train = _prepare_xy(train_frame)
    x_test, y_test = _prepare_xy(test_frame)

    if y_train.nunique() < 2:
        raise ValueError("Training target has a single class. Expand the date range or ticker set.")

    params: dict[str, Any] = dict(config.model_params)
    params.setdefault("class_weight", "balanced")
    params.setdefault("random_state", config.random_state)

    model = LGBMClassifier(**params)
    model.fit(x_train, y_train)

    test_proba = model.predict_proba(x_test)[:, 1]
    metrics = evaluate_binary_classifier(
        y_true=y_test.to_numpy(),
        y_proba=test_proba,
        threshold=config.classification_threshold,
    )

    metrics.update(
        {
            "train_start": str(train_frame["date"].min().date()),
            "train_end": str(train_frame["date"].max().date()),
            "test_start": str(test_frame["date"].min().date()),
            "test_end": str(test_frame["date"].max().date()),
        }
    )

    return TrainResult(
        model=model,
        feature_columns=list(FEATURE_COLUMNS),
        metrics=metrics,
        train_rows=int(len(train_frame)),
        test_rows=int(len(test_frame)),
        train_positive_ratio=float(y_train.mean()),
        test_positive_ratio=float(y_test.mean()),
    )
