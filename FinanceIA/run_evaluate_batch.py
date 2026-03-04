from __future__ import annotations

import argparse
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from uuid import uuid4


DEFAULT_TICKERS = ["AAPL", "MSFT", "NVDA", "AMZN", "GOOGL", "META", "JPM", "XOM", "TSLA"]


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run evaluation for multiple tickers")
    parser.add_argument("--tickers", nargs="+", default=list(DEFAULT_TICKERS), help="Ticker list")
    parser.add_argument("--model-dir", default="artifacts/double_top", help="Model directory")
    parser.add_argument("--start", default="2023-01-01", help="Start date YYYY-MM-DD")
    parser.add_argument("--end", default="2025-12-31", help="End date YYYY-MM-DD")
    parser.add_argument("--report-dir", default="artifacts/evaluation", help="Base directory for evaluation reports")
    parser.add_argument("--threshold-grid", default="0.05,0.10,0.15,0.20,0.30,0.40,0.50")
    parser.add_argument("--min-precision", type=float, default=0.10)
    parser.add_argument("--min-recall", type=float, default=0.05)
    parser.add_argument("--min-f1", type=float, default=0.08)
    parser.add_argument("--min-roc-auc", type=float, default=0.60)
    parser.add_argument("--min-rows", type=int, default=200)
    parser.add_argument("--min-positive-samples", type=int, default=5)
    parser.add_argument("--no-report", action="store_true", help="Do not persist report files")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    tickers = [ticker.strip().upper() for ticker in args.tickers if ticker and ticker.strip()]
    if not tickers:
        raise ValueError("No valid tickers provided")

    run_folder_name: str | None = None
    if not args.no_report:
        report_dir = Path(args.report_dir)
        report_dir.mkdir(parents=True, exist_ok=True)
        date_today = datetime.now(timezone.utc).strftime("%Y%m%d")
        pattern = f"evaluation_*_{date_today}_*"
        numbers: list[int] = []
        for existing_dir in report_dir.glob(pattern):
            if not existing_dir.is_dir():
                continue
            parts = existing_dir.name.rsplit("_", maxsplit=1)
            if len(parts) != 2:
                continue
            if not parts[1].isdigit():
                continue
            numbers.append(int(parts[1]))
        next_number = (max(numbers) + 1) if numbers else 1
        run_folder_name = f"evaluation_{uuid4()}_{date_today}_{next_number}"
        (report_dir / run_folder_name).mkdir(parents=False, exist_ok=False)

    print(f"Using Python executable: {sys.executable}", flush=True)
    print(f"Running evaluate for {len(tickers)} tickers...", flush=True)
    if run_folder_name is not None:
        print(f"Batch report directory: {Path(args.report_dir) / run_folder_name}", flush=True)

    for ticker in tickers:
        print(f"---- {ticker} ----", flush=True)
        command = [
            sys.executable,
            "-m",
            "finance_ia.cli.evaluate",
            "--ticker",
            ticker,
            "--model-dir",
            args.model_dir,
            "--start",
            args.start,
            "--end",
            args.end,
            "--threshold-grid",
            args.threshold_grid,
            "--min-precision",
            str(args.min_precision),
            "--min-recall",
            str(args.min_recall),
            "--min-f1",
            str(args.min_f1),
            "--min-roc-auc",
            str(args.min_roc_auc),
            "--min-rows",
            str(args.min_rows),
            "--min-positive-samples",
            str(args.min_positive_samples),
        ]

        if args.no_report:
            command.append("--no-report")
        elif run_folder_name is not None:
            command.extend(["--report-dir", str(args.report_dir), "--run-folder-name", run_folder_name])

        result = subprocess.run(command, check=False)
        if result.returncode != 0:
            print(f"Evaluation failed for ticker {ticker}", file=sys.stderr)
            return result.returncode

    print("Batch evaluation completed.", flush=True)
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
