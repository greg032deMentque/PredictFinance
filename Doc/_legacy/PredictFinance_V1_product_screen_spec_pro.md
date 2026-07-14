# PredictFinance V1 — Product Screen Specification (Professional Detailed Version)

## Document status
This document is the **professional detailed replacement** for the compact `PredictFinance_V1_product_lock_screen_matrix.md`.

It is intended to support:
- product
- design
- frontend implementation
- backend API contract review
- admin / back-office implementation

It is intentionally **screen-driven** and **product-facing**.
It stays more detailed than the compact matrix, but more implementation-neutral than frontend architecture or component specs.

---

## 1. Purpose

The purpose of this file is to define, page by page:

- the dominant user question
- the product objective
- the required visible information
- the required actions
- the allowed and disallowed wording semantics
- the required state handling
- the routing expectations between screens
- the product constraints that must stay locked during implementation

This document is a **normative screen specification** for V1.

---

## 2. Source of truth used for this document

This specification is based on:
- the compact screen-lock matrix in `PredictFinance_V1_product_lock_screen_matrix.md`
- the locked V2 / V3 UI / UX specifications
- the latest visual package used as current baseline: `PredictFinance_V1_pro_UX_direct_corrections.zip`

Relevant locking rules already stated in the compact matrix remain valid here, especially:
- login-only public entry
- strict user/admin split after login
- held / not-held recommendation split
- `Help` as explanatory surface
- `Notifications` as prioritization and routing surface
- `Account` as self-service only
- `Admin` as governance and user management area fileciteturn8file0turn8file8

The broader semantic and anti-drift rules remain aligned with the normative V3 UI / UX specification, including:
- separation of market reading, support reading, PEA, recommendation, and data completeness
- persisted snapshot truth for history and comparison
- distinct handling for unknown, unsupported, unavailable, incomplete, insufficient, and invalidated
- held / not-held recommendation split wherever recommendation is shown fileciteturn8file3turn8file4turn8file7

---

## 3. Language rule

### 3.1 User-facing frontend language
All **user-visible frontend text** must be in **French**.

This includes:
- page titles
- subtitles
- chips
- CTA labels
- empty states
- error states
- help copy
- admin visible labels
- recommendations
- PEA wording
- history and comparison wording
- notifications wording

### 3.2 Internal technical naming
Internal technical identifiers may remain in English when appropriate, including:
- enums
- variable names
- DTO names
- route keys
- component names
- internal event names
- internal status codes

### 3.3 Hard rule
Internal English naming must **never leak as raw visible UI text**.

Example:
- allowed internally: `CREDIBLE_PATTERN_FOUND`
- required in UI: `Pattern crédible détecté`

This remains aligned with the locked wording model defined in the normative screen specs. fileciteturn8file12turn8file13turn8file7

---

## 4. Back / Front vocabulary matrix

## 4.1 Purpose
This matrix is normative.
Its goal is to ensure that:
- backend naming stays stable and implementation-friendly
- frontend wording stays beginner-facing and French
- the same business state keeps the same wording across screens
- the frontend never invents shorter wording that changes meaning fileciteturn8file7turn8file12

## 4.2 Technical analysis visible outcomes

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `CREDIBLE_PATTERN_FOUND` | `Pattern crédible détecté` |
| `MULTIPLE_COMPATIBLE_PATTERNS` | `Plusieurs patterns compatibles` |
| `NO_CREDIBLE_PATTERN` | `Aucun pattern crédible retenu` |
| `INSUFFICIENT_DATA` | `Données insuffisantes pour analyser` |
| `UNSUPPORTED_INSTRUMENT` | `Instrument hors périmètre V1` |
| `UNSUPPORTED_CONTEXT` | `Contexte non pris en charge` |

## 4.3 Pattern progression states

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `FORMING` | `En formation` |
| `MONITORING` | `À surveiller` |
| `CONFIRMED` | `Confirmé` |
| `INVALIDATED` | `Invalidé` |
| `COMPLETED` | `Terminé` |
| `ABSENT` | `Absent` |

## 4.4 Recommendation verbs

### Not held context

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `MONITOR` | `Surveiller` |
| `WAIT` | `Attendre` |
| `BUY` | `Acheter` |

