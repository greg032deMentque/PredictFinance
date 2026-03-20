from __future__ import annotations

import json
import logging
import sys
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from logging_config import setup_logging

LOGGER = logging.getLogger(__name__)


@dataclass(slots=True)
class PythonCliErrorEnvelope:
    schema_version: str
    source: str
    operation: str
    error_code: str
    error_type: str
    message: str
    user_message: str
    ticker: str | None
    pattern: str | None
    details: dict[str, str] | None
    logged_at_utc: str

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


def run_cli_command(
    *,
    operation: str,
    ticker: str | None,
    pattern: str | None,
    action: Callable[[], Any],
) -> int:
    setup_logging()

    try:
        result = action()
    except Exception as exc:  # pragma: no cover - exercised via CLI tests
        envelope = build_error_envelope(
            operation=operation,
            ticker=ticker,
            pattern=pattern,
            error=exc,
        )
        LOGGER.error(
            "Finance IA CLI %s failed for ticker=%s pattern=%s error_code=%s",
            operation,
            envelope.ticker,
            envelope.pattern,
            envelope.error_code,
            exc_info=True,
        )
        sys.stderr.write(json.dumps(envelope.to_dict(), sort_keys=True))
        sys.stderr.write("\n")
        return 1

    print(json.dumps(result.to_dict(), indent=2, sort_keys=True))
    return 0


def build_error_envelope(
    *,
    operation: str,
    ticker: str | None,
    pattern: str | None,
    error: Exception,
) -> PythonCliErrorEnvelope:
    error_code = resolve_error_code(error)
    technical_message = str(error).strip() or f"{type(error).__name__} raised without a message."
    return PythonCliErrorEnvelope(
        schema_version="1.0",
        source="finance_ia.cli",
        operation=operation,
        error_code=error_code,
        error_type=type(error).__name__,
        message=technical_message,
        user_message=resolve_user_message(error_code),
        ticker=normalize_optional_value(ticker),
        pattern=normalize_optional_value(pattern),
        details=build_details(error),
        logged_at_utc=datetime.now(timezone.utc).isoformat(),
    )


def resolve_error_code(error: Exception) -> str:
    if isinstance(error, FileNotFoundError):
        return "artifact_missing"

    if isinstance(error, json.JSONDecodeError):
        return "invalid_output"

    if isinstance(error, ValueError):
        message = str(error).strip().lower()
        if any(token in message for token in DATA_UNAVAILABLE_TOKENS):
            return "data_unavailable"
        if any(token in message for token in INVALID_OUTPUT_TOKENS):
            return "invalid_output"
        return "invalid_input"

    return "unexpected_error"


def resolve_user_message(error_code: str) -> str:
    return {
        "invalid_input": "La requete envoyee au moteur IA est invalide.",
        "artifact_missing": "Le modele IA est indisponible pour le moment.",
        "data_unavailable": "Les donnees de marche necessaires sont indisponibles pour le moment.",
        "invalid_output": "Le resultat du moteur IA est invalide.",
    }.get(error_code, "Une erreur interne est survenue cote IA.")


def build_details(error: Exception) -> dict[str, str] | None:
    if isinstance(error, FileNotFoundError):
        missing_name = Path(error.filename).name if error.filename else ""
        if missing_name:
            return {"missing_file": missing_name}
        return {"missing_file": "unknown"}

    return None


def normalize_optional_value(value: str | None) -> str | None:
    normalized = (value or "").strip()
    return normalized or None


DATA_UNAVAILABLE_TOKENS = (
    "received an empty ohlcv frame",
    "ohlcv frame is empty after dropping nan rows",
    "no inference rows after indicator computation",
)

INVALID_OUTPUT_TOKENS = (
    "missing required feature columns",
    "invalid feature_columns.json format",
    "invalid metrics.json format",
    "invalid train_config.json format",
)
