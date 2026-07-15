# Data model & persistence

EF Core 10, SQL Server. Entities in `BackPredictFinance.Datas/Entities/`, context `FinanceDbContext`, migrations in `BackPredictFinance.Datas/Migrations/`. Model configuration is in `Datas/Context/ModelBuilderConfigurationExtensions.cs`.

## Entities by domain

| Domain (folder) | Entities |
|---|---|
| `Users` | `User`, `RefreshToken`, `UserAsset`, `Portfolio`, `AssetTransaction` (in `Market`), `UserNotification`, `UserScreenerPreset`, `Analytic` |
| `Market` | `Asset`, `AssetCandleSnapshot`, `AssetQuoteSnapshot`, `AssetFundamentalsSnapshot`, `AssetPeaEligibility`, `PriceHistory`, `AssetTransaction`, `IAssetSnapshot` |
| `Analysis` | `AnalysisRun`, `PatternAssessment`, `DecisionSignal`, `Recommendation`, `ModelSnapshot`, `SignalOutcome` |
| `Patterns` | `PatternDefinition`, `AnalysisConceptExplanation` |
| `Governance` | `RecommendationWordingVersion`, `RecommendationWordingScenario`, `ParameterDictionaryEntry` |
| `Content` | `FaqEntry`, `LegalCard`, `LearnTopic` |
| `Education` | `EducationArticle`, `GlossaryTerm` |
| `Audit` | `AuditTrail`, `AuditableEntity` |

## Key invariants (get these wrong and you break analysis)

- **`UserAsset.Quantity` is global** — the net quantity across *all* portfolios (archived included). It is not recomputed on archive. Any FIFO reconstruction compared against it must cover the same perimeter. See [pitfalls.md](pitfalls.md).
- **`AssetTransaction`** drives FIFO cost/quantity reconstruction (`PortfolioContextLoader`). Deleting one can silently corrupt history — prefer soft-delete and replay-validate.
- **`AnalysisRun`** is the persisted, auditable snapshot of one analysis (`RawPayload` = full JSON, `nvarchar(max)`). It carries pattern assessments, decision signal, recommendation, model snapshot. The `PublicId` is the client-facing id — it must exist in the DB before being returned.
- **`PatternAssessment.Direction`** (Bullish/Bearish/Unknown) is resolved from target/invalidation geometry and drives correct ex-post evaluation.
- **`SignalOutcome`** is the ex-post result (`TargetHit / InvalidationHit / StillOpen / TargetMiss / NotEvaluable`) used for Wilson win-rate statistics. Wrong direction handling pollutes these. Populated by the daily `SignalOutcomeEvaluationJob`; window = `DecisionSignal.HorizonDays` (default 30).
- **`DecisionSignal.EarningsDateUtc`** is resolved at persist time from the fundamentals provider; it feeds the ex-post stats grouping (`HasEarningsInWindow`) and the "résultats le …" warning. It is a signal field, not a fundamentals snapshot.
- **`User` carries RGPD state**: `DeletedAt` (soft account deletion via `DELETE api/Account/self`), consent flags (`AnalyticsConsent`, `MarketingEmailConsent`, `ProductImprovementConsent`, `ConsentLastUpdatedUtc`), and alert preferences. `Asset.Isin` (+ `LastProfileSyncUtc`) is unique-indexed.
- **`Analytic`** stores nominative request data (`Login`, `Ip`, `Request`, `Body`, `Referer`, `UserAgent`) with **no modelled retention/expiry** — an open gap tracked in the écart register [`../Doc/v1/06_ecarts_doc_code.md`](../Doc/v1/06_ecarts_doc_code.md) (A-10).

## Soft-delete

`IsDeleted` exists on `Portfolio`, `UserScreenerPreset`, `FaqEntry`, `LegalCard`, `LearnTopic`, `GlossaryTerm`, `EducationArticle`. There is **no** global query filter — every read filters `!IsDeleted` manually. Physical `DELETE` is not the convention.

## Migrations workflow

```powershell
# add (only when explicitly instructed)
dotnet ef migrations add <Name> --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API
# apply locally (Development env)
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API
# generate an idempotent script for staging/prod release
dotnet ef migrations script --idempotent --project FinanceBack/BackPredictFinance.Datas --startup-project FinanceBack/BackPredictFinance.API -o migrate.sql
```

Rules: never auto-generate without instruction; never apply as part of a code change; additive `Up()` is safe, destructive `Up()` (`DropColumn`/`DropTable`) needs a data-conversion/backup step. `dotnet ef` reads the connection string from `appsettings.Development.json` (non-secret LocalDB) — it does **not** load user-secrets.

## Serialization

Enums serialise as **strings** (`JsonStringEnumConverter`) with PascalCase (`PropertyNamingPolicy = null`), shared with the frontend. Reordering or renaming an enum is a breaking change on both sides.
