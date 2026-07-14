# V1 functional architecture — screen by screen

## Purpose

This file defines the human-facing functional architecture of V1.
It exists to prevent product drift between:
- technical pattern analysis
- fundamental scoring
- PEA eligibility exposure
- portfolio contextualization
- parameter-by-parameter pedagogy
- admin-owned product truth

V1 must help a beginner answer five distinct questions:
1. What does the market show now?
2. Does this instrument deserve attention?
3. What does each visible parameter mean?
4. What does this imply for my own situation?
5. What truth is governed by the product and not guessed by the UI?

This file is written for humans first.
The agent-operating implications are summarized in `Doc/ai/`.

## Scope lock

Current V1 functional scope remains restricted to active French listed equities already inside the supported product universe.
ETF support is out of V1 scope and reserved for V2.
The architecture may remain extensible for later ETF-specific policies, but no active V1 screen may imply ETF runtime support.

This means:
- the functional architecture may mention future ETF extensibility
- the current V1 user promise must stay coherent with French-equity scope
- no screen may silently present ETF support as already implemented

## Global reading model

The product must present each instrument through three complementary readings:
- market reading
- support reading
- personal-situation reading

A fourth transversal reading is required for pedagogy:
- parameter reading

### Market reading
Answers:
- is a continuation pattern forming, confirmed, invalidated, or absent
- what are the main compatible scenarios
- what are the invalidation and risk hints

### Support reading
Answers:
- how the instrument scores fundamentally inside the active universe
- whether the score is complete or partial
- whether PEA eligibility is confirmed, ineligible, or unknown

### Personal-situation reading
Answers:
- what the current analysis implies for the user's own context
- how the guidance differs for a held position versus a watched instrument

### Parameter reading
Answers:
- what each metric means
- how to read its current value
- why it matters
- what it implies in the user's own situation

## Screen 1 — Home dashboard

### Goal
Provide a calm and readable priority surface for a beginner user.

### Mandatory visible blocks
- watchlist items worth reviewing
- portfolio positions worth reviewing
- recent analyses
- incomplete or non-evaluable items
- recent meaningful changes since previous snapshots when available

### Allowed visible signals
- latest technical status
- latest visible main pattern
- presence of compatible alternatives
- latest pedagogical advice summary
- latest fundamental profile summary when available
- PEA status
- explicit incomplete-data states

### Forbidden behavior
- no recomputation of business truth in the UI
- no hidden aggregation into one magical final score
- no suppression of uncertainty states

## Screen 2 — Watchlist

### Goal
Help the user prioritize which instruments deserve attention.

### Minimal row content
- instrument identity
- latest known price and freshness
- technical analysis state
- fundamental profile summary
- data coverage state
- PEA status
- latest advice summary

### Explicit V1 rule
The watchlist must not expose any `convergence flag` in V1.
Technical reading, support reading, and recommendation summary must stay visible as distinct named blocks instead of being collapsed into a synthetic convergence state.

### Why this screen is central for the fundamental capability
The watchlist is the most natural V1 place to use the Excel-derived scoring logic because it improves prioritization without pretending to decide on behalf of the user.

## Screen 3 — Portfolio

### Goal
Show the user's held positions through the lens of the current analysis.

### Minimal row content
- quantity held
- average cost / PRU
- current market reading summary
- support reading summary
- situation-aware advice summary
- invalidation / risk hint summary

### Functional rule
Portfolio context may influence recommendation and explanation.
Portfolio context must not change pattern detection truth.

## Screen 4 — Instrument detail

This is the central V1 screen.

### Mandatory fixed sections
1. instrument summary
2. market reading
3. support reading
4. parameter reading
5. what this means for me
6. links to simulation and history

### 4.1 Instrument summary
Must display at least:
- instrument name
- ticker
- asset type
- country / market perimeter information
- PEA status
- data freshness
- analysis availability state

### 4.2 Market reading
Must answer: "What does the market show now?"

Must display at least:
- visible main pattern
- compatible alternative patterns
- pattern status
- confidence level
- validation / invalidation state summary
- invalidation level if available
- risk / reward hint if available
- deterministic pedagogical summary

### 4.3 Support reading
Must answer: "Does this support deserve attention?"

