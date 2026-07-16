# Business rules — analysis engine

> Authoritative source for architecture *rules* is [`../AGENTS.md`](../AGENTS.md). This page is a factual map of every analysis the backend runs and the exact business rules (thresholds, formulas) currently coded — it does not grant exceptions to the contract. Keep it in sync whenever you change a threshold, formula, or add/remove an analysis in `BackPredictFinance.Services/ClientFinanceServices/` or `BackPredictFinance.Patterns/`.

## Orchestration pipeline

Entry: `POST api/ClientFinance/analysis/run` → `ClientAnalysisOrchestrator`. Fixed order (documented in code, reversing it breaks consistency):

1. Deterministic pattern execution (`DeterministicAnalysisExecutionService`)
2. Global volume confirmation, applied to every executed pattern
3. Risk evaluation per pattern (`RiskEvaluationService`)
4. Outcome determination: 0 compatible patterns → `NoCrediblePattern`, 1 → `CrediblePatternFound`, >1 → `MultipleCompatiblePatterns`
5. Pedagogical explanations (including discarded patterns)
6. Recommendation policy (`RecommendationPolicyService`)
7. Snapshot persistence (success **or** failure — persisted before the exception is rethrown)
8. Risk/technical context rebuilt from the **persisted record**, not the request (earnings date can be enriched during persistence)

`AnalysisRequestCompatibilityResolver` resolves the request first: `AsOfDate` is pinned to the **last known quote date from the provider**, never `UtcNow` (no future data leakage). History window = `AsOfDate` minus the **max** lookback across all resolved patterns. Asset enrichment is non-destructive (a field is only overwritten if empty in DB); default currency `EUR`.

`DeterministicAnalysisExecutionService` merges all executed patterns: display order = `IsPrimary` desc, `Confidence` desc, `Probability` desc. Model status aggregation is optimistic (one `Go` artifact makes the whole status `Go`). Reference candle series = the pattern that produced the most candles.

`FreshnessClassifier` classifies data freshness by **trading days** (weekends + Euronext closing holidays excluded — fixed dates + Easter via the Gauss algorithm): `<= 1` → `Fresh`, `1-3` → `Aging`, `>= 4` → `Stale`, no date → `Missing`. `FallbackPatternMarketDataProvider` falls back to the last persisted candles only if their age is within `MarketDataOptions.DegradedModeMaxSnapshotAgeHours`; otherwise the fallback is refused and the original exception is rethrown.

## Pattern detection (`BackPredictFinance.Patterns/Definitions/`)

Eight chart patterns, fully deterministic geometric rules — no ML. Thresholds are expressed as ATR multiples so they scale with each instrument's volatility.

**Shared engine** (`PatternTechnicals.cs`, `PatternThresholds.cs`, `BulkowskiReliability.cs`):
- ATR: Wilder smoothing, period 14. Volatility floor = `max(ATR, price * 0.001)`.
- Pivot fractal half-window = 3; anti-look-ahead: the last 3 candles of a series can never be a confirmed pivot (confirmation window must be fully in the past). Candles are always re-sorted chronologically before detection, regardless of provider order.
- Breakout margin = `0.25 * ATR`. Price-equality tolerance (double top/bottom, H&S) = 6%. Volume-expanding confidence bonus = `+0.05`.
- Requested candle count = `clamp(max(calendar span, pattern minimum), minimum, 500)`.
- Two distinct scores per pattern: **probability** = a fixed Bulkowski reliability constant per pattern (never recomputed), **confidence** = recomputed per candle from current geometry. Confidence label: `>=0.80` HIGH, `>=0.60` MEDIUM, `>=0.35` LOW, else VERY_LOW. Reliability label: `>=0.70` FIABLE, `>=0.55` MODERE, else FAIBLE.
- `SupportResistanceDetector`: pivots grouped by ATR-wide price bins, zone kept from 2 touches, capped at 8 zones. **Indicative overlay only, not a pattern signal.**

