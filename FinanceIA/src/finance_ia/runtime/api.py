from __future__ import annotations

import argparse
from pathlib import Path

import uvicorn
from fastapi import FastAPI
from pydantic import BaseModel, Field

from finance_ia.model.predict import predict_ticker
from finance_ia.model.simulate import simulate_ticker
from finance_ia.patterns import list_supported_patterns

app = FastAPI(title="PredictFinance IA Runtime", version="1.0.0")


class PredictRequest(BaseModel):
    symbol: str = Field(min_length=1)
    pattern: str = "DOUBLE_TOP"
    model_dir: str = "artifacts/double_top"
    period: str = "6mo"


class SimulateRequest(PredictRequest):
    investment_amount: float = Field(gt=0)
    horizon_days: int = Field(ge=1, le=365)


@app.get("/health")
def health() -> dict[str, object]:
    return {
        "status": "ok",
        "patterns": list_supported_patterns(),
    }


@app.post("/predict")
def predict(request: PredictRequest) -> dict[str, object]:
    result = predict_ticker(
        ticker=request.symbol,
        model_dir=Path(request.model_dir),
        period=request.period,
        pattern=request.pattern,
    )
    return result.to_dict()


@app.post("/simulate")
def simulate(request: SimulateRequest) -> dict[str, object]:
    result = simulate_ticker(
        ticker=request.symbol,
        model_dir=Path(request.model_dir),
        period=request.period,
        pattern=request.pattern,
        investment_amount=request.investment_amount,
        horizon_days=request.horizon_days,
    )
    return result.to_dict()


def run(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Run Finance IA HTTP runtime")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--reload", action="store_true")
    args = parser.parse_args(argv)

    uvicorn.run("finance_ia.runtime.api:app", host=args.host, port=args.port, reload=args.reload)
    return 0
