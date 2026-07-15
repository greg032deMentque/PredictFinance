# Pitfalls — mistakes not to reproduce

Concrete traps found in this codebase, with the correct approach. **Read this before touching the analysis engine, persistence, config, or CI.** Each entry is a real bug or trap that has already cost time here.

Legend: 🔴 caused a production-impacting bug · 🟠 silent correctness/quality trap · 🔧 infra (CI / config / secrets).

---

## Analysis engine & business logic

### 🔴 Ex-post signal evaluation must respect pattern direction
`SignalOutcomeEvaluationJob` and `ClientFinanceDashboardHistoryService` compare candles to target/invalidation prices. The naive form assumes a **bullish** setup (`High >= Target`, `Low <= Invalidation`). For **bearish** patterns (BEAR_FLAG, DOUBLE_TOP, HEAD_AND_SHOULDERS) the target is *below* price and invalidation *above*, so the naive comparison fires `InvalidationHit` on the first candle — always wrong.
- **Do**: resolve `PatternDirection` from geometry (`PatternDirectionResolver`: `Target > Invalidation` ⇒ Bullish, `<` ⇒ Bearish, else Unknown) and invert High/Low comparisons for bearish. Use the shared `SignalDirectionalScanEvaluator` — do not re-implement the scan in each caller.
- **Don't**: build a static `patternId → direction` table. `RectangleContinuation` / `SymmetricalTriangleContinuation` are trend-following; their direction depends on the detected trend at runtime, not on the pattern id.

### 🔴 Portfolio FIFO reconstruction must match the quantity it is checked against
`UserAsset.Quantity` is a **global** truth (all portfolios, archived included). Reconstructing FIFO lines while filtering out archived portfolios (`ExcludeArchivedPortfolios()`) and then comparing to `UserAsset.Quantity` throws an inconsistency exception → 500 on every analysis of that symbol.
- **Do**: reconstruct over the **same** perimeter as the quantity you compare against. For the global aggregate, include archived portfolios. Keep PRU / invested amount on the same perimeter as the displayed quantity.
- **Don't**: mix a filtered reconstruction with an unfiltered aggregate.

### 🔴 Never physically delete an `AssetTransaction`
Deleting a `Buy` that has already been consumed by later `Sell`s leaves the FIFO history inconsistent → a later analysis throws "a sell exceeds the bought quantity" → durable 500. Convention is soft-delete anyway.
- **Do**: replay FIFO without the candidate transaction and refuse the delete if history becomes inconsistent; prefer soft-delete (`IsDeleted`).

### 🟠 Aggregating multi-pattern model status optimistically hides warnings
Setting global `ModelStatus = Go` if *any* pattern is `Go` strips the limitation note/warnings for **all** patterns, including `NoGo` ones. Aggregate conservatively (NoGo wins) or keep status per pattern.

### 🟠 Don't throw the raw exception type across the API boundary
Throwing `InvalidOperationException` from a service surfaces as a generic 500 with no traceable business message. Throw `CustomException` with an HTTP status and a French business message. User-facing errors are **business messages only** — never a traceId, HTTP code, or technical string.

### 🟠 Guard the failure-persistence call in the orchestrator's catch
`PersistFailedAnalysisAsync` in a `catch` is itself DB work; if the DB is down its exception masks the original and the careful `CustomException` mapping never runs. Wrap it in its own try/catch with a log, no rethrow.

---

## Persistence & EF Core

### 🟠 Persist the aggregate root before its children
In snapshot persistence, adding `AnalysisRun` to the tracker then upserting candles — if a per-candle `SaveChanges` fails and you call `ChangeTracker.Clear()`, the not-yet-saved `AnalysisRun` is detached and lost, but the call returns a `PublicId` pointing at a phantom run → later 404. Persist the run first (its own `SaveChanges`), then children.

### 🟠 No sync EF queries in constructors
`FinanceDbContext` and `BaseService` run synchronous queries in their constructors (`CurrentUser`, `UserRoles`). `BaseService` is the parent of ~56 services, so one HTTP request pays N identical sync round-trips. Lazy-load async with a per-scope cache instead.

### 🟠 Batch candle upserts
Upserting candles one-by-one (a `FirstOrDefault` + `SaveChanges` per candle) is up to ~1000 round-trips per analysis. Load the window's existing timestamps in one query, partition insert/update in memory, one `SaveChanges`.

### 🟠 Include is dropped before a projection
`.Include(...)` followed by a `.Select(...)` projection is dead code — EF ignores the Include. Remove it (it misleads readers).

