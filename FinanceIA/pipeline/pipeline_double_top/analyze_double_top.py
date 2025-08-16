#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Analyse & entraînement Double Top
- Walk-forward CV + Optuna pour LightGBM (tabulaire)
- CNN séquentiel (Keras/TensorFlow) en complément (données 3D temporelles)
- Persistance modèles: /var/www/predictFinance/ModelsIA (ou chemins du config)
- Reprise d'entraînement LightGBM si modèle déjà présent (warm start)
- MLflow pour tracer les runs (sécurisé par try/except)
"""

from __future__ import annotations

# ======================
# Imports standard
# ======================
import json
import logging
import os
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import List, Tuple, Optional

# ======================
# Imports tiers
# ======================
import joblib
import numpy as np
import optuna
import pandas as pd
import structlog

from imblearn.over_sampling import ADASYN
from imblearn.pipeline import Pipeline as ImbPipeline

from lightgbm import LGBMClassifier

from sklearn.metrics import (
    accuracy_score,
    f1_score,
    precision_score,
    recall_score,
    roc_auc_score,
)
from sklearn.model_selection import StratifiedKFold, cross_val_score, train_test_split
from sklearn.preprocessing import StandardScaler

# --- MLflow (protégé) ---
import mlflow
import mlflow.lightgbm
import mlflow.sklearn
try:
    import mlflow.tensorflow
except Exception:
    # Sur certaines stacks, mlflow.tensorflow peut ne pas être dispo;
    # on loguera en mode sklearn only si besoin.
    pass
from mlflow.models import infer_signature

# ======================
# Logging structlog
# ======================
from logging_config import setup_logging

setup_logging(logging.INFO)  # IMPORTANT : configure les handlers avant création du logger
LOGGER = structlog.get_logger(__name__)

# ======================
# Imports projet
# ======================
# Ces imports doivent exister dans ton repo (pipeline/utils + config)
from pipeline.pipeline_double_top.config_double_top import (
    MANUAL_PEAKS,
    LOOKBACK,
    LOOKBETWEEN,
    TOTAL_WINDOW,
    NEG_RATIO,
    MODEL_DIR,
    ENABLE_AUTO_PEAKS,
    TS_SPLITS,
)
from pipeline.pipeline_double_top.pipeline_utils import (
    fetch_data,
    compute_all_indicators,
    detect_peaks,
    is_double_top
)

# --- CNN builder (optionnel) ---
# On essaie plusieurs chemins d'import au cas où ton module soit dans un package.
_build_cnn = None  # fonction constructeur de modèle Keras si disponible

try:
    # Cas: fichier dans le même package (import relatif)
    from pipeline.pipeline_double_top.models_sequential import build_cnn as _build_cnn
except Exception:
    try:
        # Cas: voisin direct (si tu exécutes en tant que module)
        from .models_sequential import build_cnn as _build_cnn  # type: ignore
    except Exception:
        _build_cnn = None

# Réduire le bruit TensorFlow (logs C++/GPU, etc.)
os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")

# ======================
# Chemins persistants
# ======================
try:
    from pipeline.pipeline_double_top.config_double_top import GBM_PERSIST_FILE as CFG_GBM_PERSIST_FILE
    from pipeline.pipeline_double_top.config_double_top import CNN_PERSIST_FILE as CFG_CNN_PERSIST_FILE
except Exception:
    CFG_GBM_PERSIST_FILE = None
    CFG_CNN_PERSIST_FILE = None

PERSIST_DIR = Path("/var/www/predictFinance/ModelsIA")
PERSIST_DIR.mkdir(parents=True, exist_ok=True)

GBM_PERSIST_FILE = str(CFG_GBM_PERSIST_FILE or (PERSIST_DIR / "double_top_lightgbm.joblib"))
CNN_PERSIST_FILE = str(CFG_CNN_PERSIST_FILE or (PERSIST_DIR / "double_top_cnn.keras"))

# ======================
# Utilitaires env/versions
# ======================
def _pkg_version(name: str) -> str:
    """Retourne la version d'un package si possible (utile pour déboguer les stacks)."""
    try:
        import importlib.metadata as importlib_metadata
    except ImportError:
        try:
            import importlib_metadata as importlib_metadata  # type: ignore
        except ImportError:
            return "not-installed"
    try:
        return importlib_metadata.version(name)
    except Exception:
        return "not-installed"