### Held context

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `WAIT` | `Attendre` |
| `HOLD` | `Conserver` |
| `REINFORCE` | `Renforcer` |
| `LIGHTEN` | `Alléger` |
| `SELL` | `Vendre` |

## 4.5 PEA status wording

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `PEA_CONFIRMED_ELIGIBLE` | `Éligibilité PEA confirmée` |
| `PEA_CONFIRMED_INELIGIBLE` | `Non éligible PEA confirmée` |
| `PEA_UNKNOWN` | `Éligibilité PEA non confirmée` |

Hard rule:
`Éligibilité PEA non confirmée` must never look implicitly positive. fileciteturn8file12turn8file13

## 4.6 Composite-score availability wording

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `AVAILABLE` | `Score composite disponible` |
| `INSUFFICIENT_COVERAGE` | `Score composite indisponible : couverture de données insuffisante` |
| `PEA_UNKNOWN_BLOCKING` | `Score composite indisponible : éligibilité PEA non confirmée` |
| `CONFIRMED_INELIGIBLE_IN_UNIVERSE` | `Score composite indisponible : instrument confirmé non éligible PEA dans cet univers` |
| `UNSUPPORTED_UNIVERSE` | `Score composite indisponible : univers demandé non pris en charge` |
| `PROVIDER_DATA_INCOMPLETE` | `Score composite indisponible : données fournisseur incomplètes ou indisponibles` |

## 4.7 Support availability wording

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `FULL` | `Lecture support complète` |
| `PARTIAL` | `Lecture support partielle` |
| `UNAVAILABLE` | `Lecture support indisponible` |

## 4.8 Freshness wording

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `FRESH` | `Données à jour` |
| `AGING` | `Données à surveiller` |
| `STALE` | `Données obsolètes` |
| `MISSING` | `Données indisponibles` |

## 4.9 Generic cross-screen state wording

| Backend / enum / internal code | Required French frontend wording |
|---|---|
| `UNKNOWN` | `Inconnu` or explicit domain wording such as `non confirmé` |
| `UNSUPPORTED` | `Non pris en charge` or `hors périmètre V1` |
| `UNAVAILABLE` | `Indisponible` |
| `INCOMPLETE` | `Incomplet` |
| `INSUFFICIENT` | `Insuffisant` |
| `INVALIDATED` | `Invalidé` |

## 4.10 Visibility rule
The backend naming and frontend wording matrix must be reused consistently across:
- list pages
- detail pages
- summary rails
- notifications
- history
- comparison
- admin governance pages

If the same backend truth is shown on multiple pages, the same French wording family must be used. fileciteturn8file7turn8file12

---

## 5. Global screen contract

Every product page must respect the following:

### 5.1 One dominant question per page
A page must answer **one dominant user question**.

### 5.2 One primary action at most
A page must have:
- one primary action at most
- secondary actions only if they do not compete with the main one

### 5.3 Section ordering
The visual order must follow:
1. what it is
2. what the product knows
3. what it means
4. what the user can do next

This remains aligned with the page composition contract of the normative spec. fileciteturn8file10turn8file11

### 5.4 State family required on all canonical pages
Each canonical page must support:
- empty state
- loading state
- recoverable error state
- business non-executable state

These are not the same thing. fileciteturn8file10turn8file11

### 5.5 Recommendation visual rule
Recommendation must remain visually downstream from:
- market reading
- support reading
- PEA truth

### 5.6 Held / not-held rule
Where recommendation is shown, the page must explicitly preserve the held / not-held distinction. fileciteturn8file3turn8file14

### 5.7 Responsive rule
Desktop, tablet, and mobile may change density, but must never change semantic meaning. fileciteturn8file2turn8file7

---

## 6. Anonymous space

## 6.1 Login

### Dominant question
How do I enter the correct space?

### Product objective
Authenticate the user and route them to the correct coherent space.

### Mandatory visible areas
1. Product identity
2. Login form
3. Forgot-password access
4. Role-aware quick entry or role explanation
5. Clear distinction between user and admin spaces
6. Public reassurance copy

### Mandatory visible inputs
- email
- password
- remember-me toggle if kept
- forgot-password entry point

### Mandatory visible outputs
- successful authentication feedback
- failed authentication feedback
- route to user home or admin overview
- role differentiation clarity

### Main actions
- sign in
- use quick login in mock/demo context only
- open forgot password

