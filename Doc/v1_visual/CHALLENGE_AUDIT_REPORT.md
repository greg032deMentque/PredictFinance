# PredictFinance V1 visual spec — challenge audit after benchmark

## Verdict

The current package was audited against three sources:

1. current Angular routes/templates;
2. V1 product/screen/gap documentation;
3. comparable finance products and regulatory/data-rights expectations.

This challenged package keeps the rule: **a page is never marked implemented unless the Angular route exists**. New pages added by the challenge are therefore classified as `variant`, `target`, `component`, or `support`.

## Evidence classification

| Statement | Classification |
|---|---|
| Current routed pages are proven only from `FinanceFront/src/app/Routes/*.ts`. | PROVEN |
| V1 visual targets for onboarding, alerts, help, legal/data-rights and KPI are product needs, even when not routed. | DECIDED |
| Benchmark material is used only to challenge missing states and density, not to expand V1 scope silently. | DECIDED |
| Any target page without an Angular route remains non-implemented until code proves otherwise. | PROVEN |
| Backend/API completeness for target pages is outside the visual package and must be checked in code and gap docs. | REMAINING TO ARBITRATE |

## Main correction

The visual spec must cover not only routed pages but also the states that decide user trust:

- first-run onboarding;
- risk and legal warnings around action verbs;
- confidence breakdown edge states;
- action-plan rendering constraints;
- ex-post pending/data-unavailable/outcome states;
- alert-target visuals;
- account data export/deletion/privacy states;
- admin signal-quality and engagement KPI target states.

## Benchmark pressure retained

| Product family observed | Useful pressure for PredictFinance V1 | Decision |
|---|---|---|
| TradingView / Koyfin | dense watchlists, alerts, screeners, configurable analysis workspaces | Keep alerts as V1 target; reject full screener as V1 scope drift. |
| Simply Wall St | beginner-oriented visual stock reports, watchlist/portfolio path, plain-language analysis | Strengthen onboarding, confidence explanation, and pedagogical analysis states. |
| Stock Rover | portfolio analytics, risk analysis, multi-panel research | Keep portfolio risk summary as target only; do not invent backend scoring. |
| Yahoo Finance | custom watchlists, portfolio tracking, price alerts | Add watchlist alert target and notification variants. |
| Portfolio Performance / Wealthfolio | portfolio tracking, privacy/local-first positioning, performance focus | Add privacy/data-rights screens and portfolio performance-summary visual target. |

## Rejected scope drift

The benchmark does **not** justify adding a full market screener, multi-chart workstation, broker order flow, social trading, crypto/ETF expansion, or real-time trading terminal to V1. Those features are common externally but not proven by the current V1 scope.

## Files added by this challenge

See `VISUAL_PAGE_INVENTORY.md` and `support/screen-inventory-by-source.html`.
