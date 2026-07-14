# PredictFinance V1 Milestone 0 Backend Audit

## Purpose

This document is the Milestone 0 backend starting inventory required by `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`.

It is an audit artifact only.
It does not claim implementation.
It does not freeze still-open route strategy choices by convenience.

## Scope and inputs audited

### PROVEN repository inputs

- `FinanceBack/BackPredictFinance.API/Controllers/AccountController.cs`
- `FinanceBack/BackPredictFinance.API/Controllers/UserController.cs`
- `FinanceBack/BackPredictFinance.API/Controllers/ClientFinanceController.cs`
- `FinanceBack/BackPredictFinance.API/Controllers/FundamentalsController.cs`
- `FinanceBack/BackPredictFinance.API/Controllers/TickersController.cs`
- `FinanceBack/BackPredictFinance.API/Controllers/TradingController.cs`
- `FinanceBack/BackPredictFinance.API/Program.cs`
- `FinanceBack/BackPredictFinance.API/ProgramSubFiles/ProgramServiceDeclarator.cs`
- `FinanceBack/BackPredictFinance.Services/AuthServices/AccountService.cs`
- `FinanceBack/BackPredictFinance.Services/AuthServices/JwtGeneratorService.cs`
- `FinanceBack/BackPredictFinance.Services/UserServices/UserService.cs`
- `FinanceBack/BackPredictFinance.Services/ClientFinanceServices/ClientFinanceService.cs`
- `FinanceBack/BackPredictFinance.Services/ClientFinanceServices/Analysis/AnalysisRequestCompatibilityResolver.cs`
- `FinanceBack/BackPredictFinance.Services/ClientFinanceServices/Analysis/ClientAnalysisOrchestrator.cs`
- `FinanceBack/BackPredictFinance.Services/ClientFinanceServices/Analysis/AnalysisResultProjectionService.cs`
- `FinanceBack/BackPredictFinance.Services/ClientFinanceServices/Analysis/AnalysisSnapshotPersistenceService.cs`
- `FinanceBack/BackPredictFinance.Services/Fundamentals/FundamentalScoringService.cs`
- `FinanceBack/BackPredictFinance.Datas/Context/FinanceDbContext.cs`
- `FinanceBack/BackPredictFinance.Datas/Context/ModelBuilderConfigurationExtensions.cs`
- `FinanceBack/BackPredictFinance.Datas/Entities/*`
- `FinanceBack/BackPredictFinance.Patterns/PatternCatalog.cs`
- `FinanceBack/BackPredictFinance.Patterns/PatternIds.cs`
- `FinanceBack/BackPredictFinance.ViewModels/ClientFinanceViewModels/*`
- `FinanceBack/BackPredictFinance.ViewModels/UserViewModels/*`
- `FinanceBack/BackPredictFinance.ViewModels/Fundamentals/*`
- `FinanceBack/BackPredictFinance.Tests/*`

### PROVEN frontend cross-check inputs used only for mismatch audit

- `FinanceFront/src/app/services/AuthService.service.ts`
- `FinanceFront/src/app/core/auth.store.ts`
- `FinanceFront/src/app/services/client-finance.service.ts`
- `FinanceFront/src/app/components/login/login.ts`
- `FinanceFront/src/app/guard/admin.guard.ts`
- `FinanceFront/src/app/guard/client.guard.ts`
- `FinanceFront/src/app/components/admin/admin-dashboard/admin-dashboard.component.ts`
- `FinanceFront/src/app/components/admin/admin-analyse-finance/admin-analyse-finance.ts`
- `FinanceFront/src/app/components/client/user-finance/user-finance-page/user-finance-page.component.ts`
- `FinanceFront/README.md`

## 1. Current State Audit

### 1.1 PROVEN backend controller and route inventory

#### `AccountController`

