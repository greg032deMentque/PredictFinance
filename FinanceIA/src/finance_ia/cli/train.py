from __future__ import annotations

import argparse
import json
from pathlib import Path

from finance_ia.config import DEFAULT_END, DEFAULT_OUTPUT_DIR, DEFAULT_START, DEFAULT_TICKERS, TrainConfig
from finance_ia.io.artifacts import save_training_artifacts
from finance_ia.model.train import train_model


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Train a simple double top binary model")
    parser.add_argument("--output-dir", default=str(DEFAULT_OUTPUT_DIR), help="Output artifact directory")
    parser.add_argument("--tickers", nargs="+", default=list(DEFAULT_TICKERS), help="Ticker list")
    parser.add_argument("--start", default=DEFAULT_START, help="Start date YYYY-MM-DD")
    parser.add_argument("--end", default=DEFAULT_END, help="End date YYYY-MM-DD")
    parser.add_argument("--interval", default="1d", help="Yahoo interval (default: 1d)")
    parser.add_argument("--val-size", type=float, default=0.15, help="Temporal validation ratio")
    parser.add_argument("--test-size", type=float, default=0.2, help="Temporal test ratio")
    parser.add_argument(
        "--threshold",
        type=float,
        default=0.5,
        help="Fallback threshold if validation cannot auto-select one",
    )

    parser.add_argument("--peak-window", type=int, default=3)
    parser.add_argument("--min-peak-distance", type=int, default=5)
    parser.add_argument("--max-peak-distance", type=int, default=30)
    parser.add_argument("--peak-tolerance-pct", type=float, default=0.02)
    parser.add_argument("--valley-drop-pct", type=float, default=0.04)
    parser.add_argument("--pretrend-lookback", type=int, default=10)
    parser.add_argument("--min-pretrend-pct", type=float, default=0.05)

    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    config = TrainConfig(
        output_dir=Path(args.output_dir),
        tickers=args.tickers,
        start=args.start,
        end=args.end,
        interval=args.interval,
        val_size=args.val_size,
        test_size=args.test_size,
        classification_threshold=args.threshold,
    )

    config.pattern.peak_window = args.peak_window
    config.pattern.min_peak_distance = args.min_peak_distance
    config.pattern.max_peak_distance = args.max_peak_distance
    config.pattern.peak_tolerance_pct = args.peak_tolerance_pct
    config.pattern.valley_drop_pct = args.valley_drop_pct
    config.pattern.pretrend_lookback = args.pretrend_lookback
    config.pattern.min_pretrend_pct = args.min_pretrend_pct

    result = train_model(config)
    save_training_artifacts(result, config)

    summary = {
        "output_dir": str(config.output_dir),
        "train_rows": result.train_rows,
        "val_rows": result.val_rows,
        "test_rows": result.test_rows,
        "train_positive_ratio": result.train_positive_ratio,
        "val_positive_ratio": result.val_positive_ratio,
        "test_positive_ratio": result.test_positive_ratio,
        "selected_threshold": result.metrics.get("selected_threshold"),
        "metrics": result.metrics,
    }
    print(json.dumps(summary, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
