# V1 web architecture lock — user + admin + UI wording

## Purpose

This document freezes the V1 web architecture for the user-facing product and the admin/back-office product.
It also freezes the UI wording discipline so agents and developers do not create drift between:
- business truth,
- screen structure,
- explanation wording,
- recommendation wording,
- admin-owned governed truth.

This file is intentionally normative.
If another UI document conflicts with it, this file wins unless a newer explicit V1 decision replaces it.

---

## 1. Scope lock

Current V1 active runtime scope:
- French listed equities only
- daily analysis only
- deterministic backend-owned business truth
- no ETF runtime support in V1
- no execution / broker behavior
- no AI-authored mandatory wording

V2 posture:
- ETF support may be added later
- the architecture must remain extensible for ETF-specific policies
- no V1 screen or wording may imply that ETF support already exists

---

## 2. Product split

The web product is split into two distinct domains:

### 2.1 User domain
Purpose:
- help a beginner understand what the market shows
- help a beginner understand what the instrument looks like fundamentally
- help a beginner understand what each visible parameter means
- help a beginner understand what the analysis implies for their own situation
- help a beginner track watchlist, portfolio, analyses, and history

### 2.2 Admin domain
Purpose:
- govern product truth
- govern support-reading and PEA truth
- govern parameter explanation truth
- govern wording versions
- expose audit and data-quality visibility
- prevent hidden frontend-owned product logic

Rule:
- admin is not a technical utility only
- admin is part of the functional architecture because V1 depends on governed product truth

---

## 3. Global reading model

Every instrument in V1 must be readable through four complementary readings:

### 3.1 Market reading
Answers:
- what continuation pattern is forming, confirmed, invalidated, completed, or absent
- what alternative compatible patterns exist
- what the main risk hints are

Owned by:
- technical analysis backend truth

### 3.2 Support reading
Answers:
- how the instrument scores fundamentally inside the active V1 universe
- whether the score is complete or partial
- whether PEA eligibility is confirmed, ineligible, or unknown

Owned by:
- support-reading backend truth

### 3.3 Parameter reading
Answers:
- what each visible parameter means
- how to read the current value
- why it matters
- what it implies in the user’s own situation

Owned by:
- backend explanation payloads generated from the governed parameter dictionary

### 3.4 Personal-situation reading
Answers:
- what the current analysis implies for the user’s own position or absence of position
- how the recommendation differs when the instrument is already held

Owned by:
- recommendation policy backend truth

Hard rule:
- these four readings must not be collapsed into one opaque score or one magical summary sentence

---

## 4. Information architecture — user web app

## 4.1 Global user navigation

Primary user navigation:
- Home
- Watchlist
- Portfolio
- Analysis
- History
- Learn
- Account

Secondary contextual navigation:
- Instrument detail
- Parameter detail
- Snapshot comparison
- Simulation

Rule:
- user navigation must remain beginner-readable
- technical implementation structure must not leak into navigation labels

---

## 5. User screens

## 5.1 Home dashboard

### Goal
Provide a calm, prioritized, beginner-readable overview.

### Mandatory blocks
- Watchlist to review
- Portfolio positions to review
- Recent analyses
- Non-evaluable or incomplete items
- Quick entry to search or analyze an instrument

### Allowed visible information
- instrument name
- last known analysis state
- last visible recommendation summary
- support-reading summary if available
- PEA status if available
- freshness indicator if relevant

### Forbidden behavior
- recompute business truth
- merge all readings into one score
- hide uncertainty states

### Primary actions
- Open watchlist
- Open portfolio
- Analyze an instrument
- Open recent analysis
- Open history

---

## 5.2 Watchlist

### Goal
Help the user decide what deserves attention.

### Mandatory row/card fields
- instrument
- latest market-reading summary
- latest support-reading summary
- PEA status
- data completeness or non-evaluable status
- last recommendation summary
- last analysis timestamp

### Allowed filters
- PEA status
- market-reading status
- support-reading availability
- held / not held
- recently analyzed / not recently analyzed

### Rule
The watchlist is a prioritization surface.
It is not a trading terminal and not a hidden ranking engine.

---

## 5.3 Portfolio

### Goal
Help the user understand current held positions.

### Mandatory row/card fields
- instrument
- quantity held
- average cost / displayed PRU
- latest market-reading summary
- latest support-reading summary
- latest recommendation summary
- risk hint visibility when available