### UX rules
- before login, no internal shell is visible
- no user/admin navigation should appear yet
- admin is not a “switch” inside anonymous mode, it is a different post-login space

### States
- empty not applicable
- loading: authentication in progress
- recoverable error: invalid credentials / temporary auth failure
- business non-executable: account exists but access not available in this context

### Reference in visual package
- `index.html`
- `login.html`

---

## 6.2 Forgot password

### Dominant question
How do I start account recovery simply?

### Product objective
Allow the user to initiate recovery in a calm and reassuring way.

### Mandatory visible areas
1. Recovery explanation
2. Email / identity input
3. Return-to-login action

### Main actions
- submit recovery request
- return to login

### States
- empty not applicable
- loading: recovery request in progress
- recoverable error: temporary sending problem
- business non-executable: unknown identity handled safely without leaking account existence rules

### Reference in visual package
- `forgot-password.html`

---

## 6.3 Reset password

### Dominant question
How do I define a new password safely?

### Product objective
Complete password recovery with a clear and low-friction flow.

### Mandatory visible areas
1. Password creation form
2. Password confirmation
3. Return / cancel action

### Main actions
- save new password
- cancel and return

### States
- loading: save in progress
- recoverable error: temporary failure
- business non-executable: expired or invalid reset context

### Reference in visual package
- `reset-password.html`

---

## 7. User space

## 7.1 User home

### Dominant question
What deserves my attention now?

### Product objective
Act as the daily command center immediately after login.

### Mandatory visible areas
1. Current user context
2. Priority block
3. Recent analyses block
4. Held-position attention block or portfolio-oriented summary
5. Non-evaluable / incomplete block
6. Next-step guidance
7. Shortcut to launch analysis
8. Route to help and account

### Mandatory visible outputs
- clear prioritized ordering
- explicit next best action
- distinction between urgent, recent, held, and non-evaluable content

### Main actions
- open analysis entry
- open watchlist
- open portfolio
- open history
- open help
- open account

### UX rules
- this is not a generic menu page
- it must behave like a command center
- the first viewport must already show meaningful prioritization
- mobile must preserve the action order:
  - urgent
  - held-position implications
  - recent
  - non-evaluable
  - explore more

### States
- empty: no watchlist, no portfolio, no recent history yet
- loading: dashboard data being assembled
- recoverable error: partial dashboard fetch failed
- business non-executable: some product surfaces unavailable for this user context

### Reference in visual package
- `user-home.html`

---

## 7.2 Watchlist

### Dominant question
Which non-held instruments deserve attention?

### Product objective
Provide a filtered and prioritized list of instruments worth following.

### Mandatory visible areas
1. Watchlist title and page purpose
2. Filter / sorting controls
3. Prioritized instrument list
4. Recommendation summary per row when allowed
5. Links to deeper review
6. Empty state and unsupported-state handling

### Mandatory visible outputs per item
- instrument identity
- condensed market reading summary
- condensed support / PEA summary
- recommendation summary
- freshness or recency hint when relevant

### Main actions
- filter
- sort
- open result
- open help
- launch analysis

### UX rules
- recommendation summary must not erase the distinction between market/support/PEA
- dense tables may be simplified on mobile, but meaning must remain explicit
- an item must never look “positive” only through color

### States
- empty: no watchlist items or no result for current filters
- loading: watchlist fetch in progress
- recoverable error: watchlist retrieval failed
- business non-executable: item exists but V1 cannot analyze or support it

### Reference in visual package
- `watchlist.html`

---

## 7.3 Portfolio

### Dominant question
What does this mean for positions I already hold?

### Product objective
Provide held-position decision support.

### Mandatory visible areas
1. Held-position list
2. Explicit held-position recommendation logic
3. Quick routes to simulation and history
4. Brief reminder that held verbs differ from not-held verbs

### Mandatory visible outputs per position
- instrument identity
- held-specific recommendation verb
- condensed market reading
- condensed support reading
- route to simulation
- route to history

### Main actions
- inspect held item
- open simulation
- open history
- continue to deeper instrument understanding

### UX rules
- `Monitor` must never be used as the final held-position recommendation
- the held / not-held contract must stay explicit
- this page is action-oriented but must not swallow analytical truth

### States
- empty: no held positions
- loading: portfolio positions loading
- recoverable error: temporary portfolio fetch issue
- business non-executable: a position exists but detailed V1 support cannot be produced in that case

