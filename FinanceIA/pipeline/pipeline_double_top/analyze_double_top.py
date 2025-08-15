#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Analyse & entraînement Double Top
- Walk-forward CV + Optuna pour LightGBM
- CNN séquentiel (Keras) en complément
- Persistance modèles: /var/www/predictFinance/ModelsIA (ou chemins du config)
- Reprise d'entraînement si modèles déjà présents
"""

from __future__ import annotations

import json
import os
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import List, Tuple

import numpy as np
import pandas as pd

# -----------------------------------------------------------------------------
# Logging (structlog)
# -----------------------------------------------------------------------------
import logging
import structlog
from logging_config import setup_logging

setup_logging(logging.INFO)  # IMPORTANT: appeler avant de créer le logger
LOGGER = structlog.get_logger(__name__)  # logger structlog standard

# -----------------------------------------------------------------------------
# Imports projet (relatifs, car lancé via -m)
# -----------------------------------------------------------------------------
from pipeline.pipeline_double_top.config_double_top import (
    MANUAL_PEAKS,
    LOOKBACK,
    LOOKBETWEEN,
    TOTAL_WINDOW,
    NEG_RATIO,
    MODEL_DIR,               # utilisé pour le dossier de logs local
    ENABLE_AUTO_PEAKS,
    TS_SPLITS,
)
from pipeline.pipeline_double_top.pipeline_utils import fetch_data, compute_all_indicators, detect_peaks

# CNN builder (optionnel, si TF installé)
try:
    from .models_sequential import build_cnn
except ImportError:  # pas de TF? on ignorera la partie CNN
    build_cnn = None  # type: ignore

# -----------------------------------------------------------------------------
# Chemins PERSISTANTS des modèles
# - on essaye de les importer depuis config_double_top, sinon fallback /var/...
# -----------------------------------------------------------------------------
try:
    # s'ils existent déjà dans ton config, on les privilégie
    from pipeline.pipeline_double_top.config_double_top import GBM_PERSIST_FILE as CFG_GBM_PERSIST_FILE  # type: ignore
    from pipeline.pipeline_double_top.config_double_top import CNN_PERSIST_FILE as CFG_CNN_PERSIST_FILE  # type: ignore
except ImportError:
    CFG_GBM_PERSIST_FILE = None
    CFG_CNN_PERSIST_FILE = None

PERSIST_DIR = Path("/var/www/predictFinance/ModelsIA")
PERSIST_DIR.mkdir(parents=True, exist_ok=True)

GBM_PERSIST_FILE = str(
    CFG_GBM_PERSIST_FILE if CFG_GBM_PERSIST_FILE else (PERSIST_DIR / "double_top_lightgbm.joblib")
)
CNN_PERSIST_FILE = str(
    CFG_CNN_PERSIST_FILE if CFG_CNN_PERSIST_FILE else (PERSIST_DIR / "double_top_cnn.keras")
)

# Réduire le bruit TensorFlow si présent
os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")

# -----------------------------------------------------------------------------
# Utils env / versions
# -----------------------------------------------------------------------------
def _pkg_version(name: str) -> str:
    try:
        import importlib.metadata as importlib_metadata
    except ImportError:
        try:
            import importlib_metadata as importlib_metadata  # type: ignore
        except ImportError:
            return "not-installed"
    try:
        return importlib_metadata.version(name)
    except ImportError:
        return "not-installed"


def log_environment() -> None:
    info = {
        "python": sys.version.replace("\n", " "),
        "pid": os.getpid(),
        "cwd": os.getcwd(),
        "platform": sys.platform,
        "numpy": _pkg_version("numpy"),
        "pandas": _pkg_version("pandas"),
        "scipy": _pkg_version("scipy"),
        "yfinance": _pkg_version("yfinance"),
        "lxml": _pkg_version("lxml"),
        "lightgbm": _pkg_version("lightgbm"),
        "imbalanced_learn": _pkg_version("imbalanced-learn"),
        "optuna": _pkg_version("optuna"),
        "mlflow": _pkg_version("mlflow"),
        "tensorflow": _pkg_version("tensorflow"),
    }
    LOGGER.info("environment_info", **info)


def try_import_lightgbm():
    try:
        import lightgbm as lgb  # type: ignore
        return lgb, None
    except OSError as e:
        LOGGER.error("lightgbm_import_error", error=str(e),
                     hint="sudo apt-get update && sudo apt-get install -y libgomp1")
        return None, e
    except Exception as e:
        LOGGER.error("lightgbm_import_error", error=str(e))
        return None, e


def try_import_tf():
    try:
        import tensorflow as tf  # type: ignore
        return tf, None
    except Exception as e:
        return None, e


# -----------------------------------------------------------------------------
# Tickers (S&P 500 via Wikipedia)
# -----------------------------------------------------------------------------
def get_sp500_tickers() -> List[str]:
    url = "https://en.wikipedia.org/wiki/List_of_S%26P_500_companies"
    try:
        LOGGER.debug("tickers_fetch_start", hint=url)
        tables = pd.read_html(url)
        symbols = [str(s).replace(".", "-") for s in tables[0]["Symbol"].tolist()]
        LOGGER.info("tickers_loaded", samples=len(symbols))
        return symbols
    except ImportError as e:
        LOGGER.error("tickers_lxml_missing", error=str(e),
                     hint="pip install lxml (ou apt-get install libxml2-dev libxslt1-dev)")
        return []
    except ValueError as e:
        LOGGER.error("tickers_html_value_error", error=str(e))
        return []
    except Exception as e:
        LOGGER.error("tickers_unknown_error", error=str(e))
        return []


# -----------------------------------------------------------------------------
# Dataset construction
# -----------------------------------------------------------------------------
def generate_dataset() -> Tuple[np.ndarray, np.ndarray]:
    tickers = get_sp500_tickers()
    if not tickers:
        LOGGER.warning("no_tickers_loaded")

    X: List[np.ndarray] = []
    y: List[int] = []

    end_str = datetime.today().strftime("%Y-%m-%d")
    LOGGER.info("dataset_generation_start", samples=len(tickers), hint=end_str)

    for tk in tickers:
        t0 = time.time()
        LOGGER.debug("ticker_start", ticker=tk)

        prices, vols = fetch_data(tk, end_date=end_str)
        if prices is None:
            LOGGER.warning("ticker_fetch_failed", ticker=tk)
            continue

        # Indicateurs
        try:
            ind_df = compute_all_indicators(prices, vols)  # (len, features)
            LOGGER.debug("indicators_ok", ticker=tk, hint=f"shape={ind_df.shape}")
        except Exception as e:
            LOGGER.error("indicators_failed", ticker=tk, error=str(e))
            continue

        # Pics (auto + manuels) -> liste d'indices (i1, i2)
        peaks: List[Tuple[int, int]] = []
        try:
            if ENABLE_AUTO_PEAKS:
                auto_dates = detect_peaks(prices)
                auto_idx = [prices.index.get_loc(d) for d in auto_dates]
                for i1 in auto_idx:
                    for i2 in auto_idx:
                        if 0 < (i2 - i1) <= LOOKBETWEEN:
                            peaks.append((i1, i2))
            for d1, d2 in MANUAL_PEAKS.get(tk, []):
                try:
                    idx1 = prices.index.get_loc(pd.to_datetime(d1))
                    idx2 = prices.index.get_loc(pd.to_datetime(d2))
                except (KeyError, ValueError):
                    continue
                if 0 < (idx2 - idx1) <= LOOKBETWEEN:
                    peaks.append((idx1, idx2))
        except Exception as e:
            LOGGER.error("peaks_failed", ticker=tk, error=str(e))

        # Fenêtres positives
        pos: List[np.ndarray] = []
        for i1, i2 in peaks:
            for shift in range(-3, 4):
                start = i1 - LOOKBACK + shift
                end = start + TOTAL_WINDOW
                if 0 <= start and end <= len(ind_df):
                    # fenêtre de shape (TOTAL_WINDOW, features)
                    pos.append(ind_df.iloc[start:end].values)

        # Fenêtres négatives (tirage aléatoire)
        neg: List[np.ndarray] = []
        max_start = max(len(ind_df) - TOTAL_WINDOW, 0)
        neg_count = int(max(len(pos) * NEG_RATIO, NEG_RATIO))
        if max_start > 0:
            draw = int(min(neg_count, max_start))
            choices = np.random.choice(max_start, draw, replace=False)
            for idx in choices:
                neg.append(ind_df.iloc[idx: idx + TOTAL_WINDOW].values)

        X.extend(pos)
        y.extend([1] * len(pos))
        X.extend(neg)
        y.extend([0] * len(neg))

        LOGGER.info(
            "ticker_windows",
            ticker=tk,
            positives=len(pos),
            negatives=len(neg),
            hint=f"elapsed={time.time()-t0:.2f}s",
        )

    X_arr = np.array(X)   # (n_samples, TOTAL_WINDOW, n_features)
    y_arr = np.array(y)   # (n_samples,)
    LOGGER.info(
        "dataset_generation_end",
        samples=int(len(X_arr)),
        positives=int(y_arr.sum() if len(y_arr) else 0),
        negatives=int(len(y_arr) - y_arr.sum() if len(y_arr) else 0),
    )
    return X_arr, y_arr


# -----------------------------------------------------------------------------
# Walk-forward CV (compat 1.5.x)
# -----------------------------------------------------------------------------
def get_walk_forward_splits(n_splits: int, initial_window: int, test_window: int, gap: int = 0):
    class _WF:
        def __init__(self, n_splits, initial_window, test_window, gap):
            self.n_splits = n_splits
            self.initial_window = initial_window
            self.test_window = test_window
            self.gap = gap

        def split(self, X):
            n = len(X)
            end_train = self.initial_window
            splits = 0
            while True:
                start_test = end_train + self.gap
                end_test = start_test + self.test_window
                if end_test > n or splits >= self.n_splits:
                    break
                train_idx = list(range(0, end_train))
                test_idx = list(range(start_test, end_test))
                yield (train_idx, test_idx)
                end_train = end_test
                splits += 1
    return _WF(n_splits, initial_window, test_window, gap)


# -----------------------------------------------------------------------------
# Optuna objective (LightGBM uniquement)
# -----------------------------------------------------------------------------
def objective(trial, X: np.ndarray, y: np.ndarray) -> float:
    from imblearn.over_sampling import ADASYN
    from imblearn.pipeline import Pipeline as ImbPipeline
    from sklearn.preprocessing import StandardScaler
    from sklearn.metrics import roc_auc_score

    lgb, err = try_import_lightgbm()
    if lgb is None:
        LOGGER.error("objective_lightgbm_missing", error=str(err))
        return 0.0

    params = {
        "num_leaves": trial.suggest_int("num_leaves", 20, 100),
        "learning_rate": trial.suggest_float("learning_rate", 1e-3, 1e-1, log=True),
        "n_estimators": trial.suggest_int("n_estimators", 50, 300),
        "verbose": -1,
    }
    LOGGER.debug("trial_params", hint=json.dumps(params))

    n = len(X)
    initial = max(int(0.5 * n), 1)
    test_w = max(int(0.1 * n), 1)
    splitter = get_walk_forward_splits(TS_SPLITS, initial, test_w)

    scores: List[float] = []
    for fold, (tr, te) in enumerate(splitter.split(X), start=1):
        pipe = ImbPipeline([
            ("adasyn", ADASYN()),
            ("scaler", StandardScaler()),
            ("clf", lgb.LGBMClassifier(**params)),
        ])
        # LightGBM -> flatten
        X_tr = X[tr].reshape(len(tr), -1)
        X_te = X[te].reshape(len(te), -1)
        try:
            pipe.fit(X_tr, y[tr])
        except Exception:
            from sklearn.pipeline import Pipeline
            pipe = Pipeline([
                ("scaler", StandardScaler()),
                ("clf", lgb.LGBMClassifier(**params)),
            ])
            pipe.fit(X_tr, y[tr])
        preds = pipe.predict_proba(X_te)[:, 1]
        auc = float(roc_auc_score(y[te], preds))
        scores.append(auc)
        LOGGER.debug("fold_score", hint=f"fold={fold} auc={auc:.4f}")

    m = float(np.mean(scores)) if scores else 0.0
    LOGGER.info("trial_summary", hint=f"auc_mean={m:.4f}")
    return m


# -----------------------------------------------------------------------------
# Entraînement + sauvegarde (LGBM + CNN)
# -----------------------------------------------------------------------------
def train_and_save() -> None:
    # imports locaux
    global mlflow
    try:
        import mlflow  # type: ignore
        _MLFLOW_OK = True
    except Exception:
        _MLFLOW_OK = False

    import optuna
    from imblearn.over_sampling import ADASYN
    from imblearn.pipeline import Pipeline as ImbPipeline
    from sklearn.preprocessing import StandardScaler
    from sklearn.metrics import roc_auc_score

    log_environment()

    lgb, err = try_import_lightgbm()
    if lgb is None:
        LOGGER.error("training_aborted_lightgbm_missing")
        return

    X, y = generate_dataset()
    if len(y) == 0 or len(np.unique(y)) < 2:
        LOGGER.error("single_class_or_empty_dataset", samples=int(len(y)))
        return

    # Optuna pour LGBM
    if _MLFLOW_OK:
        try:
            mlflow.autolog()
        except Exception:
            LOGGER.warning("mlflow_autolog_failed")

    LOGGER.info("optuna_study_start")
    study = optuna.create_study(direction="maximize", pruner=optuna.pruners.MedianPruner())
    study.optimize(lambda t: objective(t, X, y), n_trials=30)
    best = study.best_params
    LOGGER.info("best_params", hint=json.dumps(best))

    # Walk-forward CV (report)
    n = len(X)
    initial = max(int(0.5 * n), 1)
    test_w = max(int(0.1 * n), 1)
    splitter = get_walk_forward_splits(TS_SPLITS, initial, test_w)

    aucs: List[float] = []
    for tr, te in splitter.split(X):
        pipe = ImbPipeline([
            ("adasyn", ADASYN()),
            ("scaler", StandardScaler()),
            ("gbm", lgb.LGBMClassifier(**best, verbose=-1)),
        ])
        try:
            pipe.fit(X[tr].reshape(len(tr), -1), y[tr])
        except Exception:
            from sklearn.pipeline import Pipeline
            pipe = Pipeline([
                ("scaler", StandardScaler()),
                ("gbm", lgb.LGBMClassifier(**best, verbose=-1)),
            ])
            pipe.fit(X[tr].reshape(len(tr), -1), y[tr])
        preds = pipe.predict_proba(X[te].reshape(len(te), -1))[:, 1]
        aucs.append(float(roc_auc_score(y[te], preds)))
    LOGGER.info("cv_summary", hint=f"mean={np.mean(aucs):.4f} std={np.std(aucs):.4f}")

    # ======= Entraînement final LightGBM (avec reprise si possible) =======
    try:
        import joblib, os
        if os.path.exists(GBM_PERSIST_FILE):
            LOGGER.info("loading_existing_gbm", path=GBM_PERSIST_FILE)
            loaded = joblib.load(GBM_PERSIST_FILE)
            try:
                if hasattr(loaded, "named_steps"):
                    gbm = loaded.named_steps.get("gbm")
                    if gbm is not None and hasattr(gbm, "set_params"):
                        n_old = getattr(gbm, "n_estimators", best.get("n_estimators", 100))
                        gbm.set_params(warm_start=True, n_estimators=n_old + 100)
                        loaded.fit(X.reshape(len(X), -1), y)
                        final_gbm = loaded
                    else:
                        final_gbm = ImbPipeline([
                            ("adasyn", ADASYN()),
                            ("scaler", StandardScaler()),
                            ("gbm", lgb.LGBMClassifier(**best, verbose=-1)),
                        ])
                        final_gbm.fit(X.reshape(len(X), -1), y)
                else:
                    final_gbm = ImbPipeline([
                        ("adasyn", ADASYN()),
                        ("scaler", StandardScaler()),
                        ("gbm", lgb.LGBMClassifier(**best, verbose=-1)),
                    ])
                    final_gbm.fit(X.reshape(len(X), -1), y)
            except Exception as e:
                LOGGER.warning("gbm_warm_start_failed_training_new", error=str(e))
                final_gbm = ImbPipeline([
                    ("adasyn", ADASYN()),
                    ("scaler", StandardScaler()),
                    ("gbm", lgb.LGBMClassifier(**best, verbose=-1)),
                ])
                final_gbm.fit(X.reshape(len(X), -1), y)
        else:
            final_gbm = ImbPipeline([
                ("adasyn", ADASYN()),
                ("scaler", StandardScaler()),
                ("gbm", lgb.LGBMClassifier(**best, verbose=-1)),
            ])
            final_gbm.fit(X.reshape(len(X), -1), y)

        joblib.dump(final_gbm, GBM_PERSIST_FILE)
        LOGGER.info("gbm_saved", path=GBM_PERSIST_FILE)
    except Exception as e:
        LOGGER.error("gbm_save_failed", error=str(e), path=GBM_PERSIST_FILE)

    # ======= Entraînement / reprise CNN (optionnel) =======
    tf, tf_err = try_import_tf()
    if tf is None or build_cnn is None:
        LOGGER.warning("cnn_skipped_tensorflow_missing", error=str(tf_err))
        return

    # Préparer X pour CNN -> (n, TOTAL_WINDOW, n_features)
    X_cnn = X.astype(np.float32)
    y_cnn = y.astype(np.float32)

    # split rapide train/val
    try:
        from sklearn.model_selection import train_test_split
        X_tr_cnn, X_va_cnn, y_tr_cnn, y_va_cnn = train_test_split(
            X_cnn, y_cnn, test_size=0.1, random_state=42, stratify=y_cnn
        )
    except Exception:
        # fallback simple
        split = max(int(0.9 * len(X_cnn)), 1)
        X_tr_cnn, X_va_cnn = X_cnn[:split], X_cnn[split:]
        y_tr_cnn, y_va_cnn = y_cnn[:split], y_cnn[split:]

    EPOCHS = int(os.getenv("CNN_EPOCHS", "5"))
    BATCH_SIZE = int(os.getenv("CNN_BATCH_SIZE", "32"))

    try:
        if os.path.exists(CNN_PERSIST_FILE):
            LOGGER.info("loading_existing_cnn", path=CNN_PERSIST_FILE)
            cnn = tf.keras.models.load_model(CNN_PERSIST_FILE)
            # reprise d'entraînement (fine-tuning)
            cnn.fit(
                X_tr_cnn, y_tr_cnn,
                validation_data=(X_va_cnn, y_va_cnn),
                epochs=EPOCHS,
                batch_size=BATCH_SIZE,
                verbose=1,
            )
        else:
            input_shape = X_tr_cnn.shape[1:]  # (TOTAL_WINDOW, n_features)
            cnn = build_cnn(input_shape)
            cnn.compile(optimizer="adam", loss="binary_crossentropy", metrics=["accuracy", tf.keras.metrics.AUC(name="auc")])
            cnn.fit(
                X_tr_cnn, y_tr_cnn,
                validation_data=(X_va_cnn, y_va_cnn),
                epochs=EPOCHS,
                batch_size=BATCH_SIZE,
                verbose=1,
            )
        cnn.save(CNN_PERSIST_FILE)
        LOGGER.info("cnn_saved", path=CNN_PERSIST_FILE)
    except Exception as e:
        LOGGER.error("cnn_training_or_save_failed", error=str(e), path=CNN_PERSIST_FILE)


# -----------------------------------------------------------------------------
# MAIN
# -----------------------------------------------------------------------------
def main() -> None:
    t0 = time.time()
    LOGGER.info("script_start")
    try:
        train_and_save()
        elapsed = time.time() - t0
        LOGGER.info("script_end_success", elapsed=f"{elapsed:.2f}s")
    except Exception as e:
        LOGGER.exception("script_failed", error=str(e))
        raise


if __name__ == "__main__":
    # Si jamais tu veux aussi pouvoir l'exécuter "direct", utilise plutôt:
    #   python -m pipeline.pipeline_double_top.analyze_double_top
    main()