- `POST /api/Account/Register`
- `GET /api/Account/ForgotPassword`
- `POST /api/Account/ForgotPassword`
- `POST /api/Account/ResetPassword`
- `POST /api/Account/ChangePassword`
- `POST /api/Account/Login`
- `POST /api/Account/LoginAdmin`
- `POST /api/Account/Refresh`
- `POST /api/Account/Logout`
- `POST /api/Account/UnlockUser/{userId}`
- `DELETE /api/Account/UnblockIp/{ipAddress}`

#### `UserController`

- `POST /api/User/GetUsersList`
- `GET /api/User/GetUserById`
- `POST /api/User/CreateUser`
- `PUT /api/User/UpdateUser`
- `DELETE /api/User/DeleteUser`
- `GET /api/User/GetUserRoles`

#### `ClientFinanceController`

- `GET /api/ClientFinance/dashboard`
- `GET /api/ClientFinance/assets/search`
- `GET /api/ClientFinance/watchlist`
- `POST /api/ClientFinance/watchlist`
- `DELETE /api/ClientFinance/watchlist/{symbol}`
- `GET /api/ClientFinance/quote/{symbol}`
- `GET /api/ClientFinance/transactions`
- `POST /api/ClientFinance/transactions`
- `DELETE /api/ClientFinance/transactions/{id}`
- `POST /api/ClientFinance/analysis/run`
- `GET /api/ClientFinance/analysis/recent`
- `POST /api/ClientFinance/simulation/run`

#### `FundamentalsController`

- `POST /api/Fundamentals/score`

#### `TickersController`

- `GET /api/Tickers/exchanges`
- `GET /api/Tickers/symbols`
- `GET /api/Tickers/symbols/all`
- `GET /api/Tickers/timeseries/{symbol}`

#### `TradingController`

- `POST /api/Trading/predict`
- `GET /api/Trading/predict/{symbol}`
- PROVEN retired surface behavior: both endpoints return `410 Gone` and are quarantined from V1 behavior.

### 1.2 PROVEN auth and session contract reality

- The API uses JWT bearer authentication with the `"Bearer"` policy.
- JWT access tokens are generated with:
  - `sub`
  - `jti`
  - `iat`
  - `amr`
  - repeated role claims via `ClaimTypes.Role`
- The access token does not currently carry:
  - display name
  - email
  - allowed areas
  - permissions
  - default area or landing choice
  - tenant or site truth required by the current frontend `AuthStore`
- `TokenViewModel` returned by login/refresh contains:
  - access token
  - refresh token
  - refresh expiry
  - first name and last name only on login flows
- PROVEN missing route: `GET /api/Account/me`
- REMAINING TO ARBITRATE: `GET /api/Account/me/permissions`
  - Milestone 1A requires it only if role truth alone is insufficient.

### 1.3 PROVEN authorization gap reality

- `DELETE /api/Account/UnblockIp/{ipAddress}` is authenticated but not admin-restricted.
- `GET /api/User/GetUserById` is authenticated but not admin-restricted and accepts any `userId`.
- `PUT /api/User/UpdateUser` is authenticated but not admin-restricted.
- `PUT /api/User/UpdateUser` does not implement admin-targeted user update behavior:
  - the controller shape suggests generic user update,
  - the service updates the current authenticated user only,
  - the frontend admin edit form uses this route for editing arbitrary users.
- PROVEN no integration-test proof currently covers ASP.NET authorization pipeline behavior for these routes.
- PROVEN current tests for controllers are controller-level unit tests with mocks, not security pipeline proofs.

### 1.4 PROVEN admin surface reality versus V1 target

- Current exposed admin-capable routes are limited to:
  - `POST /api/Account/UnlockUser/{userId}`
  - `POST /api/User/GetUsersList`
  - `POST /api/User/CreateUser`
  - `DELETE /api/User/DeleteUser`
  - `GET /api/User/GetUserRoles`
- Current exposed admin surface does not include dedicated governance routes for:
  - admin overview
  - instrument registry
  - PEA registry
  - scoring policy
  - parameter dictionary
  - wording versions
  - snapshot audit
  - data quality