### Reference in visual package
- `portfolio.html`

---

## 7.4 Analysis entry

### Dominant question
How do I launch an analysis correctly?

### Product objective
Prepare and launch an analysis request with the right context.

### Mandatory visible areas
1. Instrument input
2. Holding-state choice
3. Contextual help
4. Primary CTA to analyze
5. Recovery route when the user is unsure
6. Product limit reminder where relevant

### Mandatory visible inputs
- instrument search / entry
- held / not-held selection

### Main actions
- analyze
- ask for help
- leave to another core user flow

### UX rules
- this page must be simple
- the product should prevent wrong recommendation interpretation by asking holding context upfront
- language must stay calm and educational

### States
- empty: no input yet
- loading: analysis launch in progress
- recoverable error: request launch failed
- business non-executable: request understood but not executable in V1

### Reference in visual package
- `analysis-entry.html`

---

## 7.5 Analysis result

### Dominant question
What does the product conclude for this analysis request?

### Product objective
Present a consolidated and understandable analytical result.

### Mandatory fixed sections
1. Outcome banner
2. Market reading
3. Support reading
4. Personal-situation reading
5. Contextual summary rail
6. Links to history, parameter detail, help and other follow-up surfaces

### Mandatory market reading fields
- visible primary pattern
- compatible alternatives
- pattern status
- confidence indication when available
- invalidation summary / hint
- main risk hint when available

### Mandatory support reading fields
- score availability / composite score when allowed
- support completeness
- PEA status
- blocking or partial conditions when relevant

### Mandatory recommendation rule
This page must explicitly preserve:
- no current position
- existing position

Allowed frontend verbs remain:
- not held: `Surveiller`, `Attendre`, `Acheter`
- held: `Attendre`, `Conserver`, `Renforcer`, `Alléger`, `Vendre`

### Main actions
- inspect result
- open history
- open parameter detail
- open help
- continue to another user flow

### UX rules
- outcome first
- market reading before support reading
- support reading before recommendation
- recommendation must not visually swallow the rest
- compatible alternative patterns must remain visible
- if a “why this recommendation” block is shown, it must remain traceable to visible product truths and must not invent backend truth fileciteturn8file14turn8file7

### States
- loading: analysis result loading
- recoverable error: temporary fetch failure
- business non-executable examples:
  - no credible pattern retained
  - insufficient data
  - unsupported instrument perimeter
  - support reading incomplete
  - PEA unknown when it blocks downstream interpretation

### Reference in visual package
- `analysis-result.html`

---

## 7.6 Instrument detail

### Dominant question
How should I understand this instrument in depth?

### Product objective
Act as the central V1 instrument page.

### Mandatory fixed sections
1. Instrument summary
2. Market reading
3. Support reading
4. Parameter reading access
5. What this means for me
6. Links to simulation and history

### Mandatory instrument summary fields
- instrument name
- ticker
- asset type
- perimeter / market context
- PEA status
- data freshness
- analysis availability state

### Mandatory market reading fields
- visible primary pattern
- compatible alternatives
- pattern status
- confidence level
- validation / invalidation summary
- invalidation level when available
- risk / reward hint when available
- pedagogical summary

### Mandatory support reading fields
- scoring version
- active universe id
- PEA status
- category scores when shown
- coverage ratio
- composite score only when allowed
- missing categories or metrics summary when relevant

### Parameter reading access model
For each visible parameter, the user must be able to reach:
1. definition
2. how to read the current value
3. why it matters
4. what it implies for their own situation

### “What this means for me” rule
Must branch explicitly on holding state.

### Main actions
- inspect instrument
- open parameter detail
- open simulation
- open history

### UX rules
- this is the central V1 page
- recommendation must not visually swallow market reading
- compatible alternatives must remain visible
- the two holding contexts must be explicit

### States
- loading
- recoverable error
- business non-executable
- partial support reading

### Reference in visual package
- `instrument-detail.html`

---

## 7.7 Parameter detail

### Dominant question
What does this metric mean and how should I read it?

### Product objective
Provide pedagogical interpretation of one parameter.

### Mandatory visible sections
- parameter name
- simple definition
- role in the category
- how to read the current value
- limits of interpretation
- what the value supports
- what it does not prove
- implication without position
- implication with position

### Main actions
- read meaning
- return to previous analytical context

