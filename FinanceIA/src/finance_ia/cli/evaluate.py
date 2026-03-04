from __future__ import annotations

import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from uuid import uuid4

from finance_ia.config import DEFAULT_END, DEFAULT_START
from finance_ia.model.validate import EvaluationThresholds, validate_model_on_ticker


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Evaluate a trained model on a ticker and date range")
    parser.add_argument("--ticker", required=True, help="Ticker symbol, e.g. AAPL")
    parser.add_argument("--model-dir", default="artifacts/double_top", help="Artifact directory")
    parser.add_argument("--start", default=DEFAULT_START, help="Start date YYYY-MM-DD")
    parser.add_argument("--end", default=DEFAULT_END, help="End date YYYY-MM-DD")
    parser.add_argument("--interval", default="1d", help="Yahoo interval (default: 1d)")
    parser.add_argument(
        "--threshold",
        type=float,
        default=None,
        help="Optional threshold override. If omitted, uses selected_threshold from metrics.json",
    )
    parser.add_argument(
        "--threshold-grid",
        default="0.10,0.20,0.30,0.40,0.50",
        help="Comma-separated thresholds to compare (for precision/recall/F1 analysis)",
    )
    parser.add_argument("--min-precision", type=float, default=0.10)
    parser.add_argument("--min-recall", type=float, default=0.05)
    parser.add_argument("--min-f1", type=float, default=0.08)
    parser.add_argument("--min-roc-auc", type=float, default=0.60)
    parser.add_argument("--min-rows", type=int, default=200)
    parser.add_argument("--min-positive-samples", type=int, default=5)
    parser.add_argument("--report-dir", default="artifacts/evaluation", help="Directory to store evaluation logs")
    parser.add_argument(
        "--run-folder-name",
        default=None,
        help="Optional existing/new run folder name under report-dir (example: evaluation_<GUID>_<YYYYMMDD>_<N>)",
    )
    parser.add_argument("--no-report", action="store_true", help="Do not persist evaluation report file")
    return parser


def _parse_threshold_grid(raw_grid: str) -> list[float]:
    values: list[float] = []
    for chunk in raw_grid.split(","):
        text = chunk.strip()
        if not text:
            continue
        values.append(float(text))
    if not values:
        raise ValueError("threshold-grid is empty")
    return values


def _compute_next_daily_number(report_dir: Path, date_today: str) -> int:
    pattern = re.compile(rf"^evaluation_[0-9a-fA-F-]{{36}}_{date_today}_(\d+)$")
    max_number = 0
    for path in report_dir.iterdir():
        if not path.is_dir():
            continue
        match = pattern.match(path.name)
        if match is None:
            continue
        number = int(match.group(1))
        if number > max_number:
            max_number = number
    return max_number + 1


def _resolve_run_folder_name(report_dir: Path, explicit_run_folder_name: str | None) -> str:
    if explicit_run_folder_name:
        return explicit_run_folder_name

    date_today = datetime.now(timezone.utc).strftime("%Y%m%d")
    daily_number = _compute_next_daily_number(report_dir, date_today)
    return f"evaluation_{uuid4()}_{date_today}_{daily_number}"


def _write_report(report_dir: Path, payload: dict[str, object], run_folder_name: str | None = None) -> str:
    report_dir.mkdir(parents=True, exist_ok=True)
    resolved_run_folder_name = _resolve_run_folder_name(report_dir, run_folder_name)
    run_dir = report_dir / resolved_run_folder_name
    run_dir.mkdir(parents=True, exist_ok=True)

    ticker = str(payload.get("ticker", "UNKNOWN")).upper()
    report_path = run_dir / f"evaluation_{ticker}.json"
    report_path.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")
    return str(report_path)


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    threshold_grid = _parse_threshold_grid(args.threshold_grid)

    thresholds = EvaluationThresholds(
        min_precision=args.min_precision,
        min_recall=args.min_recall,
        min_f1=args.min_f1,
        min_roc_auc=args.min_roc_auc,
        min_rows=args.min_rows,
        min_positive_samples=args.min_positive_samples,
    )

    result = validate_model_on_ticker(
        model_dir=Path(args.model_dir),
        ticker=args.ticker,
        start=args.start,
        end=args.end,
        interval=args.interval,
        threshold=args.threshold,
        threshold_grid=threshold_grid,
        evaluation_thresholds=thresholds,
    )

    payload = result.to_dict()
    payload["evaluation_thresholds"] = {
        "min_precision": thresholds.min_precision,
        "min_recall": thresholds.min_recall,
        "min_f1": thresholds.min_f1,
        "min_roc_auc": thresholds.min_roc_auc,
        "min_rows": thresholds.min_rows,
        "min_positive_samples": thresholds.min_positive_samples,
    }

    if not args.no_report:
        report_path = _write_report(Path(args.report_dir), payload, run_folder_name=args.run_folder_name)
        payload["report_file"] = report_path

    print(json.dumps(payload, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
