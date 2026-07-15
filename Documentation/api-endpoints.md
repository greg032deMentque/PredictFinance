# API surface — controller map

30 controllers in `BackPredictFinance.API/Controllers/` (4 root, 10 ClientFinance, 16 admin). Controllers are **thin**: they authenticate/authorise, bind, call a service, return a ViewModel. Business logic lives in `Services`.

## Auth model

- **Admin** controllers: `[Authorize(Policy = "RequireAdminRole")]` — require the `Admin` role (not just a valid token). All 16 verified.
- **ClientFinance** controllers: `[Authorize]` — any authenticated user; queries are always scoped to the current user's id from the JWT claim (`sub` / `NameIdentifier`), never a client-supplied id (IDOR-safe).
- **Account**: mostly `[AllowAnonymous]` auth endpoints (login, register, refresh, password reset…).
- `MapHealthChecks("/health")` is anonymous.

## Root controllers (`api/[controller]`)

| Controller | Route | Purpose |
|---|---|---|
| `AccountController` | `api/Account` | authentication & account lifecycle (login, admin login, register, confirm, refresh, logout, password reset) **plus RGPD self-service**: `GET/PUT Profile`, `GET me`, `GET/PATCH consents`, `POST data-export`, `DELETE self` (soft-delete via `User.DeletedAt`), `GET/PATCH alert-preferences` |
| `NotificationsController` | `api/Notifications` | user notifications (list, mark as read) |
| `TickersController` | `api/Tickers` | market data / ticker info |
| `TradingController` | `api/Trading` | **410 Gone tombstone** — `predict` / `predict/{symbol}` return `410` with a French problem-detail; the product is not an execution system |

## ClientFinance controllers (`api/ClientFinance`)

The client-facing surface. All share the `api/ClientFinance` base and per-user scoping.

| Controller | Covers |
|---|---|
| `ClientFinanceAnalysisController` | run analysis, analysis history, snapshots |
| `ClientFinancePortfolioController` | portfolios & transactions (CRUD) |
| `ClientFinancePortfolioRiskController` | portfolio risk metrics |
| `ClientFinanceMarketController` | quotes, instruments, watchlist, dashboard |
| `ClientFinanceScreenerController` | screener & saved presets |
| `ClientFinanceTaxController` | tax summary |
| `ClientFinanceFundamentalsController` | per-instrument fundamentals |
| `ClientFinanceTechnicalIndicatorsController` | technical indicators |
| `ClientFinanceLearningController` | learn / education / glossary (client view) |
| `ClientFinanceContactController` | contact / support |

## Admin controllers (`api/admin`)

Governance & content administration; all require the `Admin` role. Routes are `api/admin/<kebab>` (not `api/<Controller>/<Action>`).

`AdminOverviewController`, `AdminUserController`, `AdminKpiController`, `AdminInstrumentRegistryController`, `AdminPeaRegistryController`, `AdminScoringPolicyController`, `AdminParameterDictionaryController`, `AdminSnapshotAuditController`, `AdminDataQualityController`, `AdminAnalysisContentController`, `AdminWordingVersionsController`, `AdminLearnTopicController`, `AdminGlossaryController`, `AdminEducationController`, `AdminFaqController`, `AdminLegalController`.

## Contract notes

- Enums are serialised as strings (PascalCase). The frontend must send/receive string names, not ordinals.
- Error responses carry a French business message + a traceId for correlation (the traceId is for logs/support, never shown to the user by the frontend).
- Lists should be paginated.