### UX rules
- strong readability
- one parameter must never become a final recommendation by itself
- pedagogical tone must remain simpler than raw finance jargon

### States
- loading
- recoverable error
- business non-executable only when the selected parameter cannot be rendered meaningfully

### Reference in visual package
- `parameter-detail.html`

---

## 7.8 Simulation

### Dominant question
What happens if I test a scenario?

### Product objective
Allow scenario exploration without confusing it with persisted truth.

### Mandatory visible areas
1. Simulation inputs
2. Simulation outputs
3. Explicit distinction between simulation and persisted history

### Main actions
- edit scenario inputs
- inspect simulated result
- route back to other analytical flows

### UX rules
- simulation must not look like persisted history
- wording must avoid implying that simulated output is an observed historical truth

### States
- empty: no scenario yet
- loading: simulation in progress
- recoverable error: simulation failed temporarily
- business non-executable: scenario unsupported in V1

### Reference in visual package
- `simulation.html`

---

## 7.9 History

### Dominant question
What has been persisted over time?

### Product objective
Show persisted analytical states and make the snapshot nature explicit.

### Mandatory visible areas
1. History timeline
2. Snapshot identity
3. Timestamp
4. Summary of persisted meaning
5. Route to comparison
6. Distinction between history and current live reading

### Main actions
- inspect history entry
- compare snapshots

### UX rules
- must read as persisted truth
- must not feel like reconstruction from current state
- timeline structure should reinforce the dated nature of the information

### States
- empty: no persisted history yet
- loading
- recoverable error
- business non-executable if history exists conceptually but cannot be rendered in the current scope

### Reference in visual package
- `history.html`

---

## 7.10 Snapshot comparison

### Dominant question
What changed between two persisted states?

### Product objective
Compare two persisted snapshots reliably.

### Mandatory visible areas
1. Left snapshot summary
2. Right snapshot summary
3. Change summary
4. Non-comparability feedback when relevant
5. Reading-order guidance

### Mandatory outputs
- what changed in market reading
- what changed in support reading
- what changed in recommendation
- why comparison may be limited

### Main actions
- compare
- return to history
- continue navigation

### UX rules
- comparison must never invent frontend approximations
- change reading must distinguish:
  - cause
  - consequence
- non-comparability must be explicit, not hidden

### States
- empty: no valid pair selected
- loading
- recoverable error
- business non-executable: snapshots cannot be compared meaningfully in V1 conditions

### Reference in visual package
- `snapshot-comparison.html`

---

## 7.11 Learn

### Dominant question
How do I understand the product concepts outside execution flows?

### Product objective
Provide educational content without mixing it into action-heavy screens.

### Mandatory visible areas
- learning topic entry points
- conceptual explanations
- routes back to relevant task screens

### Main actions
- browse learning topics
- return to product flows

### UX rules
- pedagogical
- calm
- non-urgent
- must not replace task-oriented help when the user is mid-flow

### Reference in visual package
- `learn.html`

---

## 7.12 Notifications

### Dominant question
What needs my attention and where should I go?

### Product objective
Act as a prioritization and routing layer.

### Mandatory visible areas
1. Notification list
2. Category / status filtering
3. Clear route from each item to the relevant screen
4. Read/unread or attention-state handling when supported

### Main actions
- filter notifications
- open related screen
- mark as handled later when supported

### UX rules
- notifications do not explain everything
- they prioritize and route
- the destination page keeps semantic truth

### States
- empty: no notifications
- loading
- recoverable error
- business non-executable where an item exists but the destination cannot be executed in the current scope

### Reference in visual package
- `notifications.html`

---

## 7.13 Help center

### Dominant question
How do I understand the product, its limits, and my current confusion?

### Product objective
Provide contextual explanation and route the user back to the relevant task surface.

### Mandatory visible areas
1. Product concepts
2. Product limits
3. Routes from frequent doubts to task screens
4. Contextual explanations around:
   - result meaning
   - history meaning
   - comparison meaning
   - account/help flows

### Main actions
- open help topic
- jump back to task screen
- understand product limits

### UX rules
- help explains
- help does not replace the task page
- help must be contextual where possible

### Reference in visual package
- `help-center.html`

---

## 7.14 Onboarding empty

### Dominant question
How do I get first value when I have little or no data yet?

### Product objective
Guide first-use behavior.