- REMAINING TO ARBITRATE:
  - keep admin user management under `UserController`
  - or introduce `AdminUsersController`
  - Current repository proof is not yet strong enough to freeze that route family decision in Milestone 0.

### 1.5 PROVEN persistence inventory relevant to Sprint 1 through Sprint 7

#### User and session persistence

- `User`
- `RefreshToken`
- ASP.NET Identity user/role tables via `FinanceDbContext`

#### User asset and market persistence

- `Asset`
- `UserAsset`
- `AssetTransaction`
- `PriceHistory`
- `AssetQuoteSnapshot`
- `AssetCandleSnapshot`

#### Analysis and history persistence

- `AnalysisRun`
- `PatternAssessment`
- `DecisionSignal`
- `ModelSnapshot`
- `AnalysisRun.RawPayload` persists a snapshot-oriented JSON payload
- `AnalysisSnapshotPersistenceService` persists:
  - requested pattern ids
  - executed pattern ids
  - portfolio context snapshot and detailed portfolio context
  - pedagogical summary
  - recommendation payload
  - model snapshot payload
  - trace and version fields
  - market data range used

#### Fundamentals and PEA persistence

- `AssetPeaEligibility`
- `FundamentalScoringService` reads explicit PEA registry truth from `AssetPeaEligibility`
- PROVEN no separate persisted scoring-policy entity exists
- PROVEN scoring versions and eligibility policy versions are currently hard-coded service constants

### 1.6 PROVEN persistence reality by required Milestone 0 area

#### History

- PROVEN exists:
  - `AnalysisRun`
  - `PatternAssessment`
  - `DecisionSignal`
  - `ModelSnapshot`
  - snapshot JSON persistence via `AnalysisRun.RawPayload`
- PROVEN current read surface remains limited to `GET /api/ClientFinance/analysis/recent`

#### Comparison

- PROVEN partial persistence foundations exist:
  - analysis snapshots in `AnalysisRun.RawPayload`
  - quote snapshots in `AssetQuoteSnapshot`
  - candle snapshots in `AssetCandleSnapshot`
- PROVEN missing comparison route:
  - `POST /api/ClientFinance/snapshots/compare`
- REMAINING TO ARBITRATE:
  - whether current persisted truth is already sufficient for the final comparison contract without schema change

#### Notifications

- PROVEN_MISSING_PERSISTENCE
- No notification entity, no notification route, no notification service area is present.

#### Wording governance

- PROVEN_MISSING_PERSISTENCE
- No wording version entity, no wording publication entity, no wording governance route is present.

#### Parameter dictionary

- PROVEN_MISSING_PERSISTENCE
- No parameter dictionary entity or route is present.

#### Scoring policy

- PROVEN_MISSING_PERSISTENCE
- Scoring policy truth is currently embedded as service constants, not governed durable persistence.

#### Onboarding

- PROVEN_MISSING_PERSISTENCE
- No onboarding state entity or route is present.

#### Admin data quality

- PROVEN_MISSING_PERSISTENCE
- No data-quality entity, route, or bounded service is present.

### 1.7 DEROGATION and documentation mismatch reality

- DEROGATION:
  - `BackPredictFinance.Services -> BackPredictFinance.ViewModels` dependency remains present.
- DEROGATION:
  - analysis-domain contracts still live under `BackPredictFinance.Common/AnalysisV1`.
- DEROGATION:
  - some persisted closed-state truth remains stored as `string`, including `AnalysisRun.Status`, `PatternAssessment.PatternId`, and `PatternAssessment.Phase`.
- PROVEN documentation mismatch:
  - `Doc/product/12_v1_documentation_baseline_and_canonical_map.md` still says `DOUBLE_TOP` compatibility residue exists.
  - Current repository reality no longer exposes `DOUBLE_TOP` in the active runtime catalog or registry.
  - Current repository only retains rejection tests and a migration removing legacy persistence.

