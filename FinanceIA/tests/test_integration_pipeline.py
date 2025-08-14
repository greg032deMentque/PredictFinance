import numpy as np
import pytest
from pipeline.pipeline_double_top import analyze_double_top as adt

@pytest.mark.slow
def test_integration_small_data(monkeypatch):
    # Données factices
    X_train = np.random.rand(10, 5)
    y_train = np.random.randint(0, 2, 10)
    X_val = np.random.rand(5, 5)
    y_val = np.random.randint(0, 2, 5)

    # Patch du train_and_save pour éviter un vrai entraînement lourd
    monkeypatch.setattr(
        adt, "train_and_save",
        lambda *a, **k: ("fake_lgbm", "fake_cnn")
    )
    
    # Patch evaluate_model
    monkeypatch.setattr(
        adt, "evaluate_model",
        lambda *a, **k: {"accuracy": 0.9}
    )

    # Doit s’exécuter sans lever d’erreur
    if hasattr(adt, "main"):
        adt.main()