### Mandatory visible areas
- first-value explanation
- suggested first steps
- route to watchlist / analysis / portfolio entry depending on context

### Main actions
- follow onboarding suggestion
- move to first meaningful task

### UX rules
- onboarding must remain concrete
- no vague motivational filler
- clear first action

### Reference in visual package
- `onboarding-empty.html`

---

## 7.15 Account

### Dominant question
How do I manage my own information and preferences?

### Product objective
Provide self-service management for the current user.

### Mandatory visible areas
1. Profile
2. Preferences
3. Notification preferences or route to them
4. Security route
5. Help / recovery routes when useful

### Hard rule
This page manages **self information only**.
It is not admin user management.

### Main actions
- edit own profile
- edit preferences
- open notifications
- open security

### UX rules
- segmentation should remain explicit
- no admin actions here
- user autonomy and clarity come first

### Reference in visual package
- `account.html`

---

## 8. Admin space

## 8.1 Admin overview

### Dominant question
What requires governance or operational attention in the admin space?

### Product objective
Act as the admin entry dashboard.

### Mandatory visible areas
- high-level admin priorities
- routes to user management
- routes to registries, policy, audit, and quality
- clear admin posture

### Main actions
- open user management
- open registry surfaces
- open policy surfaces
- open audit / data-quality surfaces

### Reference in visual package
- `admin-overview.html`

---

## 8.2 Admin users

### Dominant question
How do I manage users structurally?

### Product objective
Provide structured user administration.

### Mandatory visible areas
1. User list
2. Search / filter controls
3. Role and status visibility
4. Management action entry point
5. Empty / error state handling

### Mandatory visible outputs per user
- identity
- role
- status
- last activity when relevant

### Main actions
- search
- filter
- manage user
- create user

### Hard rule
This page belongs to admin only.
User self-management must stay in `Account`.

### Reference in visual package
- `admin-users.html`

---

## 8.3 Admin instrument registry

### Dominant question
What is the governed product truth for each instrument?

### Product objective
Expose instrument registry information under governance visibility.

### Mandatory visible areas
- search / filtering
- instrument identity
- provider mapping
- active universe membership
- support state
- freshness state

### Main actions
- search
- filter
- inspect registry row

### UX rules
- governance clarity first
- no hiding of traceability for convenience fileciteturn8file13turn8file7

### Reference in visual package
- `admin-instrument-registry.html`

---

## 8.4 Admin PEA registry

### Dominant question
What is the governed PEA truth for each instrument?

### Product objective
Show the explicit governed PEA truth and its source context.

### Mandatory visible areas
- instrument identity
- PEA status
- source type / source reference
- checked date
- policy version
- notes or status history summary when useful

### Main actions
- inspect entry
- search / filter
- govern status later when editable workflows are implemented

### UX rules
- unknown must remain visibly distinct from confirmed ineligible
- missing registry truth must not be rendered as implicit eligibility

### Reference in visual package
- `admin-pea-registry.html`

---

## 8.5 Admin scoring policy

### Dominant question
Which scoring rules are active?

### Product objective
Provide governance visibility into active scoring policy.

### Mandatory visible areas
- active scoring version
- active universes
- active categories
- metric inclusion rules
- coverage rules
- version history

### Main actions
- inspect policy
- compare policy versions later if needed

### Reference in visual package
- `admin-scoring-policy.html`

---

## 8.6 Admin parameter dictionary

### Dominant question
What wording and interpretation truth exists for each parameter?

### Product objective
Expose the governed dictionary for parameter pedagogy.

### Mandatory visible areas per parameter
- stable parameter id
- user-facing label
- simple definition
- advanced definition when useful
- category
- reading direction semantics
- interpretation guardrails
- limits of interpretation
- what the parameter does not prove
- implication templates for user without position
- implication templates for user with position
- wording version status

### UX rules
- governance-first
- strong readability
- no hidden frontend wording outside governed area fileciteturn8file13

### Reference in visual package
- `admin-parameter-dictionary.html`

---

## 8.7 Admin wording versions

### Dominant question
Which pedagogical and recommendation wording is active?

### Product objective
Provide governance visibility for wording packages.

### Mandatory visible areas
- action verb set
- recommendation strengths
- advice scenario codes
- deterministic text templates summary
- publication state

### Main actions
- inspect wording version
- check publication state

