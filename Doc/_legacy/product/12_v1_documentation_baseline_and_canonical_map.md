# V1 documentation baseline and canonical map

## Purpose

This file closes the documentation-finalization work for the V1 baseline.
It exists to answer four questions unambiguously:
1. Which files are canonical?
2. Which files are summaries or derived reading aids?
3. Which files are agent-operating documents?
4. What is still a real repository mismatch versus a documentation ambiguity?

## Baseline status

The V1 documentation baseline is now considered **closed for product-specification purposes**.

Meaning:
- the V1 product scope is frozen,
- the main business contracts are frozen,
- the web architecture is frozen,
- the admin perimeter is frozen,
- the UI wording discipline is frozen,
- remaining implementation work must not be treated as unresolved product ambiguity.

This does **not** mean the repository code is fully aligned with the target architecture.
It also does **not** mean every quantitative backend threshold is fully specified in the product documents.
It means the documentation no longer leaves material first-rank business or architectural ambiguity for V1, and any remaining quantitative calibration belongs in explicit technical contracts rather than reopened product scope.

## V1 frozen scope

Frozen V1 runtime scope:
- French listed equities only
- daily analysis only
- deterministic backend-owned business truth
- multi-pattern continuation support in the documented V1 target
- beginner-readable user UI
- governed admin UI
- no ETF runtime support
- no broker/execution behavior
- no AI-authored mandatory wording

Explicitly out of scope for V1:
- ETF runtime support
- nightly batch delivery
- AI-generated mandatory explanations
- hidden frontend-owned recommendation truth
- silent expansion to unsupported markets or instruments

## Canonical file map

### Canonical product contract files
Use these as the source of truth when overlap exists.

- `Doc/contract_freeze.md`
  - canonical contract owner for:
    - Instrument
    - PortfolioLine
    - PortfolioContext
    - AnalysisRequest
    - PatternAssessment
    - Recommendation
    - AnalysisSnapshot
    - AnalysisOutcome
    - canonical PatternStatus codes

- `Doc/product/10_v1_resolved_product_decisions.md`
  - canonical owner for:
    - V1 closed product decisions
    - canonical admin perimeter
    - recommendation composition rule
    - support-reading role
    - PatternStatus decision posture
    - AnalysisOutcome visibility posture

- `Doc/product/07_v1_functional_architecture_screen_by_screen.md`
  - canonical owner for:
    - user journeys
    - screen-by-screen functional behavior aligned to the web architecture lock
    - screen-level composition reminders
  - limitation:
    - route hierarchy and first-rank screen decomposition must follow `Doc/product/11_v1_web_architecture_and_ui_wording_lock.md` when both files overlap

- `Doc/product/08_v1_admin_and_parameter_explanation_contract.md`
  - canonical owner for:
    - admin functional perimeter details
    - parameter dictionary governance
    - wording governance for parameter explanations

- `Doc/product/11_v1_web_architecture_and_ui_wording_lock.md`
  - canonical owner for:
    - web information architecture
    - user/admin route architecture
    - UI wording rules
    - allowed versus forbidden UI wording

- `Doc/product/06_v1_fundamental_scoring_and_pea_eligibility.md`
  - canonical owner for:
    - support-reading/scoring semantics
    - PEA eligibility semantics
    - coverage rules
    - informational-only parameter rules such as `payoutRatio`

### Canonical gap / mismatch owner
- `Doc/product/09_v1_gaps_and_required_arbitrations.md`
  - canonical owner for:
    - repository-truth mismatches still present in code
    - intentionally non-closed or removed notions
    - “do not pretend implemented” safeguards
    - high-level API surface gap classification versus the normative UI/UX baseline

- `Doc/product/13_v1_api_surface_gap_audit.md`
  - canonical owner for:
    - route-by-route API coverage audit versus the normative UI/UX baseline
    - distinction between existing routes, insufficient payloads, and truly missing routes
    - minimum endpoint additions still required before the documented V1 web architecture can be considered API-covered

### Human-readable product summaries
These are reading aids and must align to the canonical files above:
- `Doc/01_Besoin_metier_specification_produit_detaillee.md`
- `Doc/02_Planning_versioning_specification_produit_detaillee.md`
- `Doc/product/01_besoin_metier_resume.md`
- `Doc/product/02_versioning_plan_resume.md`
- `Doc/product/03_v1_contract_freeze_corrected.md`
- `Doc/product/04_repository_audit_and_target_v1.md`
- `Doc/product/05_v1_multi_pattern_lock.md`
- `Doc/product/README.md`
- `Doc/product/pattern_reference_pack/*`

### Agent-operating files
These files are for implementation discipline only:
- `AGENTS.md`
- `Doc/ai/01_product_operating_contract.md`
- `Doc/ai/02_architecture_and_quality_guardrails.md`
- `Doc/ai/02_documentation_guardrails.md`
- `Doc/ai/03_agent_execution_guardrails.md`
- `Doc/codex/prompt-*.md`

Rule:
- agent-operating files must never silently override the canonical product contracts
- they may only define how to work safely from them
- execution prompts must never own product truth, frozen scope truth, or next-step decisions

## Document status matrix

| File family | Status | Role |
|---|---|---|
| `Doc/contract_freeze.md` | Canonical | Contract truth |
| `Doc/product/06..11` | Canonical | Product truth by domain |
| `Doc/product/09...md` | Canonical | Gap and mismatch truth |
| `Doc/product/13...md` | Canonical | API surface gap audit |
| `Doc/01..02` | Derived high-level | Human-readable product framing |
| `Doc/product/01..05` | Derived/bridging | Summaries and transition context |
| `Doc/ai/*` | Operating | Agent discipline |
| `Doc/codex/prompt-*.md` | Operating | Execution prompts only |
| `Doc/product/pattern_reference_pack/*` | Reference | Pattern-specific detailed references |
| `.xlsx` files | Reference/input artifacts | External decision support, not canonical normative contract |

## Remaining repository-truth mismatches

These remain real codebase mismatches and must stay documented honestly:

- `BackPredictFinance.Patterns` exists in the solution.
- `BackPredictFinance.Contracts` does not yet exist in the repository.
- Several business-core analysis contracts still live under `BackPredictFinance.Common/AnalysisV1`.
- `BackPredictFinance.Services` still depends on `BackPredictFinance.ViewModels`.
- `DOUBLE_TOP` compatibility residue still exists.

These are **implementation mismatches**, not unresolved V1 documentation ambiguities.

## Final closure rule

From this point onward, a coding agent must not reopen already-frozen V1 documentation decisions unless:
- repository evidence contradicts the current documentation, or
- a new explicit product decision replaces the baseline.

Absent one of those two conditions, the V1 documentation baseline must be treated as closed.
