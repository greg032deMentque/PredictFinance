import numpy as np
import pandas as pd
import pytest
from types import SimpleNamespace

from pipeline.pipeline_double_top import evaluate_model as em

class DummyModel:
    def predict(self, X):
        return np.random.randint(0, 2, len(X))
    def predict_proba(self, X):
        proba = np.random.rand(len(X), 2)
        proba /= proba.sum(axis=1, keepdims=True)
        return proba

def test_evaluate_model_metrics():
    X_test = np.random.rand(20, 5)
    y_test = np.random.randint(0, 2, 20)
    model = DummyModel()
    
    results = em.evaluate_model(model, X_test, y_test)
    
    assert isinstance(results, dict)
    assert "accuracy" in results
    assert 0 <= results["accuracy"] <= 1
    assert "f1" in results
    assert "auc" in results
