from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
import pandas as pd
import pytest

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
if str(SRC) not in sys.path:
    sys.path.insert(0, str(SRC))


@pytest.fixture
def sample_ohlcv() -> pd.DataFrame:
    index = pd.date_range("2020-01-01", periods=200, freq="D")
    rng = np.random.default_rng(42)

    baseline = np.linspace(100.0, 130.0, len(index))
    seasonal = 1.5 * np.sin(np.linspace(0.0, 18.0, len(index)))
    close = baseline + seasonal

    open_price = close + rng.normal(0.0, 0.3, len(index))
    high = np.maximum(open_price, close) + 0.7
    low = np.minimum(open_price, close) - 0.7
    volume = rng.integers(1_000, 10_000, len(index))

    return pd.DataFrame(
        {
            "Open": open_price,
            "High": high,
            "Low": low,
            "Close": close,
            "Volume": volume,
        },
        index=index,
    )
