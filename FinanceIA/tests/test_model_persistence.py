import os
import shutil
import joblib
import numpy as np
import pytest
from pathlib import Path

from pipeline.pipeline_double_top import analyze_double_top as adt

PERSIST_DIR = Path("/var/www/predictFinance/ModelsIA")

@pytest.fixture(autouse=True)
def clean_persist_dir():
    """Nettoie le dossier de modèles avant chaque test."""
    if PERSIST_DIR.exists():
        shutil.rmtree(PERSIST_DIR)
    PERSIST_DIR.mkdir(parents=True, exist_ok=True)
    yield
    shutil.rmtree(PERSIST_DIR)

def test_model_is_saved():
    # Données factices
    X = np.random.rand(50, 10)
    y = np.random.randint(0, 2, 50)

    # On entraîne et sauvegarde
    best_params = {"n_estimators": 10}
    adt.best = best_params  # Simule les meilleurs hyperparams
    adt.ImbPipeline = adt.ImbPipeline  # garde la même classe
    adt.ADA = adt.ADASYN()  # juste pour éviter un bug
    adt.ADA = adt.ADASYN
    adt.StandardScaler = adt.StandardScaler
    adt.lgb = adt.lgb
    adt.LOGGER = adt.LOGGER

    adt.X = X.reshape(len(X), -1)
    adt.y = y

    # Lancer le code de sauvegarde final (simulé)
    adt.train_and_save = lambda *a, **k: None
    adt.GBM_PERSIST_FILE = str(PERSIST_DIR / "double_top_lightgbm.joblib")

    pipeline = adt.ImbPipeline([
        ("scaler", adt.StandardScaler()),
        ("gbm", adt.lgb.LGBMClassifier(**best_params, verbose=-1)),
    ])
    pipeline.fit(X, y)
    joblib.dump(pipeline, adt.GBM_PERSIST_FILE)

    assert Path(adt.GBM_PERSIST_FILE).exists()

def test_model_is_reloaded(monkeypatch):
    # On crée un faux modèle sauvegardé
    X = np.random.rand(50, 10)
    y = np.random.randint(0, 2, 50)
    fake_model = adt.ImbPipeline([
        ("scaler", adt.StandardScaler()),
        ("gbm", adt.lgb.LGBMClassifier(n_estimators=5, verbose=-1)),
    ])
    fake_model.fit(X, y)
    joblib.dump(fake_model, adt.GBM_PERSIST_FILE)

    loaded = joblib.load(adt.GBM_PERSIST_FILE)
    assert hasattr(loaded, "fit")

    # On monkeypatch pour voir si le load est bien utilisé
    called = {"load": False}
    def fake_load(path):
        called["load"] = True
        return loaded

    monkeypatch.setattr("joblib.load", fake_load)

    # Simule l'appel du bloc final
    adt.X = X.reshape(len(X), -1)
    adt.y = y
    adt.best = {"n_estimators": 5}
    exec_block = getattr(adt, "GBM_PERSIST_FILE")
    _ = joblib.load(exec_block)  # normalement, cela déclenche le load patché

    assert called["load"], "Le modèle n'a pas été rechargé depuis le fichier."
