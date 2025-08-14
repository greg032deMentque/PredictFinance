# training.py
import os
import joblib
import structlog
import numpy as np
from sklearn.model_selection import StratifiedKFold, RandomizedSearchCV, KFold
from sklearn.metrics import classification_report, confusion_matrix, mean_squared_error, r2_score, f1_score
from sklearn.preprocessing import LabelEncoder, StandardScaler
from sklearn.ensemble import RandomForestClassifier
from xgboost import XGBClassifier, XGBRegressor
from sklearn.neural_network import MLPClassifier
from imblearn.pipeline import Pipeline as ImbPipeline
from imblearn.over_sampling import SMOTE
from pipeline.pipeline_config import RESULTS_DIR

log = structlog.get_logger("myapp.training")


def train_classification(X_train, X_test, y_train, y_test):
    """
    Entraîne et évalue plusieurs classifieurs sur des données déséquilibrées.
    - Pipeline : SMOTE (k_neighbors=1) + StandardScaler + Classifieur
    - CV : StratifiedKFold adapté à la ou les classes les plus rares
    - Recherche aléatoire (RandomizedSearchCV) avec scoring F1 macro
    - Sauvegarde des modèles et des métriques
    """
    log.info("Starting classification training")

    # 1) Encodage des labels string en entiers
    le = LabelEncoder()
    y_train_enc = le.fit_transform(y_train)

    # 2) Configuration de la CV : nombre de splits basé sur la classe la moins représentée
    counts = np.bincount(y_train_enc)
    min_count = counts.min()
    n_splits = max(2, min(5, min_count))
    cv = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=42)
    log.debug("CV config", splits=n_splits, class_counts=dict(enumerate(counts)))

    # 3) Pipeline commun
    smote = SMOTE(random_state=42, k_neighbors=1)  # k_neighbors=1 évite erreurs sur classes très rares
    scaler = StandardScaler()  # normalisation nécessaire pour MLP

    classifiers = {}
    results = {}

    # 4a) XGBoost
    xgb = XGBClassifier(
        random_state=42,
        eval_metric="mlogloss",  # utilisé pour multiclass log-loss
        verbosity=0,               # supprime warnings XGBoost
        n_jobs=-1
    )
    pipe_xgb = ImbPipeline([('smote', smote), ('scale', scaler), ('clf', xgb)])
    param_xgb = {
        'clf__n_estimators': [100, 200, 300],  # plus d'arbres augmente la capacité
        'clf__max_depth': [3, 6, 9],           # profondeur contrôlant complexité
        'clf__learning_rate': [0.01, 0.1],     # pas d'apprentissage: petit pour fine-tuning
        'clf__subsample': [0.7, 1.0],          # fraction d'échantillons pour réduire corrélation
        'clf__colsample_bytree': [0.7, 1.0]     # fraction de features pour chaque arbre
    }
    search_xgb = RandomizedSearchCV(
        pipe_xgb, param_distributions=param_xgb,
        n_iter=15, cv=cv, scoring='f1_macro', n_jobs=-1, random_state=42
    )
    search_xgb.fit(X_train, y_train_enc)
    best_xgb = search_xgb.best_estimator_
    classifiers['xgb'] = best_xgb
    log.info("XGB best params", **search_xgb.best_params_)

    # 4b) RandomForest baseline
    rf = RandomForestClassifier(random_state=42, n_jobs=-1)
    pipe_rf = ImbPipeline([('smote', smote), ('scale', scaler), ('clf', rf)])
    param_rf = {
        'clf__n_estimators': [100, 200],  # nombre d'arbres
        'clf__max_depth': [None, 10, 20]  # profondeur max
    }
    search_rf = RandomizedSearchCV(pipe_rf, param_distributions=param_rf,
                                   n_iter=6, cv=cv, scoring='f1_macro', n_jobs=-1, random_state=42)
    search_rf.fit(X_train, y_train_enc)
    best_rf = search_rf.best_estimator_
    classifiers['rf'] = best_rf
    log.info("RF best params", **search_rf.best_params_)

    # 4c) MLPClassifier
    mlp = MLPClassifier(
        random_state=42,
        max_iter=1000,             # augmenter itérations pour converger
        early_stopping=True,       # arrête si pas d'amélioration
        validation_fraction=0.1,   # jeu de validation pour early stopping
        n_iter_no_change=20,       # patience
        tol=1e-4                   # seuil de tolérance
    )
    pipe_mlp = ImbPipeline([('smote', smote), ('scale', scaler), ('clf', mlp)])
    param_mlp = {
        'clf__hidden_layer_sizes': [(50,), (100,), (50,50)],
        'clf__alpha': [1e-4, 1e-3],             # régularisation L2
        'clf__learning_rate_init': [1e-3, 1e-2]  # pas d'apprentissage
    }
    search_mlp = RandomizedSearchCV(pipe_mlp, param_distributions=param_mlp,
                                    n_iter=10, cv=cv, scoring='f1_macro', n_jobs=-1, random_state=42)
    search_mlp.fit(X_train, y_train_enc)
    best_mlp = search_mlp.best_estimator_
    classifiers['mlp'] = best_mlp
    log.info("MLP best params", **search_mlp.best_params_)

    # 5) Évaluation finale
    for name, model in classifiers.items():
        y_pred_enc = model.predict(X_test)
        y_pred = le.inverse_transform(y_pred_enc)
        results[name] = {'f1_macro': f1_score(y_test, y_pred, average='macro')}
        print(f"=== {name} Classification ===")
        print(classification_report(y_test, y_pred))
        print(confusion_matrix(y_test, y_pred))

    # 6) Sauvegarde
    os.makedirs(RESULTS_DIR, exist_ok=True)
    joblib.dump({'classifiers': classifiers, 'label_encoder': le},
                os.path.join(RESULTS_DIR, "classifiers.pkl"))
    log.info("Saved classification models and encoder")

    return classifiers, results


def train_regression(X_train, X_test, y_train, y_test):
    """
    Entraîne et évalue le modèle de régression XGBoost.
    - Pipeline : StandardScaler + XGBRegressor
    - Recherche aléatoire (RandomizedSearchCV)
    - Sauvegarde du modèle
    """
    log.info("Starting regression training")

    scaler = StandardScaler()
    xgb_r = XGBRegressor(random_state=42, n_jobs=-1, verbosity=0)
    pipe_r = ImbPipeline([('scale', scaler), ('reg', xgb_r)])
    param_r = {
        'reg__n_estimators': [100, 200],   # plus d'arbres réduit biais
        'reg__max_depth': [3, 6],          # complexité du modèle
        'reg__learning_rate': [0.01, 0.1]  # équilibre vitesse/convergence
    }
    search_r = RandomizedSearchCV(pipe_r, param_distributions=param_r,
                                  n_iter=8, cv=KFold(n_splits=5, shuffle=True, random_state=42),
                                  scoring='neg_mean_squared_error', n_jobs=-1, random_state=42)
    search_r.fit(X_train, y_train)
    best_r = search_r.best_estimator_
    log.info("XGBReg best params", **search_r.best_params_)

    # Évaluation
    y_pred = best_r.predict(X_test)
    print("=== Regression XGB ===")
    print("MSE:", mean_squared_error(y_test, y_pred))
    print("R2 :", r2_score(y_test, y_pred))

    # Sauvegarde
    os.makedirs(RESULTS_DIR, exist_ok=True)
    joblib.dump({'regressor': best_r}, os.path.join(RESULTS_DIR, "regressors.pkl"))
    log.info("Saved regression model")

    return best_r
