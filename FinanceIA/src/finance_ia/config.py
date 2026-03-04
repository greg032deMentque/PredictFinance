from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date
from pathlib import Path
from typing import Any

DEFAULT_TICKERS = ["AAPL", "MSFT", "NVDA", "AMZN", "GOOGL", "META", "JPM", "XOM"]
DEFAULT_START = "2018-01-01"
DEFAULT_END = date.today().isoformat()
DEFAULT_INTERVAL = "1d"
DEFAULT_OUTPUT_DIR = Path("artifacts/double_top")

FEATURE_COLUMNS = [
    "ret_1d",
    "ret_5d",
    "sma_10",
    "sma_20",
    "ema_10",
    "ema_20",
    "rsi_14",
    "macd",
    "macd_signal",
    "macd_hist",
    "volatility_10",
    "volume_norm_20",
]


@dataclass(slots=True)
class PatternConfig:
    peak_window: int = 3
    min_peak_distance: int = 5
    max_peak_distance: int = 30
    peak_tolerance_pct: float = 0.02
    valley_drop_pct: float = 0.04
    pretrend_lookback: int = 10
    min_pretrend_pct: float = 0.05


def pattern_config_from_dict(payload: dict[str, Any] | None) -> PatternConfig:
    if payload is None:
        return PatternConfig()
    return PatternConfig(
        peak_window=int(payload.get("peak_window", 3)),
        min_peak_distance=int(payload.get("min_peak_distance", 5)),
        max_peak_distance=int(payload.get("max_peak_distance", 30)),
        peak_tolerance_pct=float(payload.get("peak_tolerance_pct", 0.02)),
        valley_drop_pct=float(payload.get("valley_drop_pct", 0.04)),
        pretrend_lookback=int(payload.get("pretrend_lookback", 10)),
        min_pretrend_pct=float(payload.get("min_pretrend_pct", 0.05)),
    )


@dataclass(slots=True)
class TrainConfig:
    output_dir: Path = DEFAULT_OUTPUT_DIR
    tickers: list[str] = field(default_factory=lambda: list(DEFAULT_TICKERS))
    start: str = DEFAULT_START
    end: str = field(default_factory=lambda: date.today().isoformat())
    interval: str = DEFAULT_INTERVAL
    val_size: float = 0.15
    test_size: float = 0.2
    random_state: int = 42
    classification_threshold: float = 0.5
    pattern: PatternConfig = field(default_factory=PatternConfig)
    model_params: dict[str, Any] = field(
        default_factory=lambda: {
            "n_estimators": 300,
            "learning_rate": 0.05,
            "num_leaves": 31,
            "random_state": 42,
            "class_weight": "balanced",
            "n_jobs": -1,
        }
    )

    def __post_init__(self) -> None:
        self.output_dir = Path(self.output_dir)
        self.tickers = [ticker.strip().upper() for ticker in self.tickers if ticker and ticker.strip()]
        if not self.tickers:
            raise ValueError("Tickers list is empty")
        if not (0.05 <= self.test_size <= 0.5):
            raise ValueError("test_size must be between 0.05 and 0.5")
        if not (0.05 <= self.val_size <= 0.3):
            raise ValueError("val_size must be between 0.05 and 0.3")
        if (self.val_size + self.test_size) >= 0.7:
            raise ValueError("val_size + test_size must remain below 0.7")

    def to_dict(self) -> dict[str, Any]:
        return {
            "output_dir": str(self.output_dir),
            "tickers": list(self.tickers),
            "start": self.start,
            "end": self.end,
            "interval": self.interval,
            "val_size": self.val_size,
            "test_size": self.test_size,
            "random_state": self.random_state,
            "classification_threshold": self.classification_threshold,
            "pattern": {
                "peak_window": self.pattern.peak_window,
                "min_peak_distance": self.pattern.min_peak_distance,
                "max_peak_distance": self.pattern.max_peak_distance,
                "peak_tolerance_pct": self.pattern.peak_tolerance_pct,
                "valley_drop_pct": self.pattern.valley_drop_pct,
                "pretrend_lookback": self.pattern.pretrend_lookback,
                "min_pretrend_pct": self.pattern.min_pretrend_pct,
            },
            "model_params": dict(self.model_params),
            "feature_columns": list(FEATURE_COLUMNS),
        }
