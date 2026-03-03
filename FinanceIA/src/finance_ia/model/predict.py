from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from finance_ia.data.yahoo import fetch_ohlcv
from finance_ia.features.indicators import add_indicators
from finance_ia.io.artifacts import load_feature_columns, load_model


@dataclass(slots=True)
class PredictResult:
    ticker: str
    as_of: str
    mean_prob: float
    max_prob: float
    last_prob: float
    n_windows: int

    def to_dict(self) -> dict[str, object]:
        return {
            "schema_version": "1.0",
            "pattern": "double_top",
            "ticker": self.ticker,
            "as_of": self.as_of,
            "mean_prob": self.mean_prob,
            "max_prob": self.max_prob,
            "last_prob": self.last_prob,
            "n_windows": self.n_windows,
        }


def predict_ticker(ticker: str, model_dir: Path, period: str = "6mo") -> PredictResult:
    """Run inference for a ticker and return aggregated probabilities."""
    model = load_model(model_dir)
    feature_columns = load_feature_columns(model_dir)

    raw = fetch_ohlcv(ticker=ticker, period=period, interval="1d")
    enriched = add_indicators(raw)

    missing = [column for column in feature_columns if column not in enriched.columns]
    if missing:
        raise ValueError(f"Missing required feature columns: {missing}")

    inference_frame = enriched.dropna(subset=feature_columns)
    if inference_frame.empty:
        raise ValueError("No inference rows after indicator computation")

    probabilities = model.predict_proba(inference_frame[feature_columns])[:, 1]

    return PredictResult(
        ticker=ticker.strip().upper(),
        as_of=inference_frame.index[-1].date().isoformat(),
        mean_prob=float(probabilities.mean()),
        max_prob=float(probabilities.max()),
        last_prob=float(probabilities[-1]),
        n_windows=int(len(probabilities)),
    )