def log_environment() -> None:
    """Logue les versions des dépendances clés (utile si comportement étrange)."""
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
        "keras": _pkg_version("keras"),
    }
    LOGGER.info("environment_info", **info)


def try_import_lightgbm():
    """Teste import LightGBM bas niveau (utile pour messages d'erreurs plus clairs)."""
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
    """Teste import TensorFlow (utile pour décider si on entraîne le CNN)."""
    try:
        import tensorflow as tf  # type: ignore
        return tf, None
    except Exception as e:
        return None, e


# ======================
# Récupération tickers (S&P500)
# ======================
def get_sp500_tickers() -> List[str]:
    """Lit la liste des tickers du S&P500 via la page Wikipedia (simple et efficace)."""
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


# ======================
# Construction dataset
# ======================
def generate_dataset() -> Tuple[np.ndarray, np.ndarray]:
    """
    Construit le dataset (X, y) pour l'entraînement Double Top.

    Sorties
    -------
    X : np.ndarray, shape = (n_samples, TOTAL_WINDOW, n_features)
        Chaque échantillon est une fenêtre temporelle séquentielle d'indicateurs.
    y : np.ndarray, shape = (n_samples,)
        1 pour fenêtre "Double Top" (positive), 0 sinon.

    Étapes (par ticker) — explications pour débutant :
      1) Télécharger les prix & volumes jusqu’à 'end_str' (aujourd’hui).
      2) Calculer les indicateurs techniques (features par jour).
      3) Générer des paires de pics (auto + manuelles) puis
         NE GARDER que celles qui passent le filtre chartiste `is_double_top`.
      4) Créer les fenêtres POSITIVES autour du 1er sommet (i1) avec un petit
         décalage (-3..+3) pour la robustesse.
      5) Tirer des fenêtres NÉGATIVES aléatoirement en évitant toute zone
         qui chevauche une fenêtre positive (réduit la fuite de label).
    """
    # Import ici pour éviter les import cycles si vous réutilisez pipeline_utils ailleurs
    from pipeline.pipeline_double_top.pipeline_utils import is_double_top

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

        # 1) Données brutes
        prices, vols = fetch_data(tk, end_date=end_str)
        if prices is None:
            LOGGER.warning("ticker_fetch_failed", ticker=tk)
            continue

        # 2) Indicateurs techniques (features par jour)
        try:
            ind_df = compute_all_indicators(prices, vols)  # DataFrame (time, n_features)
            n_features = ind_df.shape[1]
            LOGGER.debug("indicators_ok", ticker=tk, hint=f"shape={ind_df.shape}")
        except Exception as e:
            LOGGER.error("indicators_failed", ticker=tk, error=str(e))
            continue

        # 3) Détection des doubles sommets (pics) -> PAIRES (i1, i2) VALIDÉES
        peaks: List[Tuple[int, int]] = []
        try:
            # a) pics automatiques -> indices
            auto_idx: List[int] = []
            if ENABLE_AUTO_PEAKS:
                auto_dates = detect_peaks(prices)  # retourne des dates
                auto_idx = [prices.index.get_loc(d) for d in auto_dates]

            # b) paires candidates (écart max = LOOKBETWEEN)
            cand_pairs: List[Tuple[int, int]] = []
            for i1 in auto_idx:
                for i2 in auto_idx:
                    if 0 < (i2 - i1) <= LOOKBETWEEN:
                        cand_pairs.append((i1, i2))

            # c) paires manuelles depuis la config (converties en indices)
            for d1, d2 in MANUAL_PEAKS.get(tk, []):
                try:
                    idx1 = prices.index.get_loc(pd.to_datetime(d1))
                    idx2 = prices.index.get_loc(pd.to_datetime(d2))
                    if 0 < (idx2 - idx1) <= LOOKBETWEEN:
                        cand_pairs.append((idx1, idx2))
                except (KeyError, ValueError):
                    continue

            # d) filtrage chartiste/tempo
            for (i1, i2) in cand_pairs:
                if is_double_top(
                    prices, i1, i2,
                    tolerance_pct=0.02,       # sommets ~égaux à ±2%
                    min_valley_drop_pct=0.04, # creux >= 4% sous les sommets
                    min_separation=3,         # au moins 3 jours entre sommets
                    max_separation=LOOKBETWEEN,
                    min_up_pct=0.05,          # au moins +5% de hausse avant 1er sommet
                    confirm_break=False       # passez à True si vous voulez une classe + stricte
                ):
                    peaks.append((i1, i2))
        except Exception as e:
            LOGGER.error("peaks_failed", ticker=tk, error=str(e))

        # 4) Fenêtres POSITIVES (autour du 1er sommet i1, avec petits décalages)
        pos_windows: List[np.ndarray] = []
        pos_spans: List[Tuple[int, int]] = []

        # ✅ Choix de centrage : "i2" (recommandé) ou "i1"
        CENTER_ON = "i2"  # options: "i2" (par défaut), "i1"

        for i1, i2 in peaks:
            # On définit un "ancrage" (anchor) selon le choix
            anchor = i2 if CENTER_ON == "i2" else i1

            # Data augmentation légère: on translate la fenêtre autour de l’ancrage
            for shift in range(-3, 4):
                # ------------------------------------------------------------
                # Fenêtre = [anchor - LOOKBACK + shift, ..., + (TOTAL_WINDOW-1)]
                # -> on ne dépasse pas les bornes (0 ... len(ind_df))
                # ------------------------------------------------------------
                start = anchor - LOOKBACK + shift
                end = start + TOTAL_WINDOW  # end exclus
                if 0 <= start and end <= len(ind_df):
                    # ⚠️ Débutant: ind_df est un DataFrame d’indicateurs par jour;
                    # on prend un "cube" (temps, features) que l’on donne au modèle.
                    window = ind_df.iloc[start:end].values  # shape: (TOTAL_WINDOW, n_features)
                    pos_windows.append(window)
                    pos_spans.append((start, end))  # on mémorise la zone pour éviter les collisions avec les négatives

        # 5) Fenêtres NÉGATIVES (tirage aléatoire) SANS chevaucher les positives
        neg_windows: List[np.ndarray] = []
        # Tous les starts possibles pour une fenêtre complète
        all_starts = np.arange(0, max(len(ind_df) - TOTAL_WINDOW + 1, 0), dtype=int)

        if len(pos_spans) > 0:
            # Construit un masque booléen des starts qui tombent DANS une zone positive
            forbidden = np.zeros_like(all_starts, dtype=bool)
            for (s, e) in pos_spans:
                # Tous les starts tels que [start, start+TOTAL_WINDOW) chevauchent [s, e)
                # Chevauchement si: start < e and start+TOTAL_WINDOW > s
                # => start ∈ [s - TOTAL_WINDOW + 1, e - 1]
                low = max(0, s - TOTAL_WINDOW + 1)
                high = min(len(ind_df) - TOTAL_WINDOW, e - 1)
                if low <= high:
                    forbidden[(all_starts >= low) & (all_starts <= high)] = True
            candidate_starts = all_starts[~forbidden]
        else:
            candidate_starts = all_starts

        # Nombre de négatifs à tirer : proportionnel au nb de positifs (ou valeur min NEG_RATIO)
        neg_target = int(max(len(pos_windows) * NEG_RATIO, NEG_RATIO))
        if len(candidate_starts) > 0 and neg_target > 0:
            draw = min(neg_target, len(candidate_starts))
            choices = np.random.choice(candidate_starts, size=draw, replace=False)
            for st in choices:
                neg_windows.append(ind_df.iloc[st: st + TOTAL_WINDOW].values)

        # 6) Alimente X/y
        X.extend(pos_windows)
        y.extend([1] * len(pos_windows))
        X.extend(neg_windows)
        y.extend([0] * len(neg_windows))

        LOGGER.info(
            "ticker_windows",
            ticker=tk,
            positives=len(pos_windows),
            negatives=len(neg_windows),
            n_features=int(n_features),
            hint=f"elapsed={time.time()-t0:.2f}s",
        )

    # 7) Packaging final
    X_arr = np.array(X, dtype=float)   # (n_samples, TOTAL_WINDOW, n_features)
    y_arr = np.array(y, dtype=int)     # (n_samples,)
    LOGGER.info(
        "dataset_generation_end",
        samples=int(len(X_arr)),
        positives=int(y_arr.sum() if len(y_arr) else 0),
        negatives=int(len(y_arr) - y_arr.sum() if len(y_arr) else 0),
        window=int(TOTAL_WINDOW),
    )
    return X_arr, y_arr

