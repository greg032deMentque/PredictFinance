# PredictFinance V1 Technical Derogation Register

## Purpose

This file is the explicit technical derogation register required before wider backlog expansion.

It records repository-truth debt that already exists.
It does not normalize that debt into target architecture.
It exists to prevent new milestones from spreading known derogations by convenience.

## Derogation entries

### DEROGATION-001

- Title: `BackPredictFinance.Services -> BackPredictFinance.ViewModels` coupling exists
- PROVEN repository evidence:
  - `FinanceBack/BackPredictFinance.Services/BackPredictFinance.Services.csproj` references `BackPredictFinance.ViewModels`
  - service implementations use view-model types directly, including `ClientFinanceService`, `UserService`, and `AccountService`
- Why this is a derogation:
  - `ViewModels` is the frontend transport boundary, not the target backend internal contract boundary
- Operating rule:
  - do not introduce new service-to-ViewModel coupling unless the touched scope already requires temporary compatibility and the derogation is called out explicitly

### DEROGATION-002

- Title: analysis-domain contracts still live under `BackPredictFinance.Common/AnalysisV1`
- PROVEN repository evidence:
  - multiple analysis contracts still live under `FinanceBack/BackPredictFinance.Common/AnalysisV1/*`
- Why this is a derogation:
  - capability-specific business contracts should not keep expanding under `Common`
- Operating rule:
  - do not extend `Common/AnalysisV1` further unless the touched scope has no smaller safe boundary in current repository reality

### DEROGATION-003

- Title: some persisted closed-state truth is stored as `string`
- PROVEN repository evidence:
  - `FinanceBack/BackPredictFinance.Datas/Entities/AnalysisRun.cs` stores `Status` as `string`
  - `FinanceBack/BackPredictFinance.Datas/Entities/PatternAssessment.cs` stores `PatternId` as `string`
  - `FinanceBack/BackPredictFinance.Datas/Entities/PatternAssessment.cs` stores `Phase` as `string`
- Why this is a derogation:
  - closed-set business truth is safer when enum-backed or equivalently locked
- Operating rule:
  - do not introduce new free-form persisted closed-state fields without explicit justification
  - do not treat current string-backed persistence as a green light to expand string drift

### DEROGATION-004

- Title: current documentation still contains a stale `DOUBLE_TOP` residue statement
- PROVEN repository evidence:
  - `Doc/product/12_v1_documentation_baseline_and_canonical_map.md` still says `DOUBLE_TOP` compatibility residue exists
  - active backend runtime files `PatternCatalog.cs` and `PatternIds.cs` no longer expose `DOUBLE_TOP`
  - current repository retains only rejection tests and a migration removing legacy persistence
- Why this is a derogation:
  - documentation now lags repository truth on this point
- Operating rule:
  - do not document `DOUBLE_TOP` as active V1 runtime support
  - update stale documentation when the touched milestone allows it

## Explicitly not treated as derogation

### PROVEN

- `BackPredictFinance.Patterns` existing as a project is repository truth, not a derogation.
- `TradingController` being retired with `410 Gone` is repository truth, not a derogation.
- `AssetPeaEligibility` explicit persistence is repository progress, not a derogation.

## Next required use of this register

### DECIDED

- Milestone 1A must use this register when fixing authorization and establishing `/api/Account/me`.
- Later milestones must avoid widening any entry above unless the change explicitly documents why.