| Pattern | Family | Probability (Bulkowski) | Lookback | Min candles |
|---|---|---|---|---|
| Inverse Head & Shoulders | Bullish reversal | 0.71 (highest) | 12mo | 80 |
| Rectangle Continuation | Continuation | 0.68 | 6mo | 44 |
| Bull Flag | Bullish continuation | 0.67 | 6mo | 40 |
| Bear Flag | Bearish continuation | 0.67 | 6mo | 40 |
| Double Bottom | Bullish reversal | 0.65 | 12mo | 60 |
| Double Top | Bearish reversal | 0.64 | 12mo | 60 |
| Symmetrical Triangle | Continuation | 0.54 | 6mo | 48 |
| Head & Shoulders | Bearish reversal | 0.51 (lowest) | 12mo | 80 |

### Bull Flag / Bear Flag Continuation
Window: last 22 candles, positionally split 12 (pole) + 10 (flag). Pole move `>= 8%` (`>= 12%` = strong, bonus); flag height `<= 18%` of avg close; retracement `<= 60%` (`<= 40%` = tight, bonus); consolidation slope `<= 0.10 * ATR`. Breakout margin `0.25 * ATR`. Target = measured move from the broken boundary + pole height. Invalidation = the opposite boundary. Confidence additive: base 0.15 + 0.25 (structure) + 0.15 (strong pole) + 0.10 (tight retracement) + 0.15 (breakout) − 0.10 (opposite breakout simultaneously). Confirmed → confidence floor 0.76; forming → floor 0.52. Opposite breakout or retracement > 60% → invalidated, confidence forced to 0.20.

### Double Bottom / Double Top
Two pivots (fractal N=3) within 6% of each other, `>= 3` candles apart, intermediate rebound `>= 10%` of figure height. Neckline = highest/lowest **close** (not high/low) between the extremes. Double Bottom scans pivots oldest→newest; Double Top scans newest→oldest (deliberate divergence, documented in code). Target = neckline ± figure height. Invalidation = the opposite extreme. Double Bottom confidence is **tiered by phase** (forming 0.65 / confirmed 0.80, +0.05 volume); Double Top confidence is **additive** (base 0.15 + 0.35 + 0.15 + 0.05 volume, capped at 0.80/0.65).

### Head and Shoulders / Inverse Head and Shoulders
Three consecutive pivots, head depth `>= 10%` of figure height, shoulder temporal symmetry in `[0.833, 1.2]` (candle-count ratio). Neckline = average of the two intermediate troughs (H&S) or peaks (Inverse H&S, since the breakout is upward — guarded by `neckline > headLow`). Target = neckline ± figure height. Invalidation = the head level. Confidence tiered by phase: confirmed 0.80 / forming 0.65 / else 0.50, +0.05 volume.

### Rectangle Continuation / Symmetrical Triangle Continuation
Direction comes **not from the figure itself** but from a prior trend measured on the 20 candles immediately before the figure window (24 candles) — two disjoint, never-overlapping windows. Prior trend requires `>= 1.5 * ATR` move over those 20 candles; if none, the pattern is neutral and `IsCompatible = false`. A breakout **opposite** to the prior trend invalidates directly (confidence forced to 0.20). Rectangle: near-horizontal boundaries (`slope <= 0.10 * ATR`), `>= 2` touches per boundary (tolerance `0.30 * ATR`), relative height `2.5%-25%` of avg close; target = broken boundary ± range height. Triangle: bilateral compression via linear regression on highs/lows (opposite slopes + narrowing second half vs first half), boundaries projected at the **last** candle (avoids first-candle wick bias); target = breakout price ± starting triangle height.

## Confidence, risk and recommendation

`RiskEvaluationService`:
- Volume confidence adjustment: ratio (last candle volume / 20-candle avg) `>= 1.5` → Strong, `+0.05`; `<= 0.7` → Weak, `-0.05`; else Neutral, no change.
- ATR risk plan: `stop = price - 1.5 * ATR`; `target1 (RRR=1) = entry + |entry - stop|`; `target2 (RRR=2) = entry + 2 * |entry - stop|`; position size = `1% risk budget / stop distance %`; reward/risk ratio = `null` if risk or reward `<= 0`.
- `TechnicalIndicators.cs`: RSI(14) Wilder smoothing (oversold `<=30`, overbought `>=70`), MACD(12/26/9), volatility regime from 20-day log-return stdev (`<0.008` Low, `>0.022` High — regime warning only in High).

