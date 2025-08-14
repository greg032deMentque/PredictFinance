import pandas as pd
import numpy as np
from pipeline.pipeline_double_top import pipeline_utils as pu

def test_compute_all_indicators(fake_prices, fake_volumes):
    df = pu.compute_all_indicators(fake_prices, fake_volumes)
    assert isinstance(df, pd.DataFrame)
    assert "rsi" in df.columns
    assert not df.isnull().any().any()

def test_compute_labels(fake_prices):
    labels = pu.compute_labels(fake_prices, "TEST")
    assert isinstance(labels, np.ndarray)
    assert labels.shape[0] == len(fake_prices)
    assert set(labels).issubset({0, 1})

def test_detect_peaks(fake_prices):
    peaks = pu.detect_peaks(fake_prices)
    assert isinstance(peaks, pd.DatetimeIndex)
