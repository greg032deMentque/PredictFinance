# api.py
from fastapi import FastAPI, HTTPException
import joblib, numpy as np, json

MODEL_PATH = "stacker.pkl"
METRICS_PATH = "metrics.json"
FEATURE_DIM = 128  # adapter à votre taille de features

"""
Si vous voulez transformer ça en micro‑service pour monitorer santé et métriques.
"""

app = FastAPI()

@app.on_event("startup")
def load_model():
    try:
        app.state.model = joblib.load(MODEL_PATH)
    except Exception as e:
        raise RuntimeError(f"Impossible de charger le modèle : {e}")

@app.get("/health")
def health_check():
    try:
        # test rapide avec un vecteur nul
        app.state.model.predict_proba(np.zeros((1, FEATURE_DIM)))
        return {"status": "ok"}
    except Exception:
        raise HTTPException(500, "Modèle indisponible")

@app.get("/metrics")
def metrics():
    with open(METRICS_PATH) as f:
        return json.load(f)
