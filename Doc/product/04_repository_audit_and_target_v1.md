# Repository audit and target V1

## Current architecture summary
- Angular 21 frontend
- .NET 10 API
- Python analysis engine
- API owns auth, persistence, DTO exposure, and part of recommendation wording
- DB already has a useful history model with `AnalysisRun`, `PatternAssessment`, `DecisionSignal`, and `ModelSnapshot`

## Main issues
- core analysis truth still too Python-centered
- mixed responsibilities in `ClientFinanceService`
- mono-pattern guards hardcoded across layers
- recommendation logic not portfolio-aware
- public response contract too poor for V1
- likely DI runtime issue around market-data providers

## Target V1 architecture
- API = source of truth for market data normalization, pattern analysis, scoring, risk, recommendation, history
- frontend = display and user workflow
- Python = optional adapter only
- patterns = explicit extensible contracts
- history = snapshot-oriented and versioned
