# Current code → visual spec alignment

Source used: current Angular routes and templates under `FinanceFront/src/app`.

| Route | Component | Template source | Visual file |
|---|---|---|---|
| `/login` | `LoginComponent` | `src/app/components/login/login.html` | `anonymous/login.html` |
| `/forgot-password` | `ForgotPasswordComponent` | `src/app/components/auth/forgot-password/forgot-password.component.html` | `anonymous/forgot-password.html` |
| `/reset-password` | `ResetPasswordComponent` | `src/app/components/auth/reset-password/reset-password.component.html` | `anonymous/reset-password.html` |
| `/client/dashboard` | `ClientDashboardComponent` | `src/app/components/client/client-dashboard/client-dashboard.html` | `client/dashboard.html` |
| `/client/watchlist` | `WatchlistPageComponent` | `src/app/components/client/pages/watchlist-page/watchlist-page.component.html` | `client/watchlist.html` |
| `/client/portfolio` | `PortfolioPageComponent` | `src/app/components/client/pages/portfolio-page/portfolio-page.component.html` | `client/portfolio.html` |
| `/client/analysis` | `AnalysisEntryPageComponent` | `src/app/components/client/pages/analysis-entry-page/analysis-entry-page.component.html` | `client/analysis-entry.html` |
| `/client/analysis/:analysisId` | `AnalysisDetailPageComponent` | `src/app/components/client/pages/analysis-detail-page/analysis-detail-page.component.html` | `client/analysis-detail.html` |
| `/client/history` | `HistoryPageComponent` | `src/app/components/client/pages/history-page/history-page.component.html` | `client/history.html` |
| `/client/simulation` | `SimulationPageComponent` | `src/app/components/client/pages/simulation-page/simulation-page.component.html` | `client/simulation.html` |
| `/client/instruments/:symbol` | `InstrumentDetailPageComponent` | `src/app/components/client/pages/instrument-detail-page/instrument-detail-page.component.html` | `client/instrument-detail.html` |
| `/client/notifications` | `NotificationsComponent` | `src/app/components/client/notifications/notifications.component.html` | `client/notifications.html` |
| `/client/account/profile` | `AccountProfileComponent` | `src/app/components/client/account/profile/account-profile.component.html` | `client/account-profile.html` |
| `/client/account/security` | `AccountSecurityComponent` | `src/app/components/client/account/security/account-security.component.html` | `client/account-security.html` |
| `/admin/dashboard` | `AdminDashboardComponent` | `src/app/components/admin/admin-dashboard/admin-dashboard.component.html` | `admin/dashboard.html` |
| `/admin/users` | `AdminUsersListComponent` | `src/app/components/admin/user/list/admin-users-list.component.html` | `admin/users.html` |
| `/admin/users/add et /admin/users/edit/:id` | `AdminUserFormComponent` | `src/app/components/admin/user/form/admin-user-form.component.html` | `admin/user-form.html` |
| `/admin/instrument-registry` | `AdminInstrumentRegistryComponent` | `src/app/components/admin/governance/admin-instrument-registry/admin-instrument-registry.component.html` | `admin/instrument-registry.html` |
| `/admin/pea-registry` | `AdminPeaRegistryComponent` | `src/app/components/admin/governance/admin-pea-registry/admin-pea-registry.component.html` | `admin/pea-registry.html` |
| `/admin/scoring-policy` | `AdminScoringPolicyComponent` | `src/app/components/admin/governance/admin-scoring-policy/admin-scoring-policy.component.html` | `admin/scoring-policy.html` |
| `/admin/parameter-dictionary` | `AdminParameterDictionaryComponent` | `src/app/components/admin/governance/admin-parameter-dictionary/admin-parameter-dictionary.component.html` | `admin/parameter-dictionary.html` |
| `/admin/parameter-dictionary/detail/:parameterId` | `AdminParameterDictionaryDetailComponent` | `src/app/components/admin/governance/admin-parameter-dictionary-detail/admin-parameter-dictionary-detail.component.html` | `admin/parameter-dictionary-detail.html` |
| `/admin/wording-versions` | `AdminWordingVersionsComponent` | `src/app/components/admin/governance/admin-wording-versions/admin-wording-versions.component.html` | `admin/wording-versions.html` |
| `/admin/wording-versions/detail/:wordingVersionId` | `AdminWordingVersionDetailComponent` | `src/app/components/admin/governance/admin-wording-version-detail/admin-wording-version-detail.component.html` | `admin/wording-version-detail.html` |
| `/admin/snapshot-audit` | `AdminSnapshotAuditComponent` | `src/app/components/admin/governance/admin-snapshot-audit/admin-snapshot-audit.component.html` | `admin/snapshot-audit.html` |
| `/admin/snapshot-audit/detail/:analysisRunId` | `AdminSnapshotAuditDetailComponent` | `src/app/components/admin/governance/admin-snapshot-audit-detail/admin-snapshot-audit-detail.component.html` | `admin/snapshot-audit-detail.html` |
| `/admin/snapshot-audit/compare` | `AdminSnapshotAuditCompareComponent` | `src/app/components/admin/governance/admin-snapshot-audit-compare/admin-snapshot-audit-compare.component.html` | `admin/snapshot-audit-compare.html` |
| `/admin/data-quality` | `AdminDataQualityComponent` | `src/app/components/admin/governance/admin-data-quality/admin-data-quality.component.html` | `admin/data-quality.html` |

## Current non-routed V1 targets

| Target family | Current V1 visual files |
|---|---|
| User help and learning | `client/target-help-center.html`, `client/target-learn.html`. |
| User onboarding | `client/target-onboarding-empty.html` plus onboarding variants. |
| Parameter and snapshot reading | `client/target-parameter-detail.html`, `client/target-snapshot-comparison.html`. |
| Admin KPI steering | `admin/target-engagement.html`, `admin/target-signal-quality.html` and KPI target variants. |
| Analysis result composition | Routed `client/analysis-detail.html` plus component specs. |

These target pages remain explicit V1 needs and must not be described as implemented until routes and backend/API support prove it.

## Evidence classification

| Statement | Classification |
|---|---|
| The routed pages in the table above are present in the current Angular route files. | PROVEN |
| The target files listed here cover V1 needs that are not routed yet. | DECIDED |
| A future implementation increment may promote a target family to routed status once route and backend/API support exist. | PROPOSED |
| Backend/API completeness for target pages remains outside this visual alignment file. | REMAINING TO ARBITRATE |

## Checkpoints applied

- The visual gallery now lists only current routed screens plus support reference pages.
- Dynamic routes are represented by one visual page per routed component.
- The admin detail screens now have visual files: parameter detail, wording detail, snapshot detail and snapshot compare.
- The user add/edit routes share one visual form file because the Angular route table uses one component for both routes.


## Expanded visual pages added after route-level reconstruction

See `EXPANDED_VISUAL_COVERAGE_MATRIX.md`. These pages are not additional Angular routes; they are visual variants, component specs or non-routed product targets.