### 1.8 PROVEN frontend/backend mismatch hotspots that directly affect backend planning

- The frontend login and guards still decide `admin` versus `client` by decoding JWT role claims locally.
- The frontend `AuthStore` still expects token claims not issued by the backend:
  - `tenant_id`
  - `tenant_code`
  - `default_site`
  - `site`
  - `is_superadmin`
- The frontend analysis launch request still posts `RequestedPattern` singular.
- The backend `AnalysisRunRequestViewModel` only accepts `RequestedPatternIds`.
- The frontend default analysis request still uses `DOUBLE_TOP`, which the backend explicitly rejects.
- The frontend admin edit form uses `PUT /api/User/UpdateUser` for arbitrary-user editing, but the backend implementation updates only the current authenticated user.
- The frontend README still documents `GET /api/Trading/predict/{symbol}` as the active analysis endpoint, while the backend keeps it retired with `410 Gone`.

### 1.9 PROVEN mock or placeholder surfaces that must not be treated as governed truth

- `FinanceFront/src/app/components/admin/admin-dashboard/admin-dashboard.component.ts`
  - `activeUsersMock`
  - `activeRateMock`
  - `activeUsersMockLabel = "estimation (mock)"`
- `FinanceFront/README.md`
  - describes retired `Trading/predict` as active runtime flow
- `FinanceFront` user/admin analysis screens still display user-facing wording around "analyse IA"
  - current V1 backend target is deterministic and must work without AI

## 2. Gap Analysis Versus Target

### 2.1 Sprint 1 required backend surface classification

| Route or surface | Classification | Reason |
|---|---|---|
| `POST /api/Account/Login` | `PROVEN_EXISTS` | Current login entry exists. |
| `POST /api/Account/Refresh` | `PROVEN_EXISTS` | Current refresh entry exists. |
| `POST /api/Account/Logout` | `PROVEN_EXISTS` | Current logout entry exists. |
| `POST /api/Account/ForgotPassword` | `PROVEN_EXISTS` | Current forgot-password entry exists. |
| `POST /api/Account/ResetPassword` | `PROVEN_EXISTS` | Current reset-password entry exists. |
| `POST /api/Account/ChangePassword` | `PROVEN_EXISTS` | Current authenticated password-change entry exists. |
| `GET /api/Account/me` | `PROVEN_MISSING_ROUTE` | Required by Milestone 1A and absent from current controller surface. |
| `GET /api/Account/me/permissions` | `REMAINING_TO_ARBITRATE` | Only needed if roles alone are proven insufficient beyond `/me`. |
| `POST /api/User/GetUsersList` | `PROVEN_EXISTS` | Current admin list route exists. |
| `GET /api/User/GetUserById` | `PROVEN_AUTHZ_GAP` | Route exists but is under-protected for arbitrary `userId` reads. |
| `PUT /api/User/UpdateUser` | `PROVEN_AUTHZ_GAP` | Route exists but authz and semantics are incoherent for admin edit use. |
| `DELETE /api/Account/UnblockIp/{ipAddress}` | `PROVEN_AUTHZ_GAP` | Sensitive admin-like action lacks admin-only restriction. |

### 2.2 Sprint 2 route classification

| Route or surface | Classification | Reason |
|---|---|---|
| `GET /api/ClientFinance/dashboard` | `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT` | Current payload is KPI-only. |
| `GET /api/ClientFinance/assets/search` | `PROVEN_EXISTS` | Current search route exists. |
| `GET /api/ClientFinance/watchlist` | `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT` | Current payload lacks latest analysis/support/PEA/completeness fields. |
| `POST /api/ClientFinance/watchlist` | `PROVEN_EXISTS` | Current add route exists. |
| `DELETE /api/ClientFinance/watchlist/{symbol}` | `PROVEN_EXISTS` | Current remove route exists. |
| `GET /api/ClientFinance/transactions` | `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT` | Exists as transaction feed, not as full portfolio read contract. |
| `POST /api/ClientFinance/transactions` | `PROVEN_EXISTS` | Current transaction create route exists. |
| `DELETE /api/ClientFinance/transactions/{id}` | `PROVEN_EXISTS` | Current transaction delete route exists. |
| `GET /api/ClientFinance/portfolio` | `REMAINING_TO_ARBITRATE` | Milestone 0 must not freeze extension-versus-new-route choice. |
| `POST /api/ClientFinance/analysis/run` | `PROVEN_EXISTS` | Current execution entry exists. |
| `GET /api/ClientFinance/analysis/{analysisId}` | `PROVEN_MISSING_ROUTE` | Persisted detail read route is absent. |
| `POST /api/ClientFinance/simulation/run` | `PROVEN_EXISTS` | Current simulation entry exists. |

