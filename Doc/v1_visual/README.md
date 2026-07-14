# PredictFinance — Spec visuelle V1 challenged + benchmarked

This package is the current visual specification for PredictFinance V1. It separates proven routed screens, state variants, reusable component specs, non-routed product targets, and support/control pages.

## Rule of truth

1. Implemented screens remain based on `src/app/Routes/*.ts` and current Angular templates.
2. Variants document visible states, model-driven states, form states, loading/error states, and business non-executable states.
3. Target pages required by the V1 product/screen/gap docs remain explicitly marked as **target / non-routed / non-built**.
4. External finance-product benchmarks are used only to challenge missing UX states. They do not create V1 scope unless supported by code, V1 docs, or legal/data-rights requirements.

## Coverage delivered

| Family | Count |
|---|---:|
| HTML pages total | 167 |
| Current routed pages | 28 |
| State/context variants | 82 |
| Reusable component specs | 16 |
| Target / non-routed pages | 28 |
| Support/control pages | 13 |

## V1 needs coverage check

| V1 need | Classification | Visual coverage |
|---|---|---|
| Anonymous access without product shell | PROVEN | `anonymous/login.html`, `anonymous/forgot-password.html`, `anonymous/reset-password.html` are current routed pages. |
| User watchlist, portfolio, analysis, history, simulation, notifications and account surfaces | PROVEN | Current routed pages are listed in `CURRENT_CODE_VISUAL_ALIGNMENT.md` and `VISUAL_PAGE_INVENTORY.md`. |
| Admin governance surfaces for users, registries, scoring policy, wording, snapshots and data quality | PROVEN | Current routed admin pages are listed in `CURRENT_CODE_VISUAL_ALIGNMENT.md` and `VISUAL_PAGE_INVENTORY.md`. |
| First-rank business states: empty, loading, recoverable error, non-executable | PROVEN | `support/page-states.html`, `support/state-matrix.html`, and per-page variants cover these states. |
| No credible pattern, insufficient data, unsupported instrument | PROVEN | Dedicated analysis and instrument variants exist under `client/analysis-detail-*` and `client/instrument-detail-*`. |
| Portfolio-context recommendation wording | PROVEN | Held and non-held analysis/portfolio variants exist and keep recommendation context explicit. |
| Multi-pattern readability, confidence explanation and action plan | PROVEN | Component specs and analysis variants cover pattern lists, confidence criteria, and action-plan blocks. |
| Fundamental support reading and explicit PEA status | PROVEN | Instrument, portfolio, PEA registry, scoring policy, and data-quality variants keep PEA and support coverage visible. |
| Alert, onboarding, help, legal/data-rights and KPI surfaces not routed yet | DECIDED | Preserved as `target` pages only; they are V1 needs, not implementation proof. |
| Visual proof of backend/API implementation for targets | REMAINING TO ARBITRATE | `v1_visual` proves visual coverage only. Implementation status remains governed by code and `Doc/v1/06_ecarts_doc_code.md`. |

## Main control files

- `CHALLENGE_AUDIT_REPORT.md`
- `EXTERNAL_BENCHMARK_MATRIX.md`
- `VISUAL_TRACEABILITY_GATES.md`
- `VISUAL_PAGE_INVENTORY.md`
- `CURRENT_CODE_VISUAL_ALIGNMENT.md`
- `EXPANDED_VISUAL_COVERAGE_MATRIX.md`
- `VISUAL_SCOPE_DECISION.md`

## Open the gallery

Open `index.html` in a browser.

## Status interpretation

- **Current routed**: route exists in the Angular route table.
- **Variant**: state of an existing screen or model/spec-driven state.
- **Component**: reusable Angular UI component or cross-screen component spec.
- **Target**: V1/spec/legal/benchmark pressure, not proven as implemented.
- **Support**: audit, benchmark, navigation, design-system or validation page.
