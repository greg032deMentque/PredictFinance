from __future__ import annotations

from finance_ia.patterns.base import RuntimePatternAnalyzer
from finance_ia.patterns.double_top import DoubleTopRuntimeAnalyzer

_ANALYZERS: dict[str, RuntimePatternAnalyzer] = {
    "DOUBLE_TOP": DoubleTopRuntimeAnalyzer(),
}


def _normalize_pattern(pattern: str | None) -> str:
    normalized = (pattern or "DOUBLE_TOP").strip().upper().replace(" ", "_")
    return normalized or "DOUBLE_TOP"


def get_runtime_analyzer(pattern: str | None) -> RuntimePatternAnalyzer:
    normalized = _normalize_pattern(pattern)
    analyzer = _ANALYZERS.get(normalized)
    if analyzer is None:
        raise ValueError(f"Unsupported pattern: {normalized}")
    return analyzer


def list_supported_patterns() -> list[str]:
    return sorted(_ANALYZERS)