Must display at least:
- scoring version
- active universe id
- PEA eligibility status
- category scores
- coverage ratio
- composite score only when allowed by the scoring contract
- missing categories and missing metrics summary
- deterministic support summary

### 4.4 Parameter reading
Must answer: "What does each visible parameter mean?"

The screen must let the user inspect parameters category by category.
It must not reduce all pedagogy to a final composite score.

For each parameter shown to the user, the UI must provide access to four layers:
1. definition
2. how to read the current value
3. why the parameter matters
4. what this implies for the user's own situation

### 4.5 What this means for me
Must answer: "What does this imply in my own situation?"

If the user does not hold the instrument, allowed action verbs remain:
- Monitor
- Wait
- Buy

If the user holds the instrument, allowed action verbs remain:
- Monitor
- Wait
- Hold
- Reinforce
- Lighten
- Sell

This section must remain recommendation-oriented, not execution-oriented.

## Screen 5 — Parameter detail

### Goal
Turn one metric into an understandable pedagogical explanation.

### Mandatory visible sections
- parameter name
- simple definition
- role in the category
- how to read the current value
- limits of interpretation
- what the value supports
- what the value does not prove
- implication for a user without a position
- implication for a user with a position

### Why this screen exists
The product does not only rank instruments.
It helps a beginner understand what is being observed and why.

## Screen 6 — Analysis result

### Goal
Present the outcome of a fresh analysis execution.

### Must display
- executable or non-executable business outcome
- visible main pattern
- compatible alternatives
- recommendation summary
- risk summary
- support reading availability state
- PEA availability state
- snapshot id or history link when persisted

### Functional rule
Business-level non-executable outcomes must be visible as first-class product states, not buried as vague transport errors.

## Screen 7 — Simulation

### Goal
Help the user think through simple scenarios.

### Minimal inputs
- hypothetical entry price
- position size
- invalidation level
- objective level
- fees when available
- current position state

### Minimal outputs
- theoretical downside to invalidation
- theoretical upside to objective
- risk / reward ratio
- simple impact on the user's position

### Forbidden behavior
- no price prediction promise
- no pattern recomputation by the UI
- no transformation of a fundamental score into a timing or target-price truth

## Screen 8 — Analysis history

### Goal
Let the user review persisted analysis snapshots over time.

### Must display
- analysis timestamp
- version information
- visible main pattern at that date
- compatible alternatives at that date
- recommendation at that date
- support reading at that date when persisted
- PEA status at that date when persisted
- business outcome at that date

### Functional rule
History is a persisted reading surface.
It must not reconstruct the past from current wording alone.

## Screen 9 — Snapshot comparison

### Goal
Help the user compare two readings of the same instrument.

### Must compare
- market reading changes
- support reading changes
- recommendation changes
- explicit non-comparability states when versions or coverage differ materially

## Screen 10 — History

### Goal
Provide a simple persisted feed of recent executed readings and access to past snapshots.

### Must display
- instrument
- date
- business outcome
- visible main pattern
- recommendation summary
- support-reading availability
- PEA availability

## Screen 11 — Learn

### Goal
Explain clearly what the product currently supports and how to read the visible concepts.

### Must display
- current V1 market perimeter
- current active patterns
- current daily granularity scope
- current support reading perimeter
- current limits and deferred areas
- reminder that the application provides analysis and guidance, not execution

### Functional composition note
Detailed support-reading explanations, PEA explanations, and parameter pedagogy may live as sections or subviews of the central instrument-detail and learn surfaces.
They do not need dedicated first-rank screens in the V1 web architecture.

## Functional anti-drift rules

### Rule 1 — no single mega-score
No screen may merge technical pattern analysis, fundamental support reading, PEA truth, and portfolio recommendation into one hidden final score.

### Rule 2 — no parameter decides alone
A single parameter may inform the support reading.
A single parameter must never become a final recommendation by itself.

### Rule 3 — no hidden ETF expansion
ETF support is reserved for V2.
The V1 UI must not imply that ETFs are already part of the active runtime perimeter.

### Rule 4 — no frontend-owned explanation truth
Deterministic explanations must stay backend-owned or admin-governed according to the corresponding contracts.

### Rule 5 — uncertainty must be shown
The UI must display missing, unknown, unavailable, unsupported, and non-comparable states explicitly when relevant.
