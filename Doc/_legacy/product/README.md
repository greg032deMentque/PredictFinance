# Product documentation pack

This folder stores the business and architecture reference used to drive the V1 refactor.

## Reading order for humans
1. `01_besoin_metier_resume.md`
2. `02_versioning_plan_resume.md`
3. `03_v1_contract_freeze_corrected.md`
4. `04_repository_audit_and_target_v1.md`
5. `05_v1_multi_pattern_lock.md`
6. `06_v1_fundamental_scoring_and_pea_eligibility.md`
7. `07_v1_functional_architecture_screen_by_screen.md`
8. `08_v1_admin_and_parameter_explanation_contract.md`
9. `09_v1_gaps_and_required_arbitrations.md`
10. `10_v1_resolved_product_decisions.md`
11. `11_v1_web_architecture_and_ui_wording_lock.md`
12. `12_v1_documentation_baseline_and_canonical_map.md`
13. `pattern_reference_pack/README.md`

## Reading order for coding agents
- `../AGENTS.md`
- `../ai/01_product_operating_contract.md`
- `../ai/02_architecture_and_quality_guardrails.md`
- `../ai/02_documentation_guardrails.md`
- `../ai/03_agent_execution_guardrails.md`
- then the product files above in order

## Important scope rule
Current frozen V1 scope remains aligned with active French listed equities.
ETF support is explicitly out of V1 scope and reserved for V2.
The V1 architecture must remain extensible enough to add ETF-specific policies later without silently broadening current runtime scope.

## Canonical cross-document anchors

When two documents overlap, use these files as the canonical anchors:
- status, recommendation, snapshot, and analysis output contracts -> `Doc/contract_freeze.md`
- product decisions already closed for V1 -> `10_v1_resolved_product_decisions.md`
- screen behavior and user journeys -> `07_v1_functional_architecture_screen_by_screen.md`
- admin perimeter and parameter explanation governance -> `08_v1_admin_and_parameter_explanation_contract.md`
- remaining gaps or intentionally non-closed notions -> `09_v1_gaps_and_required_arbitrations.md`

## Documentation closure status

The V1 documentation baseline is closed for specification purposes.
Remaining non-alignment points are repository-truth mismatches documented in:
- `09_v1_gaps_and_required_arbitrations.md`
- `12_v1_documentation_baseline_and_canonical_map.md`