### Rule
Portfolio context may change the recommendation and explanation.
Portfolio context must not change pattern detection truth.

---

## 5.4 Analysis entry screen

### Goal
Let the user select one V1-supported instrument and launch an analysis.

### Mandatory elements
- instrument search
- scope reminder: French equities only in V1
- user-friendly explanation of what analysis does
- submit action
- safe empty/loading/error states

### Forbidden wording
- “predict”
- “guarantee”
- “best trade”
- “automatic opportunity”

Preferred wording
- “analyze”
- “review”
- “understand”
- “explore”
- “see current signal”

---

## 5.5 Analysis result screen

### Goal
Present the result of one fresh analysis execution.

### Mandatory blocks
- Analysis outcome
- Market reading
- Support reading
- Personal-situation reading
- Parameter reading entry point
- Snapshot / history entry point
- Simulation entry point when relevant

### Rule
AnalysisOutcome is first-rank information.
A non-executable business outcome must be shown as a product state, not as a vague error.

### Allowed outcome wording examples
- “Analysis available”
- “Analysis not available for this instrument in the current V1 scope”
- “Not enough market history to produce a reliable result”
- “Support reading incomplete because some required data is missing”

---

## 5.6 Instrument detail screen

This is the central V1 screen.

### Mandatory sections
1. Instrument summary
2. Market reading
3. Support reading
4. Parameter reading
5. What this means for me
6. History / compare / simulate access

### 5.6.1 Instrument summary
Must show:
- instrument name
- symbol
- market
- asset type
- country
- V1 scope compatibility
- PEA status
- data freshness summary

### 5.6.2 Market reading
Must show:
- primary display pattern
- alternative compatible patterns
- PatternStatus
- confidence label
- validation summary
- invalidation summary
- risk hints when available
- deterministic pedagogical summary

Hard rule:
- `mainPattern` is display-primary only
- alternatives must not be erased

### 5.6.3 Support reading
Must show:
- support-reading status
- universe id
- scoring version
- category summaries
- coverage summary
- composite score only when allowed
- PEA status
- missing-data summary

Hard rule:
- support reading does not choose the action verb on its own
- support reading may qualify caution, confidence, completeness, or limitations

### 5.6.4 Parameter reading
Must show entry points by category and by parameter.

For each parameter detail entry, the user must be able to access:
- what it is
- how to read the current value
- why it matters
- what it implies for the user’s situation
- what it does not imply on its own

### 5.6.5 What this means for me
Must show:
- holding context
- recommendation action verb
- recommendation rationale
- caution or completeness qualifiers when relevant
- review horizon only when explicitly justified by backend truth

Hard composition rule:
- market reading remains the primary source for action-verb selection
- support reading may qualify caution, confidence, or completeness
- parameter reading may enrich the explanation
- no single parameter may choose the action verb alone

---

## 5.7 Parameter detail screen

### Goal
Turn one metric into beginner-readable understanding.

### Mandatory sections
- parameter label
- short definition
- current value
- simple interpretation
- why this matters
- limits of interpretation
- what this means if I do not hold the instrument
- what this means if I already hold the instrument
- what this does not justify by itself

### Rule
This screen exists to teach.
It must not become a pseudo-expert raw-data screen only.

---

## 5.8 Simulation screen

### Goal
Let the user explore a pedagogical scenario.

### Allowed inputs
- hypothetical entry price
- hypothetical size
- invalidation level
- target level
- fees if available
- current holding or no holding

### Allowed outputs
- theoretical downside
- theoretical upside
- risk/reward ratio
- simplified impact on the position

### Forbidden behavior
- no price prediction
- no guaranteed scenario
- no transformation of a support score into a target price
- no direct use of one parameter as a trade trigger

---

## 5.9 History screen

### Goal
Let the user browse past analyses.

### Mandatory information
- analysis date
- instrument
- AnalysisOutcome
- primary display pattern
- recommendation summary
- support-reading summary
- PEA status snapshot
- version visibility

### Rule
History must read persisted truth, not recompute the past from current rules in the UI.

---

## 5.10 Snapshot comparison screen

### Goal
Let the user compare two snapshots of the same instrument.

### Comparison dimensions
- market-reading evolution
- recommendation evolution
- support-reading evolution
- PEA status evolution
- completeness evolution
- comparability limits when versions changed

### Mandatory wording behavior
The UI must explicitly say when two values are not directly comparable.

---

## 5.11 Learn screen

