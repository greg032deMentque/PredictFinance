from __future__ import annotations

import argparse
from pathlib import Path

from finance_ia.cli.error_handling import run_cli_command
from finance_ia.model.predict import predict_ticker


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run inference for one ticker")
    parser.add_argument("--ticker", required=True, help="Ticker symbol, e.g. AAPL")
    parser.add_argument("--model-dir", default="artifacts/double_top", help="Artifact directory")
    parser.add_argument("--period", default="6mo", help="Yahoo period for inference")
    parser.add_argument("--pattern", default="DOUBLE_TOP", help="Pattern to analyze")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    return run_cli_command(
        operation="predict",
        ticker=args.ticker,
        pattern=args.pattern,
        action=lambda: predict_ticker(
            ticker=args.ticker,
            model_dir=Path(args.model_dir),
            period=args.period,
            pattern=args.pattern,
        ),
    )


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