# ======================
# Walk-forward CV simple
# ======================
def get_walk_forward_splits(n_splits: int, initial_window: int, test_window: int, gap: int = 0):
    """Génère des splits successifs train->test pour séries temporelles."""
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


# ======================
# Optuna objective (LightGBM brut, sur walk-forward)
# ======================
def objective(trial, X: np.ndarray, y: np.ndarray) -> float:
    """
    Objectif pour Optuna : optimise quelques hyperparams LightGBM
    sur un schéma walk-forward (utile quand pas de split aléatoire).
    """
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
        # Pour LightGBM (tabulaire), on aplatit (window * features)
        pipe = ImbPipeline([
            ("adasyn", ADASYN()),
            ("scaler", StandardScaler()),
            ("clf", lgb.LGBMClassifier(**params)),
        ])
        X_tr = X[tr].reshape(len(tr), -1)
        X_te = X[te].reshape(len(te), -1)
        try:
            pipe.fit(X_tr, y[tr])
        except Exception:
            # fallback sans ADASYN si souci de dimension
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


# ======================
# Entraînement + sauvegarde (GBM + CNN)
# ======================
def train_and_save(
    add_trees_on_resume: int = 200,   # nb d’arbres LightGBM à ajouter en reprise (warm start)
    allow_resume: bool = True,        # autoriser la reprise si un modèle existe déjà
    n_trials_optuna: int = 20,        # nb d’essais Optuna si on repart de zéro
    train_cnn: bool = True,           # active l'entraînement CNN si possible
    cnn_epochs: int = 12,             # époques CNN (valeur modeste par défaut)
    cnn_batch_size: int = 64,         # batch size CNN
    cnn_validation_split: float = 0.2 # split de validation interne pour Keras
) -> None:
    """
    Entraîne :
      1) Un pipeline tabulaire (ADASYN -> StandardScaler -> LGBMClassifier).
         - Reprise "warm start" si un modèle existe déjà (on augmente n_estimators).
      2) Un modèle CNN séquentiel (si build_cnn + TensorFlow/Keras disponibles).

    Sauvegarde :
      - GBM_PERSIST_FILE (.joblib)
      - CNN_PERSIST_FILE (.keras)
    """
    t0 = time.time()
    LOGGER.info("train_start", gbm_path=str(GBM_PERSIST_FILE), cnn_path=str(CNN_PERSIST_FILE))

    # (optionnel mais recommandé pour diagnostics)
    log_environment()

    # ---------------------------------------------------------------------
    # 0) Récupération du dataset (X 3D, y 1D)
    # ---------------------------------------------------------------------
    X_source, y_source = generate_dataset()
    LOGGER.info("dataset_loaded_in_train", n_samples=int(len(X_source)))

    if len(X_source) == 0:
        raise RuntimeError("Dataset vide : impossible d'entraîner.")

    # LightGBM (API sklearn) attend X en 2D ; le CNN travaillera en 3D.
    if X_source.ndim != 3:
        raise ValueError(f"X doit être 3D (n_samples, window, features). Reçu: {X_source.shape}")
    n_samples, timesteps, n_features = X_source.shape

    # Aplatir pour la branche LightGBM
    X_flat = X_source.reshape(n_samples, -1)

    if len(X_flat) != len(y_source):
        raise ValueError(f"X et y doivent avoir la même longueur: {len(X_flat)} vs {len(y_source)}")

    # Split train/test (même split pour GBM et CNN pour comparabilité)
    X_train_flat, X_test_flat, y_train, y_test = train_test_split(
        X_flat, y_source, test_size=0.2, random_state=42, stratify=y_source
    )
    # Pour le CNN: on reprend les mêmes indices en reshapant
    # Astuce: recréer les matrices 3D à partir des shapes connues
    idx_train = len(y_train)
    # Pour aligner proprement, on re-split X_source identiquement
    X_train_3d, X_test_3d, _, _ = train_test_split(
        X_source, y_source, test_size=0.2, random_state=42, stratify=y_source
    )

    LOGGER.info("split_done", n_train=int(len(X_train_flat)), n_test=int(len(X_test_flat)))

    # ---------------------------------------------------------------------
    # 1) MLflow : réduire le bruit et cadrer l'autolog
    # ---------------------------------------------------------------------
    try:
        mlflow.autolog(disable=True)  # on reprend la main
        mlflow.lightgbm.autolog(
            log_models=False,
            exclusive=True,
            log_input_examples=False,
            log_post_training_metrics=True,
        )
        mlflow.sklearn.autolog(
            log_models=False,
            exclusive=False,
            log_input_examples=False,
            log_datasets=False,
        )
        logging.getLogger("mlflow").setLevel(logging.WARNING)
        logging.getLogger("urllib3").setLevel(logging.WARNING)
        LOGGER.info("mlflow_autolog_configured")
    except Exception as e:
        LOGGER.warning("mlflow_autolog_config_failed", error=str(e))

    # Sécurité : s’assurer que le dossier Models existe
    Path(os.path.dirname(GBM_PERSIST_FILE)).mkdir(parents=True, exist_ok=True)
    Path(os.path.dirname(CNN_PERSIST_FILE)).mkdir(parents=True, exist_ok=True)

    # ---------------------------------------------------------------------
    # 2) Reprise GBM (warm start) si possible
    # ---------------------------------------------------------------------
    resumed_ok = False
    if allow_resume and Path(GBM_PERSIST_FILE).exists():
        try:
            pipeline_existing = joblib.load(GBM_PERSIST_FILE)
            LOGGER.info("resume_found_model", path=str(GBM_PERSIST_FILE))

            # Vérifie la présence du step "classifier" de type LGBMClassifier
            if not hasattr(pipeline_existing, "named_steps") or "classifier" not in pipeline_existing.named_steps:
                raise RuntimeError("Pipeline incompatible (pas de step 'classifier').")
            clf = pipeline_existing.named_steps["classifier"]
            if not isinstance(clf, LGBMClassifier):
                raise RuntimeError("Le step 'classifier' n'est pas un LGBMClassifier.")

            # Warm start = on augmente le nombre d’arbres et on re-fit
            current_estimators = getattr(clf, "n_estimators", 300)
            new_estimators = int(current_estimators) + int(add_trees_on_resume)
            clf.set_params(warm_start=True, n_estimators=new_estimators)

            t_fit = time.time()
            pipeline_existing.fit(X_train_flat, y_train)
            fit_time = time.time() - t_fit

            # Évaluation
            y_proba = pipeline_existing.predict_proba(X_test_flat)[:, 1]
            y_pred = (y_proba >= 0.5).astype(int)
            test_roc = float(roc_auc_score(y_test, y_proba))
            test_f1 = float(f1_score(y_test, y_pred))
            test_prec = float(precision_score(y_test, y_pred))
            test_rec = float(recall_score(y_test, y_pred))

            LOGGER.info(
                "resume_metrics",
                added_trees=int(add_trees_on_resume),
                new_n_estimators=int(new_estimators),
                roc_auc=test_roc, f1=test_f1, precision=test_prec, recall=test_rec,
                fit_time_sec=round(fit_time, 3)
            )

            # Sauvegarde mise à jour
            joblib.dump(pipeline_existing, GBM_PERSIST_FILE)
            LOGGER.info("gbm_model_saved", path=str(GBM_PERSIST_FILE))

            # MLflow (modèle repris)
            try:
                with mlflow.start_run(run_name="double_top_resumed"):
                    mlflow.log_param("resume_added_trees", int(add_trees_on_resume))
                    mlflow.log_param("new_n_estimators", int(new_estimators))

                    X_example = X_flat[:100] if len(X_flat) > 100 else X_flat
                    try:
                        y_proba_example = pipeline_existing.predict_proba(X_example)[:, 1]
                        signature = infer_signature(X_example, y_proba_example)
                    except Exception:
                        y_pred_example = pipeline_existing.predict(X_example)
                        signature = infer_signature(X_example, y_pred_example)

                    mlflow.sklearn.log_model(
                        sk_model=pipeline_existing,
                        artifact_path="model",
                        signature=signature,
                        input_example=X_example,
                        registered_model_name=None
                    )
                    mlflow.log_metric("test_roc_auc", test_roc)
                    mlflow.log_metric("test_f1", test_f1)
                    mlflow.log_metric("test_precision", test_prec)
                    mlflow.log_metric("test_recall", test_rec)

                    LOGGER.info("mlflow_final_model_logged", mode="resume")
            except Exception as e:
                LOGGER.warning("mlflow_final_model_log_failed", error=str(e), mode="resume")

            resumed_ok = True
        except Exception as e:
            LOGGER.warning("resume_failed_fallback_to_fresh_train", error=str(e))

    # ---------------------------------------------------------------------
    # 3) Entraînement GBM from scratch (si pas de reprise ou reprise KO)
    # ---------------------------------------------------------------------
    if not resumed_ok:
        base_pipeline = ImbPipeline(steps=[
            ("adasyn", ADASYN(random_state=42)),
            ("scaler", StandardScaler(with_mean=True, with_std=True)),
            ("classifier", LGBMClassifier(
                objective="binary",
                boosting_type="gbdt",
                n_estimators=300,
                random_state=42,
                n_jobs=-1
            )),
        ])
        LOGGER.info("pipeline_defined_fresh")

        # CV stratifiée (3 folds) sur TRAIN
        cv = StratifiedKFold(n_splits=3, shuffle=True, random_state=42)

        def _optuna_objective(trial: optuna.trial.Trial) -> float:
            """Objective Optuna: ajuste les hyperparamètres du step 'classifier'."""
            params = {
                "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.2, log=True),
                "num_leaves": trial.suggest_int("num_leaves", 15, 255),
                "min_child_samples": trial.suggest_int("min_child_samples", 5, 80),
                "subsample": trial.suggest_float("subsample", 0.6, 1.0),
                "colsample_bytree": trial.suggest_float("colsample_bytree", 0.6, 1.0),
                "reg_alpha": trial.suggest_float("reg_alpha", 1e-8, 1.0, log=True),
                "reg_lambda": trial.suggest_float("reg_lambda", 1e-8, 1.0, log=True),
            }
            # IMPORTANT : préfixer par "classifier__" pour cibler le bon step
            prefixed = {f"classifier__{k}": v for k, v in params.items()}

            # Clone du pipeline base pour isoler chaque essai
            pipe = joblib.loads(joblib.dumps(base_pipeline))
            pipe.set_params(**prefixed)

            try:
                scores = cross_val_score(
                    pipe, X_train_flat, y_train, scoring="roc_auc", cv=cv, n_jobs=-1
                )
                mean_score = float(np.mean(scores))
                LOGGER.info("optuna_trial",
                            roc_auc_cv=mean_score,
                            **{k: (float(v) if isinstance(v, float) else v) for k, v in params.items()})
                return mean_score
            except Exception as e:
                LOGGER.warning("optuna_trial_failed", error=str(e))
                return 0.0

        LOGGER.info("optuna_study_start")
        study = optuna.create_study(direction="maximize")
        study.optimize(_optuna_objective, n_trials=int(n_trials_optuna), show_progress_bar=False)
        LOGGER.info("optuna_study_done", best_value=float(study.best_value))

        best_params = study.best_params
        LOGGER.info("optuna_best_params", **best_params)

        # Pipeline final avec meilleurs hyperparams (→ clés préfixées)
        final_pipeline = joblib.loads(joblib.dumps(base_pipeline))
        final_pipeline.set_params(**{f"classifier__{k}": v for k, v in best_params.items()})

        # Fit final sur tout TRAIN
        t_fit = time.time()
        final_pipeline.fit(X_train_flat, y_train)
        fit_time = time.time() - t_fit

        # Évaluation sur TEST
        y_proba = final_pipeline.predict_proba(X_test_flat)[:, 1]
        y_pred = (y_proba >= 0.5).astype(int)
        test_roc = float(roc_auc_score(y_test, y_proba))
        test_f1 = float(f1_score(y_test, y_pred))
        test_prec = float(precision_score(y_test, y_pred))
        test_rec = float(recall_score(y_test, y_pred))
        LOGGER.info("test_metrics_fresh",
                    roc_auc=test_roc, f1=test_f1, precision=test_prec, recall=test_rec,
                    fit_time_sec=round(fit_time, 3))

        # Sauvegarde du modèle final GBM
        joblib.dump(final_pipeline, GBM_PERSIST_FILE)
        LOGGER.info("gbm_model_saved", path=str(GBM_PERSIST_FILE))

        # MLflow : run unique pour le modèle final (fresh)
        try:
            with mlflow.start_run(run_name="double_top_final_gbm"):
                mlflow.log_params(best_params)
                X_example = X_flat[:100] if len(X_flat) > 100 else X_flat
                try:
                    y_proba_example = final_pipeline.predict_proba(X_example)[:, 1]
                    signature = infer_signature(X_example, y_proba_example)
                except Exception:
                    y_pred_example = final_pipeline.predict(X_example)
                    signature = infer_signature(X_example, y_pred_example)

                mlflow.sklearn.log_model(
                    sk_model=final_pipeline,
                    artifact_path="model",
                    signature=signature,
                    input_example=X_example,
                    registered_model_name=None
                )
                mlflow.log_metric("test_roc_auc", test_roc)
                mlflow.log_metric("test_f1", test_f1)
                mlflow.log_metric("test_precision", test_prec)
                mlflow.log_metric("test_recall", test_rec)
                LOGGER.info("mlflow_final_model_logged", mode="fresh_gbm")
        except Exception as e:
            LOGGER.warning("mlflow_final_model_log_failed", error=str(e), mode="fresh_gbm")

    # ---------------------------------------------------------------------
    # 4) Entraînement CNN (optionnel, si build_cnn et TF dispos)
    # ---------------------------------------------------------------------
    if train_cnn:
        if _build_cnn is None:
            LOGGER.warning("cnn_skipped", reason="build_cnn_not_found")
        else:
            tf, tf_err = try_import_tf()
            if tf is None:
                LOGGER.warning("cnn_skipped", reason="tensorflow_import_failed", error=str(tf_err))
            else:
                # Préparation des tenseurs pour Keras:
                # Keras accepte (batch, timesteps, features); éventuel canal supplémentaire si besoin.
                X_train_cnn = X_train_3d.astype("float32")
                X_test_cnn = X_test_3d.astype("float32")
                y_train_cnn = y_train.astype("float32")
                y_test_cnn = y_test.astype("float32")

                # Construction du modèle via ton builder projet
                # Convention proposée: build_cnn(input_shape=(timesteps, n_features)) -> tf.keras.Model compilé
                try:
                    model = _build_cnn(input_shape=(timesteps, n_features))
                except TypeError:
                    # Fallback si ta signature demande autre chose (ex: units, dropout...)
                    model = _build_cnn((timesteps, n_features))  # type: ignore

                LOGGER.info("cnn_model_built", params=sum([tf.keras.backend.count_params(w) for w in model.trainable_weights]))

                # Callback simples: EarlyStopping + ModelCheckpoint vers fichier final
                es = tf.keras.callbacks.EarlyStopping(monitor="val_auc" if "auc" in [m.name for m in model.metrics] else "val_loss",
                                                      mode="max" if "auc" in [m.name for m in model.metrics] else "min",
                                                      patience=3, restore_best_weights=True)
                ckpt_path = str(Path(CNN_PERSIST_FILE).with_suffix(".tmp.keras"))
                mc = tf.keras.callbacks.ModelCheckpoint(ckpt_path, monitor="val_loss", save_best_only=True, verbose=0)

                # MLflow autolog pour TensorFlow (protégé)
                try:
                    mlflow.tensorflow.autolog(log_models=False)
                except Exception:
                    pass  # pas bloquant

                # Entraînement
                t_fit_cnn = time.time()
                history = model.fit(
                    X_train_cnn,
                    y_train_cnn,
                    epochs=int(cnn_epochs),
                    batch_size=int(cnn_batch_size),
                    validation_split=float(cnn_validation_split),
                    verbose=1
                )
                fit_time_cnn = time.time() - t_fit_cnn

                # Évaluation simple sur TEST
                try:
                    # Si le modèle a une méthode evaluate :
                    eval_res = model.evaluate(X_test_cnn, y_test_cnn, verbose=0)
                    # eval_res peut être une liste -> on tente de retrouver AUC si dispo
                    metrics_names = getattr(model, "metrics_names", None) or []
                    metrics_dict = {}
                    if isinstance(eval_res, (list, tuple)) and metrics_names:
                        for k, v in zip(metrics_names, eval_res):
                            metrics_dict[k] = float(v)
                    elif isinstance(eval_res, (int, float)):
                        metrics_dict["loss"] = float(eval_res)
                    else:
                        metrics_dict["evaluate"] = float(eval_res) if eval_res is not None else -1.0
                except Exception as e:
                    LOGGER.warning("cnn_evaluate_failed", error=str(e))
                    # Fallback: métriques via prédictions proba -> ROC AUC + F1
                    y_proba_cnn = np.asarray(model.predict(X_test_cnn, verbose=0)).reshape(-1)
                    y_pred_cnn = (y_proba_cnn >= 0.5).astype(int)
                    metrics_dict = {
                        "roc_auc": float(roc_auc_score(y_test_cnn, y_proba_cnn)),
                        "f1": float(f1_score(y_test_cnn, y_pred_cnn)),
                        "precision": float(precision_score(y_test_cnn, y_pred_cnn)),
                        "recall": float(recall_score(y_test_cnn, y_pred_cnn)),
                    }

                LOGGER.info("cnn_test_metrics", **{k: (float(v) if isinstance(v, (int, float, np.floating)) else v)
                                                   for k, v in metrics_dict.items()},
                            fit_time_sec=round(fit_time_cnn, 3))

                # Sauvegarde du meilleur modèle (checkpoint) -> chemin final
                try:
                    # Si un checkpoint a été écrit, on le déplace; sinon on sauve le modèle courant
                    if Path(ckpt_path).exists():
                        Path(Path(CNN_PERSIST_FILE).parent).mkdir(parents=True, exist_ok=True)
                        # Remplace le fichier final
                        if Path(CNN_PERSIST_FILE).exists():
                            os.remove(CNN_PERSIST_FILE)
                        os.replace(ckpt_path, CNN_PERSIST_FILE)
                    else:
                        model.save(CNN_PERSIST_FILE)
                    LOGGER.info("cnn_model_saved", path=str(CNN_PERSIST_FILE))
                except Exception as e:
                    LOGGER.warning("cnn_save_failed", error=str(e))

                # MLflow logging du modèle CNN (sans forcer autolog)
                try:
                    with mlflow.start_run(run_name="double_top_final_cnn"):
                        # Quelques hyperparams utiles à tracer
                        mlflow.log_param("cnn_epochs", int(cnn_epochs))
                        mlflow.log_param("cnn_batch_size", int(cnn_batch_size))
                        mlflow.log_param("cnn_validation_split", float(cnn_validation_split))

                        # Signature d'entrée (ex: 2D aplatir un batch d'exemple)
                        X_example_cnn = X_train_cnn[:100] if len(X_train_cnn) > 100 else X_train_cnn
                        y_proba_example = np.asarray(model.predict(X_example_cnn, verbose=0)).reshape(-1)
                        # On donne comme "features" une version aplatie pour la signature (mlflow aime bien 2D)
                        X_example_flat = X_example_cnn.reshape(len(X_example_cnn), -1)
                        signature = infer_signature(X_example_flat, y_proba_example)

                        # On log le fichier sauvegardé plutôt que de re-sérialiser via mlflow.tf pour éviter les surprises Keras3
                        mlflow.log_artifact(CNN_PERSIST_FILE, artifact_path="cnn_model")
                        for k, v in metrics_dict.items():
                            try:
                                mlflow.log_metric(f"cnn_{k}", float(v))
                            except Exception:
                                pass
                        LOGGER.info("mlflow_final_cnn_logged")
                except Exception as e:
                    LOGGER.warning("mlflow_final_cnn_log_failed", error=str(e))

    LOGGER.info("train_done", elapsed_sec=round(time.time() - t0, 3))


# ======================
# MAIN
# ======================
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
    # Exécution directe :
    #   python -m pipeline.pipeline_double_top.analyze_double_top
    # ou simplement :
    #   python analyze_double_top.py
    main()