### UX rules
- wording governance must stay traceable
- summary cards must not invent a different meaning than the governed wording family fileciteturn8file7turn8file12

### Reference in visual package
- `admin-wording-versions.html`

---

## 8.8 Admin snapshot audit

### Dominant question
What exactly was persisted and under which version context?

### Product objective
Expose auditability around persisted snapshots.

### Mandatory visible areas
- snapshot identity
- timestamp
- rule versions
- market reading payload summary
- support reading payload summary
- recommendation payload summary
- audit comparison tools

### Main actions
- inspect snapshot
- compare snapshots

### UX rules
- auditability first
- link back to persisted-truth model
- no convenience abstraction that hides version context

### Reference in visual package
- `admin-snapshot-audit.html`

---

## 8.9 Admin data quality

### Dominant question
Where is product truth degraded or incomplete?

### Product objective
Show prioritized data-quality problems and their likely downstream product impact.

### Mandatory visible areas
- missing metrics by category
- unsupported or stale instruments
- provider freshness issues
- PEA registry incompleteness
- coverage degradation trends
- user impact linkage
- next admin action hint

### Main actions
- inspect issue
- filter issues
- open affected product surface
- move to impacted registry view

### UX rules
- distinguish source freshness issues from scoring-rule issues
- distinguish registry incompleteness from unsupported universe cases
- prioritize by likely user impact when possible fileciteturn8file13turn8file6

### Reference in visual package
- `admin-data-quality.html`

---

## 9. Screens explicitly excluded from this document

The following items may exist in the latest package, but they are **support artifacts**, not canonical product screens for this document:
- `design-system.html`
- `page-states.html`
- `ARCHITECTURE_REVIEW.md`
- `UI_DISPLAY_OBJECTS.ts.md`
- `UX_UI_EVOLUTION_MATRIX.md`
- `PERSONAS.md`
- `UX_SCORED_MATRIX.md`
- `DIRECT_CORRECTIONS_APPLIED.md`

This exclusion remains aligned with the compact matrix scope. fileciteturn8file6

---

## 10. Locked cross-screen rules

1. The same business state must keep the same wording family across pages.
2. Frontend visible wording must stay in French.
3. Backend/internal naming may stay in English, but never leak raw into the UI.
4. Recommendation must remain visually downstream from analytical truth.
5. Held / not-held branching must remain explicit wherever recommendation exists.
6. History and comparison must use persisted snapshot truth.
7. Admin pages must not hide governance traceability for convenience.
8. No screen may imply ETF runtime support in active V1.
9. Summary wording must stay traceable to the same action verb family as the source page.
10. Mobile may simplify layout density, but not meaning. fileciteturn8file7turn8file2

---

## 11. Success criteria for future refonte work

A serious refonte of `PredictFinance_V1_product_lock_screen_matrix.md` should be considered successful only if:

1. Every canonical screen has a precise dominant question.
2. Every screen defines required visible information and actions.
3. Every screen defines empty / loading / recoverable error / business non-executable behavior.
4. The front/back vocabulary matrix is explicitly reused during implementation.
5. All visible user-facing wording stays in French.
6. The held / not-held recommendation split remains locked.
7. Market reading, support reading, PEA, recommendation, and data completeness remain separated.
8. Admin governance screens stay traceable and non-ambiguous.
9. The resulting implementation backlog can map directly to these screens without inventing missing product semantics.
10. If a later mockup conflicts with this file, this file remains normative until an explicit replacement is approved.

---

## 12. Practical reference inventory

### Anonymous
- `index.html`
- `login.html`
- `forgot-password.html`
- `reset-password.html`

### User
- `user-home.html`
- `watchlist.html`
- `portfolio.html`
- `analysis-entry.html`
- `analysis-result.html`
- `instrument-detail.html`
- `parameter-detail.html`
- `simulation.html`
- `history.html`
- `snapshot-comparison.html`
- `learn.html`
- `notifications.html`
- `help-center.html`
- `onboarding-empty.html`
- `account.html`

### Admin
- `admin-overview.html`
- `admin-users.html`
- `admin-instrument-registry.html`
- `admin-pea-registry.html`
- `admin-scoring-policy.html`
- `admin-parameter-dictionary.html`
- `admin-wording-versions.html`
- `admin-snapshot-audit.html`
- `admin-data-quality.html`