### 2.3 Sprint 3 route classification

| Route or surface | Classification | Reason |
|---|---|---|
| `GET /api/ClientFinance/analysis/recent` | `PROVEN_EXISTS_BUT_PAYLOAD_INSUFFICIENT` | Supports recent list but not full history or detail navigation contract. |
| Generic history controller or route family | `REMAINING_TO_ARBITRATE` | Milestone 0 must leave extension-versus-new-controller open. |
| `GET /api/ClientFinance/instruments/{symbol}` | `PROVEN_MISSING_ROUTE` | No instrument detail read route exists. |
| `GET /api/ClientFinance/instruments/{symbol}/analysis-history` | `PROVEN_MISSING_ROUTE` | No per-instrument history route exists. |
| `POST /api/ClientFinance/snapshots/compare` | `PROVEN_MISSING_ROUTE` | No snapshot comparison route exists. |

### 2.4 Sprint 4 route classification

| Route or surface | Classification | Reason |
|---|---|---|
| `GET /api/ClientFinance/runtime-scope` | `PROVEN_MISSING_ROUTE` | No learn/runtime-scope metadata route exists. |
| `GET /api/ClientFinance/parameters/{parameterId}` | `PROVEN_MISSING_ROUTE` | No governed parameter-detail route exists. |
| Dedicated learn controller | `REMAINING_TO_ARBITRATE` | Route family choice should stay open until touched-path audit. |
| Dedicated account self-service profile route | `REMAINING_TO_ARBITRATE` | Current repository does not yet prove the smallest safe self-profile route shape. |

### 2.5 Sprint 5 route classification

| Route or surface | Classification | Reason |
|---|---|---|
| `GET /api/Admin/operations/dashboard` | `PROVEN_MISSING_ROUTE` | No admin overview route exists. |
| `GET /api/Admin/instruments` | `PROVEN_MISSING_ROUTE` | No admin instrument registry route exists. |
| `GET /api/Admin/instruments/{instrumentId}` | `PROVEN_MISSING_ROUTE` | No admin instrument detail route exists. |
| `GET /api/Admin/pea-registry` | `PROVEN_MISSING_ROUTE` | No PEA registry route exists. |
| `GET /api/Admin/pea-registry/{instrumentId}` | `PROVEN_MISSING_ROUTE` | No PEA registry detail route exists. |
| `PUT /api/Admin/pea-registry/{instrumentId}` | `PROVEN_MISSING_ROUTE` | No PEA registry write route exists. |
| `GET /api/Admin/scoring-policies` | `PROVEN_MISSING_ROUTE` | No scoring-policy governance route exists. |
| `GET /api/Admin/scoring-policies/active` | `PROVEN_MISSING_ROUTE` | No active scoring-policy route exists. |
| `GET /api/Admin/parameter-dictionary` | `PROVEN_MISSING_ROUTE` | No admin parameter-dictionary route exists. |
| `GET /api/Admin/parameter-dictionary/{parameterId}` | `PROVEN_MISSING_ROUTE` | No admin parameter-detail governance route exists. |
| `AdminUsersController` route family | `REMAINING_TO_ARBITRATE` | Existing `UserController` covers part of the space, but authz and edit semantics are currently incoherent. |

