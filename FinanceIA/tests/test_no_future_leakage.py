from __future__ import annotations

import inspect

from finance_ia.features import indicators


def test_features_module_does_not_use_future_shift() -> None:
    source = inspect.getsource(indicators.add_indicators)
    assert "shift(-" not in source
    assert "center=True" not in source
