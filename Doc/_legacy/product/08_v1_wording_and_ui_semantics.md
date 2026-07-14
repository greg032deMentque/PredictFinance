# V1 wording and UI semantics

## Purpose

This document locks product vocabulary to avoid drift across code, API, documentation, and interface.

## Human-facing wording principles

- The product is pedagogical.
- The product is not prescriptive in a legal or regulated sense.
- The product explains what it sees and why it says it.
- The product must show explicitly when information is absent, insufficient, unsupported, or not confirmed.
- The product must not suggest that one score or one pattern alone guarantees future performance.

## Product wording for technical analysis

### Pattern display
- `mainPattern` = displayed primary pattern
- `alternativePatterns` = compatible alternative patterns
- never call `mainPattern` “the true pattern” or “the only pattern” if alternatives exist

### Pattern progression labels
Use stable beginner-facing labels only:
- `FORMING` -> En formation
- `MONITORING` -> À surveiller
- `CONFIRMED` -> Confirmé
- `INVALIDATED` -> Invalidé
- `COMPLETED` -> Terminé

### No-pattern state
When no credible pattern is retained, the wording must explicitly indicate that:
- no credible pattern is currently detected
- this is a valid business outcome
- the system is not forcing a weak interpretation

Recommended wording:
- `NO_CREDIBLE_PATTERN` -> Aucun pattern crédible n’est retenu pour le moment.

### Recommendation verbs
Recommended user-facing verbs:
- Surveiller
- Attendre
- Acheter
- Conserver
- Renforcer
- Alléger
- Vendre

Recommendation wording must reflect:
- technical state
- confidence and validation state
- portfolio context
- risk framing

## Product wording for fundamental scoring

### What the score means
Preferred wording:
- score fondamental
- classement relatif dans l’univers
- score composite
- couverture des données
- score indisponible

Forbidden wording:
- note absolue de qualité
- garantie
- action à acheter
- certifié PEA

### PEA eligibility wording
User-facing statuses:
- `CONFIRMED_ELIGIBLE` -> Éligibilité PEA confirmée
- `CONFIRMED_INELIGIBLE` -> Non éligible PEA confirmée
- `UNKNOWN` -> Éligibilité PEA non confirmée

Mandatory wording rule:
- `UNKNOWN` must never be rendered in a way that sounds implicitly positive
- the wording must keep the uncertainty explicit

### Composite-score availability wording
Recommended stable renderings:
- `AVAILABLE` -> Score composite disponible
- `INSUFFICIENT_COVERAGE` -> Score composite indisponible : couverture de données insuffisante
- `PEA_STATUS_UNKNOWN` -> Score composite indisponible : éligibilité PEA non confirmée
- `PEA_CONFIRMED_INELIGIBLE` -> Score composite indisponible : instrument confirmé non éligible PEA dans cet univers
- `UNSUPPORTED_UNIVERSE` -> Score composite indisponible : univers demandé non pris en charge
- `PROVIDER_DATA_UNAVAILABLE` -> Score composite indisponible : données fournisseur incomplètes ou indisponibles

## Closed wording for explicit negative and incomplete states

The UI and API-facing summaries must reuse these meanings consistently.

### Technical-analysis first-rank business outcomes
- `CREDIBLE_PATTERN_FOUND` -> a retained compatible pattern exists
- `MULTIPLE_COMPATIBLE_PATTERNS` -> several compatible patterns remain materially plausible
- `NO_CREDIBLE_PATTERN` -> no retained compatible pattern
- `INSUFFICIENT_DATA` -> the backend lacks enough valid analysis input to evaluate the V1 pattern set
- `UNSUPPORTED_INSTRUMENT` -> the instrument exists but is outside the analyzable V1 perimeter
- `UNSUPPORTED_CONTEXT` -> the requested technical-analysis context is outside the enabled V1 rules

### Technical-analysis second-rank explanatory causes
These labels may be shown as detail or diagnostics, but they must not replace the first-rank `AnalysisOutcome` taxonomy:
- `INSUFFICIENT_PRICE_HISTORY` -> not enough candle history to evaluate the requested pattern set
- `UNSUPPORTED_PATTERN_REQUEST` -> requested pattern identifiers are outside the enabled V1 set
- `INSTRUMENT_OUTSIDE_V1_SCOPE` -> instrument exists but is not analyzable in the V1 perimeter
- `MARKET_DATA_UNAVAILABLE` -> provider data needed for the analysis is unavailable

### Fundamental-scoring explicit states
- `INSUFFICIENT_COVERAGE` -> too few categories available for a reliable composite score
- `PEA_STATUS_UNKNOWN` -> PEA status is not confirmed, so the composite score stays unavailable in a PEA-scoped universe
- `PEA_CONFIRMED_INELIGIBLE` -> instrument is product-confirmed ineligible in the selected PEA-scoped universe
- `UNSUPPORTED_UNIVERSE` -> the backend does not support the requested universe id
- `PROVIDER_DATA_UNAVAILABLE` -> provider payload does not give enough valid raw fundamentals to compute the scoring output

## Vocabulary discipline for code and docs

### Preferred English identifiers in code
- `PatternAssessment`
- `PortfolioContext`
- `RecommendationPolicy`
- `PeaEligibilityStatus`
- `FundamentalScoreResult`
- `CoverageRatio`
- `UniverseId`
- `CompositeScoreStatus`

### Preferred French wording for product documents and UI
- score fondamental
- univers PEA actions françaises
- éligibilité PEA
- couverture des données
- pattern principal affiché
- patterns alternatifs compatibles
- recommandation pédagogique
- résumé pédagogique

## UI composition rule

When technical analysis and fundamental scoring are shown together:
- do not merge them into a single unnamed block
- clearly label the technical-analysis block
- clearly label the fundamental-scoring block
- make explicit that they answer different questions

Recommended section labels:
- Analyse technique
- Score fondamental
- Éligibilité PEA

## Anti-drift rule

Any new doc, API payload, UI wording, or implementation detail touching V1 analysis or scoring must reuse the vocabulary of this file unless a stronger explicit decision replaces it.
