from __future__ import annotations

from fastapi.testclient import TestClient

from finance_ia.runtime.api import app


def test_runtime_health_endpoint() -> None:
    client = TestClient(app)
    response = client.get("/health")

    assert response.status_code == 200
    payload = response.json()
    assert payload["status"] == "ok"
    assert "DOUBLE_TOP" in payload["patterns"]