### 2.6 Sprint 6 and Sprint 7 route classification

| Route or surface | Classification | Reason |
|---|---|---|
| `GET /api/Admin/recommendation-wordings` | `PROVEN_MISSING_ROUTE` | No wording governance route exists. |
| `GET /api/Admin/recommendation-wordings/{scenarioCode}` | `PROVEN_MISSING_ROUTE` | No wording scenario detail route exists. |
| `GET /api/Admin/snapshots` | `PROVEN_MISSING_ROUTE` | No snapshot audit route exists. |
| `GET /api/Admin/snapshots/{snapshotId}` | `PROVEN_MISSING_ROUTE` | No snapshot audit detail route exists. |
| `GET /api/Admin/data-quality/overview` | `PROVEN_MISSING_ROUTE` | No data-quality route exists. |
| `GET /api/Notifications` or equivalent notification-center surface | `PROVEN_MISSING_ROUTE` | No notification-center route exists. |

## 3. Target Design Boundaries Confirmed by the Audit

### DECIDED

- `TradingController` stays retired and must not host V1 analysis behavior.
- `AccountController`, `UserController`, and `ClientFinanceController` are the first controllers to extend before creating parallel route families.
- `ClientFinanceController` already owns the coherent user analysis/watchlist/transaction surface.
- History persistence already exists and must be reused before inventing new snapshot tables.
- PEA eligibility already has explicit durable persistence in `AssetPeaEligibility` and must be reused.

### REMAINING TO ARBITRATE

- dedicated portfolio route versus coherent extension of current user-asset reads
- dedicated history controller versus coherent extension of existing route family
- dedicated learn runtime-scope route versus reuse of current user route family
- admin user management staying inside `UserController` versus dedicated `AdminUsersController`
- whether later snapshot comparison requires new durable fields beyond current persisted snapshot foundations

## 4. Exact Files To Change For Milestone 0

### PROVEN changed in this milestone

- `Doc/PredictFinance_V1_Milestone_0_Backend_Audit.md`
- `Doc/PredictFinance_V1_Technical_Derogation_Register.md`

### DECIDED not changed in this milestone

- no backend code file
- no frontend code file
- no migration
- no service registration
- no controller

## 5. Implementation

### PROVEN

- Milestone 0 was executed as an audit-only increment.
- A backend starting inventory artifact now exists in `Doc/`.
- A technical derogation register now exists in `Doc/`.

### DECIDED

- No feature implementation starts in Milestone 0.
- No route strategy still open was frozen by convenience.

## 6. Validation

### PROVEN audit method

- controller inventory rebuilt from repository files
- auth/session contract rebuilt from real token generation and startup configuration
- persistence inventory rebuilt from `FinanceDbContext`, entities, and analysis/fundamental services
- frontend mismatch hotspots cross-checked from the Angular code currently in the repository

### PROVEN validation expected after the documentation artifact

- backend build and test baseline should be run before moving to Milestone 1A
- authz proof remains insufficient until real integration tests are added in Milestone 1A

## 7. Residual Risks

### DEROGATION

- `Services -> ViewModels` coupling still exists and must not spread further.

### PROVEN

- Current admin edit flow is incoherent:
  - frontend edits arbitrary users,
  - backend update route currently edits the current principal.
- Current frontend analysis request payload is misaligned with backend multi-pattern input.
- Current frontend shell depends on JWT-decoded assumptions instead of a backend-governed `/me` contract.

### REMAINING TO ARBITRATE

- exact portfolio read-route strategy
- exact history route-family strategy
- exact learn/runtime-scope route-family strategy
- exact admin-user controller strategy

## 8. Deferred But Explicitly Not Done

- no Milestone 1A authorization fix
- no `/api/Account/me`
- no permissions endpoint
- no portfolio endpoint
- no history endpoint
- no learn endpoint
- no admin governance endpoint
- no migration for notifications, wording, parameter dictionary, scoring policy, onboarding, or data quality
