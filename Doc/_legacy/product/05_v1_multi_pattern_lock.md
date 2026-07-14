# V1 multi-pattern lock

## 1. Purpose

This document updates the V1 folder state so that V1 explicitly includes a small real multi-pattern scope and so that the next implementation can be checked against repository reality without drift.

This is a repository-grounded lock, not a speculative redesign.

---

## 2. Repository-grounded status

### PROVEN
- `AGENTS.md` already defines V1 as a small real multi-pattern target and names the continuation patterns `RectangleContinuation`, `SymmetricalTriangleContinuation`, `BullFlagContinuation`, and `BearFlagContinuation`.
- `Doc/contract_freeze.md` already freezes response semantics that support multiple compatible patterns, including `mainPattern`, `alternativePatterns`, `requestedPatternIds`, and `MULTIPLE_COMPATIBLE_PATTERNS`.
- `Doc/product/04_repository_audit_and_target_v1.md` already identifies mono-pattern guards as a current repository issue.
- The real active runtime path still contains mono-pattern implementation guards centered on `DOUBLE_TOP`.

### PROVEN IN ACTIVE RUNTIME PATH
- `BackPredictFinance.Services/ClientFinanceServices/Analysis/AnalysisPatternRegistry.cs` still requires one explicit pattern when several definitions are enabled.
- `BackPredictFinance.Services/ClientFinanceServices/ClientFinanceService.cs` still normalizes to `DOUBLE_TOP` and rejects any other pattern.
- `BackPredictFinance.Services/ClientFinanceServices/Analysis/AnalysisLegacyCompatibilityService.cs` still maps only `DOUBLE_TOP` in the legacy compatibility path.
- `BackPredictFinance.Services/ClientFinanceServices/Analysis/RecommendationPolicyService.cs` and `AnalysisSnapshotPersistenceService.cs` still contain pattern-id switches centered on `DOUBLE_TOP`.
- The only concrete pattern definition currently present in the active runtime path is `DoubleTopAnalysisPatternDefinition.cs`.

### PROVEN ARCHITECTURE GAPS
- `BackPredictFinance.Services` still references `BackPredictFinance.ViewModels`, including the active analysis path.
- Core analysis-domain contracts used by the active path still live under `BackPredictFinance.Common/AnalysisV1`, which is broader than the target boundary described in `AGENTS.md`.
- The target-state business-contract boundary described in docs is not yet materialized as a real `BackPredictFinance.Contracts` project in the repository.

### DECIDED
- V1 target remains a small real multi-pattern continuation scope, not a generic plugin platform and not a permanent mono-pattern path.
- The frontend-facing API may keep a temporary compatibility input if repository reality requires it, but the internal analysis request must remain list-based for pattern ids.
- A display-primary pattern is acceptable for UX, but all compatible patterns must remain separately preserved in response and history.

### DOCUMENTED TRANSITION RULES
- `DoubleTop` may remain as compatibility residue during transition, but it must not define the intended V1 target.
- The public request contract may keep a temporary compatibility bridge from `RequestedPattern` only if the internal request is immediately normalized into explicit pattern-id lists.
- Any persistence limitation that prevents continuation patterns from being stored must be treated as a blocking runtime-path gap, not silently worked around by reverting to mono-pattern truth.

---

## 3. Coherence check for the intended V1 -> V2/V3 direction

### COHERENT
- Multi-pattern response semantics are already present in the frozen contract.
- The requirement to keep compatible patterns separate is coherent with later extensibility.
- Portfolio-aware recommendation remains compatible with multi-pattern continuation as long as recommendation truth depends on structured facts, not on pattern names alone.
- A deterministic registry plus separate detection, risk, recommendation, explanation, and persistence layers is coherent with later growth.

### NOT YET COHERENT IN THE REAL REPOSITORY
- The runtime still behaves as if V1 were mono-pattern in several critical execution and compatibility layers.
- Pattern support truth is fragmented across registry, service validation, legacy compatibility mapping, recommendation mapping, and persistence mapping.
- The touched architecture still violates the target boundary because analysis-domain services depend on `ViewModels`.

### CONSEQUENCE
The V1 direction is coherent for the future, but the real repository is not yet coherently implemented for that direction.
The next safe work is therefore not a redesign of the target.
The next safe work is to make the runtime path truly multi-pattern and to collapse mono-pattern truth duplication.

---

## 4. Locked V1 invariants for multi-pattern continuation

The following are now locked for V1 in the documentation folder:

1. V1 is multi-pattern in business scope, even if transport compatibility remains transitional.
2. Internal pattern selection must be list-based.
3. The absence of an explicit requested pattern means the default enabled set, not an implicit `DOUBLE_TOP` fallback.
4. No supported enabled pattern may be rejected by a leftover mono-pattern switch in services, compatibility mappers, recommendation mapping, or persistence mapping.
5. `mainPattern` is UX-primary only. It must never erase alternatives.
6. Recommendation truth must remain structured and portfolio-aware.
7. Pattern-specific execution may use different historical depths.
8. The runtime must not claim support for a pattern until the full path is wired: request resolution, registry, execution, recommendation, persistence, compatibility mapping, and tests.

---

## 5. Recommended implementation order

### Smallest safe next target
Make the current active runtime path continuation-ready without broad rewrite.

### Recommended order
1. Remove `DOUBLE_TOP` hard stops from runtime-path guards and replace them with enabled-pattern based validation.
2. Add the first continuation definitions and register them.
3. Update recommendation, legacy compatibility mapping, and snapshot persistence so that every enabled V1 pattern id is accepted.
4. Add focused tests proving multi-pattern request resolution, execution, recommendation mapping, and persistence mapping.
5. Only after that, tighten architecture boundaries by reducing `Services` -> `ViewModels` coupling and moving analysis-domain contracts toward `BackPredictFinance.Contracts`.

### Explicit anti-drift rule
Do not present V1 as multi-pattern-ready while any critical runtime path still rejects supported patterns outside `DOUBLE_TOP`.

---

## 6. Pattern reference documents
The following documentation references are now part of the product-doc pack:
- `PATTERN-REF-RECTANGLE-CONTINUATION`
- `PATTERN-REF-SYMMETRICAL-TRIANGLE-CONTINUATION`
- `PATTERN-REF-BULL-FLAG-CONTINUATION`
- `PATTERN-REF-BEAR-FLAG-CONTINUATION`

They document intended business reading, validation/invalidation guidance, and advice guardrails.
They do not by themselves prove that the active runtime path supports those patterns.
