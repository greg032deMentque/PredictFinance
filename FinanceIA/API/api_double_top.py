# api_double_top.py
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import joblib
import yfinance as yf
from pipeline_utils import compute_all_indicators

class Request(BaseModel):
    ticker: str

class Response(BaseModel):
    ticker: str
    mean_prob: float
    max_prob: float
    median_prob: float

app = FastAPI()
model = joblib.load("stacked_model_latest.pkl")  # scaler + modèle pipeline

@app.post("/predict_double_top", response_model=Response)
def predict(req: Request):
    # 1) Récupérer les données récentes
    df = yf.Ticker(req.ticker).history(period="6mo")
    if df.empty:
        raise HTTPException(404, "Ticker introuvable")
    prices, vols = df['Close'], df['Volume']
    # 2) Calcul des features
    ind = compute_all_indicators(prices.values, vols.values)
    windows = [ind.iloc[i - LOOKBACK : i - LOOKBACK + TOTAL_WINDOW].values.flatten()
               for i in range(LOOKBACK, len(prices) - TOTAL_WINDOW + 1)]
    # 3) Prédiction
    probs = model.predict_proba(windows)[:,1]
    return Response(
        ticker=req.ticker,
        mean_prob=float(probs.mean()),
        max_prob=float(probs.max()),
        median_prob=float(np.median(probs))
    )
