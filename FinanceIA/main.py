from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "src"
if str(SRC) not in sys.path:
    sys.path.insert(0, str(SRC))

from finance_ia.cli.evaluate import main as evaluate_main
from finance_ia.cli.predict import main as predict_main
from finance_ia.cli.simulate import main as simulate_main
from finance_ia.cli.train import main as train_main
from finance_ia.runtime.api import run as serve_main


def main(argv: list[str] | None = None) -> int:
    args = list(sys.argv[1:] if argv is None else argv)
    if not args:
        print("Usage: python main.py [train|predict|evaluate|simulate|serve] <args>")
        return 1

    command = args[0].lower()
    if command == "train":
        return train_main(args[1:])
    if command == "predict":
        return predict_main(args[1:])
    if command == "evaluate":
        return evaluate_main(args[1:])
    if command == "simulate":
        return simulate_main(args[1:])
    if command == "serve":
        return serve_main(args[1:])

    print(f"Unknown command: {command}")
    print("Usage: python main.py [train|predict|evaluate|simulate|serve] <args>")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
