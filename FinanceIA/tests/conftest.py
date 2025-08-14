import pytest
import pandas as pd
import numpy as np

@pytest.fixture
def fake_prices():
    """Série de prix factice pour les tests."""
    dates = pd.date_range("2020-01-01", periods=100)
    prices = pd.Series(np.linspace(100, 200, 100) + np.random.randn(100), index=dates)
    return prices

@pytest.fixture
def fake_volumes():
    """Série de volumes factice."""
    dates = pd.date_range("2020-01-01", periods=100)
    volumes = pd.Series(np.random.randint(1000, 5000, size=100), index=dates)
    return volumes

@pytest.fixture
def input_shape():
    """Forme factice pour les modèles IA."""
    return (30, 5)