### Goal
Support the pedagogical promise of the product.

### Minimum content
- how to read the four readings
- what a continuation pattern is
- what support reading means
- what PEA status means in product terms
- how to interpret recommendations without overconfidence
- what the app does not do

### Rule
The Learn area must remain aligned with the current backend truth and current wording dictionary.
It must not invent simplified rules that contradict the product contract.

---

## 6. Information architecture — admin web app

## 6.1 Global admin navigation

Primary admin navigation:
- Overview
- Instruments
- PEA registry
- Scoring policy
- Parameter dictionary
- Wording versions
- Snapshots
- Data quality

Rule:
- admin labels must describe governed truth, not internal technical services

---

## 7. Admin screens

## 7.1 Admin overview

### Goal
Provide product-operating visibility.

### Mandatory blocks
- active instruments count
- unsupported or inactive instruments count
- Unknown PEA count
- latest scoring version
- latest wording version
- recent non-executable analysis outcomes
- data-quality alerts
- snapshot freshness overview

---

## 7.2 Instrument registry

### Goal
Govern the product identity and support state of instruments.

### Mandatory fields
- instrument identity
- provider mapping
- asset type
- country
- universe membership
- support state
- active / inactive status
- freshness indicators

### Rule
This screen defines what the product can talk about.
It is not a raw provider-debug screen only.

---

## 7.3 PEA registry

### Goal
Govern PEA product truth.

### Mandatory fields
- instrument
- PEA status
- source type
- source reference
- checked date
- policy version
- status history when available

### Hard rule
`UNKNOWN` is a first-rank product state.
It must never be rendered as implicitly eligible.

---

## 7.4 Scoring policy

### Goal
Expose the support-reading scoring truth that governs V1.

### Mandatory fields
- active universe ids
- category list
- metric inclusion / exclusion
- metric direction
- minimum coverage rules
- coverage penalty rules
- scoring versions

### Hard rule
`payoutRatio` is informational only in V1.
If shown here, it must be clearly marked as non-composite in V1.

---

## 7.5 Parameter dictionary

### Goal
Govern beginner-facing parameter explanations.

### Mandatory fields per parameter
- stable id
- label
- short beginner definition
- expert definition when needed
- category
- display order
- interpretation guidance
- limitation guidance
- generic implication wording for not-held
- generic implication wording for held
- incomplete-data wording
- wording version
- active / inactive state

### Rule
The frontend must consume this governed truth.
It must not improvise parameter meaning locally.

---

## 7.6 Wording versions

### Goal
Provide visibility on the active wording truth.

### Must expose
- wording version id
- activation state
- activation date
- affected wording domains
- relation to scoring / recommendation / explanation versions when relevant

---

## 7.7 Snapshot audit

### Goal
Allow product-grade audit and traceability.

### Must expose
- snapshot identity
- instrument
- timestamps
- AnalysisOutcome
- market-reading payload summary
- support-reading payload summary
- recommendation snapshot
- policy versions
- comparison entry point

---

## 7.8 Data quality

### Goal
Expose what can degrade product reliability.

### Must expose
- missing support data rates
- missing category rates
- Unknown PEA rates
- stale data indicators
- repeated non-executable outcomes
- provider coverage issues

---

## 8. UI wording lock

## 8.1 Tone

The tone must be:
- calm
- explicit
- pedagogical
- non-promissory
- traceable
- beginner-readable

The tone must not be:
- sensational
- promotional
- pseudo-certain
- broker-like
- aggressive
- opaque

---

## 8.2 Hard wording rules

The UI must:
- describe what is observed
- describe what is incomplete
- describe what is implied for the user
- distinguish fact, interpretation, and advice
- avoid certainty inflation

The UI must not:
- imply guaranteed returns
- imply hidden prediction power
- imply legal or tax certainty from product heuristics
- imply that one metric alone decides
- imply ETF support in V1

---

## 8.3 Canonical wording domains

### Domain A — analysis execution
Preferred:
- Analyze
- Review analysis
- Analysis available
- Analysis not available
- Current signal
- Current reading

Forbidden:
- Predict
- Win
- Best move
- Guaranteed setup
- Sure opportunity

### Domain B — market reading
Preferred:
- Pattern forming
- Under monitoring
- Confirmed
- Invalidated
- Completed scenario
- Compatible alternative pattern
- No credible pattern
- Not enough history

Forbidden:
- Certain breakout
- Sure trend
- Guaranteed reversal
- Perfect setup