`TradingRecommendationService`: action `Buy` requires a confirmed bullish phase **and** confidence `>= 0.60`; `Sell` requires confirmed bearish phase **and** confidence `>= 0.60`; any invalidated phase → `Hold`, never actionable regardless of confidence. Risk level shown: not actionable → `Information`; else confidence `>=0.75` Low, `>=0.45` Moderate, else High.

`RecommendationPolicyService` maps the raw action to the final `RecommendationKind`, contextualised by holding status (never symmetric — see [`../AGENTS.md`](../AGENTS.md) "Non-negotiable product rules"):

| Holds? | Raw action | Final kind |
|---|---|---|
| Yes | Buy | Reinforce |
| Yes | Sell | Sell |
| Yes | Hold, invalidated/completed status | Wait |
| Yes | Hold, else | Hold |
| No | Buy | Buy |
| No | Sell | Monitor (never suggests short-selling) |
| No | Hold, active phase | Monitor |
| No | Hold, else | Wait |
| any | pattern not compatible / no credible pattern | Wait (always wins) |

`ConfidenceBreakdownAssembler`: 3 criteria (structure compatible / pattern validated / invalidation not triggered), each `Met`/`Partial`/`Absent` — an unrecognised intermediate state always falls back to `Partial`, never presented as definitively met or absent. `ActionPlanGenerationService`: max 3 steps, each reformulating an already-computed value (never introduces a new number); executable steps only when outcome is `CrediblePatternFound`/`MultipleCompatiblePatterns`.

## Technical indicators (`ClientFinanceServices/Indicators/TechnicalIndicatorsService.cs`)

