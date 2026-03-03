from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import joblib

from finance_ia.config import TrainConfig
from finance_ia.model.train import TrainResult

MODEL_FILE = "model.joblib"
FEATURE_COLUMNS_FILE = "feature_columns.json"
METRICS_FILE = "metrics.json"
TRAIN_CONFIG_FILE = "train_config.json"


def _write_json(path: Path, payload: dict[str, Any] | list[Any]) -> None:
    path.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")


def save_training_artifacts(result: TrainResult, config: TrainConfig) -> None:
    """Persist model and metadata using a stable artifact contract."""
    output_dir = Path(config.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    joblib.dump(result.model, output_dir / MODEL_FILE)

    _write_json(output_dir / FEATURE_COLUMNS_FILE, list(result.feature_columns))
    _write_json(output_dir / METRICS_FILE, dict(result.metrics))
    _write_json(output_dir / TRAIN_CONFIG_FILE, config.to_dict())


def load_model(model_dir: Path):
    path = Path(model_dir) / MODEL_FILE
    if not path.exists():
        raise FileNotFoundError(f"Model file not found: {path}")
    return joblib.load(path)


def load_feature_columns(model_dir: Path) -> list[str]:
    path = Path(model_dir) / FEATURE_COLUMNS_FILE
    if not path.exists():
        raise FileNotFoundError(f"Feature columns file not found: {path}")
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, list) or not all(isinstance(item, str) for item in data):
        raise ValueError("Invalid feature_columns.json format")
    return data
