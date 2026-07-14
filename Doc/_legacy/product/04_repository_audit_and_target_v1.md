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


## Repository-truth clarifications
- solution currently contains `BackPredictFinance.Patterns`
- solution currently does not contain `BackPredictFinance.Contracts`
- several V1 contracts still live under `BackPredictFinance.Common/AnalysisV1`
- `BackPredictFinance.Services` still depends on `BackPredictFinance.ViewModels`
- legacy compatibility around `DOUBLE_TOP` still exists in the codebase even though the active target V1 continuation scope is broader

## Documentation rule
Any target-state architecture statement must be kept distinguishable from repository truth until the refactor is actually implemented.
