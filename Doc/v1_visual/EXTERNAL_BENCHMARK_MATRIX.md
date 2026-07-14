# External benchmark matrix

## Scope

This benchmark is used only to challenge visual completeness. It does not promote competitor features into V1 unless they are already supported by the current code, the V1 documentation, or a legal/data-rights requirement.

| Source | Observed UX pattern | PredictFinance implication | Resulting visual pages |
|---|---|---|---|
| TradingView | Watchlists, watchlist alerts, screeners, charting workspace | Alert flows are expected in finance products; full screeners are too broad for V1 | `client/watchlist-price-alert-target.html`, `client/watchlist-benchmark-screener-rejected.html` |
| Koyfin | Advanced graphing, large equity screener, customizable watchlists and alerts | Dense financial tables need rules; alerts should be target-designed | `components/benchmark-aware-table-density.html`, `components/watchlist-alert-config.html` |
| Simply Wall St | Beginner-friendly stock reports, portfolio/watchlist path, visual analysis | Strengthen first-run guidance and plain-language analysis states | `client/onboarding-first-run-choice.html`, `client/analysis-detail-confidence-low.html` |
| Stock Rover | Research + portfolio management + risk analysis in one workspace | Portfolio risk summary can be a target, but not a new engine | `client/portfolio-risk-summary-target.html` |
| Yahoo Finance | Watchlists, portfolio performance, price alerts | Add watchlist alert and notification target states | `client/notifications-alert-level-crossed.html` |
| Portfolio Performance / Wealthfolio | Portfolio performance tracking and privacy/control positioning | Account data-rights and portfolio summary screens are necessary | `client/account-data-export-target.html`, `client/account-delete-target.html` |
| AMF / ESMA / CNIL pressure | Personalized recommendation risk, suitability/advice boundaries, user data rights | Risk warnings and privacy/data-rights screens must be visible | `client/legal-investment-warning.html`, `support/legal-risk-visual-rules.html` |

## Guardrail

A benchmark page is accepted only if it supports an existing V1 concept. Otherwise it is documented as rejected scope drift.
