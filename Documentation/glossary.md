# Domain glossary

Key terms an agent needs to read the code. The **canonical** product glossary lives in [`../Doc/`](../Doc) (product specs); this is a working subset.

| Term | Meaning |
|---|---|
| **Pattern** | A deterministic chart figure detected by the engine (e.g. `RectangleContinuation`, `BullFlagContinuation`, `DoubleTopReversal`, `HeadAndShoulders`). Each has geometry: target price, invalidation price, and a resolved **direction**. |
| **Direction** (`PatternDirectionEnum`) | Bullish / Bearish / Unknown, resolved from target-vs-invalidation geometry. Continuation patterns like Rectangle/Triangle are trend-following: direction depends on the detected prior trend, not the pattern id. |
| **Continuation vs Reversal** | Continuation patterns extend the prior trend; reversal patterns (DoubleTop, HeadAndShoulders, DoubleBottom, InverseHeadAndShoulders) signal a turn. |
| **Confidence** | The engine's confidence in a detected pattern. Governs the action taken. Distinct from reliability. Never merged with fundamental scoring into one opaque number. |
| **Reliability** | The historical/Bulkowski reliability of a pattern type. |
| **Recommendation** | The action (Buy / Sell / Hold / NonActionable) derived by `RecommendationPolicyService`, **context-dependent** on whether the instrument is held. |
| **Decision signal** | The actionable output tied to a recommendation; `IsActionable` gates whether a signal is tracked ex-post. |
| **Model status** (`ModelStatusEnum`) | Go / NoGo — whether the model produced a usable result (NoGo when no market data). |
| **Snapshot** (`AnalysisRun`) | The persisted, auditable record of one analysis, with full raw payload and versioned wording. |
| **Ex-post** | After-the-fact evaluation of whether a signal's target or invalidation was hit, feeding Wilson win-rate statistics. |
| **PEA** | *Plan d'Épargne en Actions* — French tax-advantaged equity account. PEA eligibility must stay explicit and traceable, never silently inferred. |
| **FIFO reconstruction** | Rebuilding open positions (quantity, average cost) from the ordered transaction history. Underpins portfolio context for analysis. |
| **Degraded mode** | When live market data is unavailable, analysis falls back to persisted candle snapshots within a freshness threshold. |
| **Watchlist / Portfolio** | A user's tracked instruments vs actually-held positions. `UserAsset.Quantity` is the global held quantity across all portfolios. |
| **Wording governance** | Versioned, reviewable recommendation/explanation text (`RecommendationWordingVersion` / `Scenario`) so user-facing wording is auditable. |

See also [data-model.md](data-model.md) for the entities behind these terms.