### Domain C — support reading
Preferred:
- Support reading
- Fundamental profile
- Partial reading
- Incomplete data
- Composite score unavailable
- PEA status confirmed
- PEA status unknown
- PEA status ineligible

Forbidden:
- Good stock
- Bad stock
- Safe stock
- Buy-worthy by score
- Guaranteed PEA eligibility

### Domain D — parameter reading
Preferred:
- What this metric means
- How to read this value
- Why this matters
- What this implies for me
- What this does not imply on its own

Forbidden:
- This proves the stock is great
- This means you should buy
- This guarantees quality
- This metric alone justifies an entry

### Domain E — recommendation wording
Canonical action labels:
- Monitor
- Wait for confirmation
- Buy
- Hold
- Reinforce
- Lighten
- Sell

Rules:
- the UI may render translated labels for the user
- the meaning must remain aligned with the backend code and holding context
- `Monitor` is not allowed for held positions
- `Hold`, `Reinforce`, `Lighten`, and `Sell` are not allowed for not-held positions
- `Wait for confirmation` may appear in both contexts

### Domain F — uncertainty and limits
Preferred:
- Not enough data
- Not available in the current V1 scope
- Incomplete reading
- This does not provide timing by itself
- This should be read with caution
- Not directly comparable

Forbidden:
- Error, when the real state is a business-level non-executable outcome
- Unknown but probably okay
- Missing but assumed positive
- Equivalent enough

---

## 8.4 Recommendation rationale wording rules

A recommendation rationale must:
- reference the current market-reading truth
- optionally reference support-reading caution or completeness
- optionally reference holding context
- remain shorter than a full educational article
- stay deterministic in V1

A recommendation rationale must not:
- invent hidden causal chains
- quote unsupported probabilities
- turn a support metric into a timing claim
- promise outcome

---

## 8.5 Parameter implication wording rules

For each parameter explanation, the UI wording must separate:
1. what the metric means,
2. how to read the current value,
3. why it matters,
4. what it implies for the user,
5. what it does not justify alone.

This five-part structure is mandatory for parameter detail screens and recommended for drawers/tooltips when space allows.

---

## 8.6 Business-state wording rules

When the backend exposes a business-level non-executable state, the UI must render it as a product state.
Examples:
- “This instrument is outside the current V1 scope.”
- “There is not enough market history to produce a reliable result.”
- “Support reading is incomplete because required data is missing.”

The UI must not collapse these states into:
- generic technical error
- empty screen
- silent fallback
- misleading positive assumption

---

## 9. Route architecture recommendation

## 9.1 User routes
Recommended user route set:
- `/`
- `/watchlist`
- `/portfolio`
- `/analysis`
- `/analysis/:instrumentId/result/:analysisId`
- `/instrument/:instrumentId`
- `/instrument/:instrumentId/parameter/:parameterId`
- `/instrument/:instrumentId/history`
- `/instrument/:instrumentId/compare`
- `/instrument/:instrumentId/simulate`
- `/learn`
- `/account`

## 9.2 Admin routes
Recommended admin route set:
- `/admin`
- `/admin/instruments`
- `/admin/pea`
- `/admin/scoring`
- `/admin/parameters`
- `/admin/wording`
- `/admin/snapshots`
- `/admin/data-quality`

Rule:
- route names must stay explicit and domain-readable
- no route should encode internal service names or temporary technical concepts

---

## 10. Components and layout zones

## 10.1 User layout zones
Recommended persistent zones:
- top navigation
- page header
- main content
- contextual help area
- status/empty/error area

## 10.2 Admin layout zones
Recommended persistent zones:
- admin navigation
- page header with current governed version visibility
- data table / form area
- audit / side panel area
- validation status area

---

## 11. Final lock rules

- The user web app must remain beginner-readable.
- The admin web app must remain truth-governance oriented.
- No frontend screen may create business truth that belongs to the backend.
- No UI wording may suggest guarantees, predictions, or unsupported certainty.
- No V1 screen may imply ETF runtime support.
- Parameter explanations must remain deterministic and governed.
- Recommendation wording must remain aligned with holding context rules.
- AnalysisOutcome must remain visible as a first-rank business concept.
- When two wording options conflict, prefer the more explicit and less promissory wording.

## 12. Suggested file placement

Recommended documentation path:
- `Doc/product/11_v1_web_architecture_and_ui_wording_lock.md`