Independent snapshot view over the last 250 candles (separate implementation from the pattern engine's `TechnicalIndicators.cs`, no shared code).

| Indicator | Params | Signal rule |
|---|---|---|
| RSI | 14 periods, Wilder | `>=70` Overbought, `<=30` Oversold |
| MACD | EMA 12/26, signal 9 (34 candles min) | MACD `>=` signal → Bullish |
| Bollinger | SMA 20 ± 2 stdev | outside band → alert |
| Moving averages | SMA 20/50/200 | price vs each MA (+1/-1 vote) |
| OBV | directional volume cumulation | `null` if `<50%` of candles have volume |

Synthesis: each available family votes bullish/bearish, `score = bullish - bearish` (-4..+4): `>=3` Strongly bullish, `2` Bullish, `1` Slightly bullish, `0` Mixed, `-1` Slightly bearish, `-2` Bearish, `<=-3` Strongly bearish.

## Fundamentals

`ClientFinanceFundamentalsService` (raw sheet): formatting only, no scoring. Dividend yield/ROE/operating margin `* 100`. Provider failure → always `404`, never a propagated exception.

`FundamentalScoringService` (composite PEA percentile score, `POST api/ClientFinance/fundamentals/score`): compares each stock to the confirmed-eligible PEA universe distribution, per metric, **within-sector percentile** with automatic fallback to the global universe (unknown sector, or sector sample `< MinimumSectorSampleSize`). 6 equal-weight categories: profitability (ROE, operating margin), liquidity (current ratio), debt (debt/equity, lower=better), valuation (PER/PEG/P-B, lower=better), dividend (yield), growth (revenue/earnings growth). Category score = average percentile of available metrics; total score = average of available categories, multiplied by coverage (`categories present / 6`) if the coverage penalty is enabled. Usable only if: universe member, `ConfirmedEligible` PEA status, and `>= MinCategoriesRequired` categories available (1-6, default 3). See `Documentation/api-endpoints.md` for the exact policy fields — as of this writing they are still hardcoded in `FundamentalScoringPolicyDefaults`/`FundamentalScoringService`; check `AdminScoringPolicyController` for the current admin surface before assuming they remain code-only.

## Portfolio

`PortfolioHoldingCalculator`: FIFO lot consumption (oldest buy consumed first by a sell). Falls back to a simple net calculation (bought − sold, average cost from gross buys) only if a sell exceeds the FIFO stock available (data inconsistency), rather than throwing.

`PortfolioAllocationService`: sector/country/currency breakdown + concentration (Herfindahl-Hirschman index) + benchmark comparison (`^FCHI`/`URTH`/`ACWI`, first found in DB). Line alert `>15%`, sector alert `>30%`, groups `<3%` merged into "Autres" beyond 8 distinct groups. HHI `>0.25` Concentrated, `>0.10` Moderate, else Diversified. Position/cost values are converted to EUR via `IClientFinanceAssetSupportService.GetForexRateToEurAsync`; when `portfolioId` is provided, positions are scoped to that portfolio via `PortfolioHoldingCalculator` on portfolio-filtered transactions, otherwise the global (all-portfolios, archived included) truth is used — mirrors `UserAsset.Quantity` semantics.

`PortfolioRiskMetricsService` (`GET api/ClientFinance/portfolio/{id}/risk-metrics`): needs `>= 20` valid daily returns. TWR = `Π(1+rᵢ) - 1`. Annualized volatility = `stdev(returns) * sqrt(252)`. Max drawdown = min((value - running peak) / peak). Transaction days are excluded from the return series (capital movement isn't a market return). Sharpe ratio formula/risk-free treatment: see the live code — this is an area under active revision, do not assume it matches an older version of this page.

## Screener, tax, alerts

**Screener** (`ScreenerService.cs`): page size clamped 1-100, sort column whitelist (11 columns), CSV export capped at 5,000 rows (`;` separator, UTF-8 BOM, invariant-culture numbers), metadata cached 1h. Fundamental filters exclude assets with no fundamentals snapshot.

**Tax** (`TaxService.cs`, `GET api/ClientFinance/tax-summary?year=`): weighted-average-cost realized gains, PFU 30% (non-PEA or PEA `<5` years, `years = int((Dec-31-of-year - firstTxDate) / 365.25)`), 17.2% (PEA `>=5` years, social contributions only). Losses are **not** offset against gains (taxable base always `>= 0`); no brokerage fees in the average cost; no loss carryforward.

**Alerts** (`ProactiveAlertEmitter.cs`, `InstrumentWatchJob.cs` 06:00 UTC daily, `SignalOutcomeEvaluationJob.cs` 03:00 UTC daily): 3 triggers (`PatternStateChange`, `LevelCrossed`, `DataStale`). Deduplication = max 1 notification/day/user/asset/trigger, enforced both in application code and via a filtered unique DB index — a race is a silent no-op, never a thrown error. Manual creation endpoint: `POST api/ClientFinance/alerts` (`ClientFinanceAlertsController`).

## Ex-post, dashboard, history

`ExPostStatisticsService`: real historical win rate per pattern, only over signals reaching a terminal state (target hit / invalidation hit / miss) — **selection bias explicitly acknowledged**: still-open signals are excluded. Minimum sample 20; Wilson 95% confidence interval (more reliable than Wald on small samples). Grouped by pattern **and** by whether an earnings date fell within the evaluation window (keeps "pure pattern" and "earnings-perturbed" populations separate).

`SignalDirectionalScanEvaluator`: if a single candle touches both target and invalidation, **invalidation is always tested first** — a deliberately conservative bias for ex-post evaluation.

## Endpoint reference

See [`api-endpoints.md`](api-endpoints.md) for the full controller list. All `ClientFinance*` routes are under `api/ClientFinance`, `[Authorize(Policy = "Bearer")]`, unless noted. `api/Trading/predict*` returns `410 Gone` — retired, fully replaced by `POST api/ClientFinance/analysis/run`.

## Known gaps / accepted derogations

- Sharpe ratio risk-free treatment: under active revision (see `PortfolioRiskMetricsService.cs` directly, this page intentionally does not pin the formula to avoid going stale).
- `AssetTransaction` soft-delete: under active revision (see `AssetTransaction.cs` / `ClientFinanceTransactionService.cs` directly).
- Fundamental scoring policy administration: under active revision (see `AdminScoringPolicyController.cs` directly).
- Euronext holiday calendar covers fixed closing days + Easter (Good Friday, Easter Monday) only — no partial-day sessions modelled.
