# CI/CD

CI runs on **Azure DevOps Pipelines** (not GitHub Actions). Four YAML pipelines live in the repo; the actual deployment step (to the host) is configured outside the repo in Azure DevOps release/environment settings.

| Pipeline file | App | Environment | Trigger branch |
|---|---|---|---|
| `FinanceBack/predictfinance-prod-pipeline.yml` | Backend | Production | `master` |
| `FinanceBack/predictfinance-staging-pipeline.yml` | Backend | Staging | `dev` |
| `FinanceFront/predictfinance-front-prod-pipeline.yml` | Frontend | Production | `master` |
| `FinanceFront/predictfinance-front-staging-pipeline.yml` | Frontend | Staging | `dev` |

Each pipeline declares an explicit `trigger:` so only its branch launches it.

## Backend pipeline stages

`restore → build → test → (vulnerability scan) → publish`

- **Build**: `dotnet build` with `TreatWarningsAsErrors=true` and SonarAnalyzer — the build **is** the quality gate; any analyzer warning fails it.
- **Test**: `dotnet test` on `BackPredictFinance.Tests`. It runs the full suite **except** 6 integration tests currently excluded at method level (see below).
- **Vulnerability scan**: `dotnet list package --vulnerable` wrapped in a PowerShell step that **fails the build** on `High`/`Critical` findings (blocking, not informational).
- **Publish**: self-contained `linux-x64`, strips local `appsettings.*.json` from the artifact.

### The 6 excluded integration tests

The root-cause infra bug (`ConnectionStrings:DefaultConnection` missing in `ApiIntegrationTestFactory`) is **fixed** — the factory now injects an in-memory test configuration, so integration tests (including `AuthzIntegrationTests`) run in the gate again.

Six tests remain excluded **at method level** because they fail on a *different* cause — missing business seed data (wording / notifications / watchlist) in the in-memory test DB:

- `NotificationsApiFeatureTests.Notifications_GetList_ReturnsCurrentUserNotifications_FilteredAndNewestFirst`
- `AdminWordingVersionVersionDetailApiFeatureTests.GetVersionDetail_ReturnsGovernedVersionProjection_ForAdmin`
- `WordingPublicationServiceTests.ResolveScenarioAsync_ReturnsDeterministicTemplate_ForClosedRecommendationState`
- `AdminGovernanceApiFeatureTests.AdminWordingVersions_ReturnsActiveGovernance_ForAdmin`
- `ClientFinanceWatchlistIntegrationTests.GetWatchlist_ExcludesHeldAssetsFromClientWatchlist`
- `WatchlistRecommendationNeutralizationTests.GetWatchlist_WhenSnapshotWasComputedAsHeld_ButAssetIsNowNotHeld_ReturnsWaitVerb`

Method-level exclusion preserves coverage of healthy sibling tests (e.g. the authz `Forbidden` test in `AdminGovernanceApiFeatureTests`). **TODO: seed this test data, then remove the `--filter`.**

## Frontend pipeline stages

`npm install → npm audit → lint → build → publish`

- **`npm audit --audit-level=high`**: blocking on high/critical.
- **`npm run lint`**: blocking.
- **Build**: production uses `npm run build -- --configuration production`; staging uses `--configuration staging`.
- Budgets are enforced in `angular.json` (950 kB initial warning, 1.2 MB error).

## Known CI hardening opportunities (not yet done)

- `npm install` → `npm ci` for reproducible installs against the committed lockfile.
- Pin toolchain versions (`global.json` for the SDK, `engines` in `package.json` for Node).
- Add `dotnet format --verify-no-changes` as an explicit style gate.

See [`../REMEDIATION-SUMMARY.md`](../REMEDIATION-SUMMARY.md) for what has already been applied.
