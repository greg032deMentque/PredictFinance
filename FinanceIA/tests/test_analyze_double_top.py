import importlib
import sys
import types

def test_main_runs(monkeypatch):
    mod = importlib.import_module("pipeline.pipeline_double_top.analyze_double_top")

    # On monkeypatch les fonctions lourdes
    monkeypatch.setattr(mod, "train_and_save", lambda *a, **k: ("lgbm", "cnn"))
    monkeypatch.setattr(mod, "evaluate_model", lambda *a, **k: None)

    # On simule un appel à main()
    if hasattr(mod, "main"):
        mod.main()
    else:
        pytest.skip("main() non défini")