### Soft-delete is manual — don't forget the filter
There is no global `HasQueryFilter(!IsDeleted)`. Entities with `IsDeleted` (`Portfolio`, `UserScreenerPreset`, `FaqEntry`, `LegalCard`, `LearnTopic`, `GlossaryTerm`, `EducationArticle`) are filtered per-query. Any new read must add `!IsDeleted` or it will surface deleted rows.

### Migrations
- Never auto-generate a migration without explicit instruction; flag when one is needed.
- Never apply a migration to a database as part of a code change.
- Never `DropColumn`/`DropTable` in `Up()` without a data-conversion step (past migrations lost data doing this). Additive is safe; destructive needs a backup step.

---

## Contracts & architecture

### 🔴 Do not expose internal analysis contracts as ViewModels
`AnalysisResponseViewModel` exposing `PatternAssessmentContract` / `AnalysisRiskContext` etc. (types from `Common/AnalysisV1`) makes every internal engine change a breaking API change. Project to dedicated ViewModels via AutoMapper.

### `Common/AnalysisV1` is shrinking, not growing
It is a documented derogation to reduce, not extend. Don't add new types there; new analysis contracts go in `Patterns` or a dedicated project. (One deliberate exception was made — a single `Direction` field on `PatternAssessmentContract` — and it was documented.)

### No new `Services → ViewModels` coupling
The existing coupling is a derogation, not a target. Don't add more.

---

## Frontend

### 🟠 The frontend must not recompute business truth
PnL / PnL% were computed in Angular in three places. The backend owns business truth — expose the value from the API and render it. Same for tax totals, scoring, recommendation.

### 🟠 Enums cross the wire as strings — no ordinal fallbacks
The backend serialises enums as strings (`JsonStringEnumConverter`, PascalCase). Frontend code that maps enum **ordinals** (`{0:'Buy',1:'Sell',...}`) or sends `status=0` breaks silently when the C# enum is reordered. Use the string names on both sides.

### 🟠 Error display is business-French only
The central `apiErrorInterceptor` must never show a traceId, HTTP code, or backend technical string. TraceId goes to `console.error` only. Any component reading `err.error.message` and showing it verbatim is a leak.

### Bind objects in PascalCase
Objects that map to backend DTOs are PascalCase (to match the backend's `PropertyNamingPolicy = null`). camelCase request DTOs get silently ignored by the model binder.

### Subscriptions need `takeUntilDestroyed`
Many components `.subscribe(...)` without teardown. Always add `takeUntilDestroyed(this.destroyRef)`.

---

## Secrets & config (see [secrets-management.md](secrets-management.md))

### 🔴 Never commit a secret — not even once
Real secrets (SMTP password, JWT secret, salt) were committed and remain in git history. Rotation makes them useless; only history rewrite removes them. Keep secrets in user-secrets (dev) / env vars (staging-prod).

### 🔧 `dotnet ef` design-time does not load user-secrets
Its entry assembly is the ef tool, not the API, so user-secrets are not applied. Keep the **non-secret** dev connection string (LocalDB `Trusted_Connection`, no password) in `appsettings.Development.json` so migrations work; move only true secrets to user-secrets.

### 🔧 Test config must be reachable in CI
`appsettings.Testing.json` matched the `appsettings*.json` gitignore rule → absent in CI. It holds only placeholders (no secret), so it is force-tracked via a `.gitignore` exception. Don't re-ignore it.

---

## CI/CD (see [ci-cd.md](ci-cd.md))

### 🔧 Keep pipeline commands in sync with `package.json`
A pipeline called `npm run build:prod`, a script that never existed → the prod front pipeline could never build. Verify every `npm run X` exists.

### 🔧 Never commit build/tool caches
The `.dotnet` SDK cache (5606 files) was tracked. Keep `bin/`, `obj/`, `.dotnet/`, `node_modules/` out of git.

### 🔧 Test-gate exclusions must be narrow and documented
The old gate excluded whole authz test classes. If you must exclude a broken test, exclude it **at method level**, with a comment naming the root cause and a TODO. Never hide security tests.

### 🔧 Vulnerability scans are gates, not decoration
`npm audit` / `dotnet list package --vulnerable` are blocking on high/critical. Don't re-add `continueOnError: true`.

---

When you discover a new trap, add it here (and, if it changes the contract, in [`../AGENTS.md`](../AGENTS.md)). This page is how the next agent avoids repeating today's mistake.
