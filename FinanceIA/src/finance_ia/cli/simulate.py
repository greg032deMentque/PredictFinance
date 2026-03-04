from __future__ import annotations

import argparse
import json
from pathlib import Path

from finance_ia.model.simulate import simulate_ticker


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run simulation for one ticker")
    parser.add_argument("--ticker", required=True, help="Ticker symbol, e.g. AAPL")
    parser.add_argument("--model-dir", default="artifacts/double_top", help="Artifact directory")
    parser.add_argument("--period", default="6mo", help="Yahoo period for inference")
    parser.add_argument("--pattern", default="DOUBLE_TOP", help="Pattern to simulate (DOUBLE_TOP only)")
    parser.add_argument("--investment-amount", required=True, type=float, help="Investment amount")
    parser.add_argument("--horizon-days", default=30, type=int, help="Investment horizon in days")
    parser.add_argument("--sell-threshold", default=0.65, type=float, help="Sell action threshold")
    parser.add_argument("--buy-threshold", default=0.20, type=float, help="Buy action threshold")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    result = simulate_ticker(
        ticker=args.ticker,
        model_dir=Path(args.model_dir),
        period=args.period,
        pattern=args.pattern,
        investment_amount=args.investment_amount,
        horizon_days=args.horizon_days,
        sell_threshold=args.sell_threshold,
        buy_threshold=args.buy_threshold,
    )
    print(json.dumps(result.to_dict(), indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
