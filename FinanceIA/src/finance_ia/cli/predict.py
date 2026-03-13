from __future__ import annotations

import argparse
import json
from pathlib import Path

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
    result = predict_ticker(
        ticker=args.ticker,
        model_dir=Path(args.model_dir),
        period=args.period,
        pattern=args.pattern,
    )
    print(json.dumps(result.to_dict(), indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
