# V1 admin and parameter explanation contract

## Purpose

This file closes two product gaps that were previously under-specified:
- the admin/back-office functional domain
- the beginner-facing parameter explanation capability

Without these two areas, V1 risks becoming:
- difficult to govern
- inconsistent in wording
- too score-centric for beginners
- vulnerable to hidden frontend-owned product logic

## Closed V1 decisions

The following decisions are now frozen for V1 documentation:
- V1 active runtime scope = French equities only
- ETF support = V2 extension only
- parameter explanations = backend deterministic generation from a governed parameter dictionary
- support-reading snapshots = persist enough support-reading truth to avoid later provider-based reconstruction
- minimal shippable admin scope = instrument registry visibility, PEA registry, scoring policy visibility, parameter dictionary governance, wording version visibility, snapshot audit read surface, data-quality visibility

## Admin is part of the product architecture

Admin is not an optional utility layer.
For V1 it is a functional product domain because the application needs governed truth for:
- PEA eligibility states
- scoring configuration and versioning
- parameter dictionary wording
- recommendation wording versions
- snapshot auditability
- data-quality follow-up

## Admin domain — required modules

### 1. Operations dashboard
Must expose at least:
- active supported instruments count
- missing-data rates
- Unknown PEA counts
- latest scoring version in use
- latest wording version in use
- recent non-executable analysis outcomes
- snapshot volume and freshness indicators

### 2. Instrument registry
Must expose at least:
- product instrument identity
- provider identity mapping
- asset type
- active universe membership
- support state
- data freshness indicators

### 3. PEA registry
Must expose at least:
- instrument
- PEA status
- source type
- source reference
- checkedUtc
- policy version
- notes when governance allows them
- status history when persisted

### 4. Scoring policy administration
Must expose at least:
- active universe ids
- active categories
- metric inclusion / exclusion
- metric direction
- minimum coverage rules
- coverage penalty rules
- scoring version history

### 5. Parameter dictionary administration
This module is mandatory if the product wants deterministic and explainable beginner guidance.

For each parameter, admin must govern at least:
- stable parameter id
- user-facing label
- simple definition
- advanced definition when useful
- category assignment
- reading direction semantics
- interpretation guardrails
- limits of interpretation
- what the parameter does not prove
- implication templates for a user without a position
- implication templates for a user with a position
- wording version status

### 6. Recommendation wording administration
Must govern at least:
- action verb set
- recommendation strengths
- advice scenario codes
- deterministic text templates
- wording version publication

### 7. Snapshot audit administration
Must expose at least:
- snapshot identity
- timestamp
- rule versions
- market reading payload summary
- support reading payload summary
- recommendation payload summary
- audit comparison tools

### 8. Data-quality administration
Must expose at least:
- missing metrics by category
- unsupported or stale instruments
- provider freshness issues
- PEA registry incompleteness
- coverage degradation trends

## Parameter explanation is a first-class product capability

The product must not stop at category scores.
A beginner user must be able to inspect a parameter and understand:
- what it measures
- how to read the current value
- why it matters
- what this means in the user's own situation

## Parameter explanation model

Each visible parameter explanation must contain the following sections.

### 1. Definition
Example question answered:
- What is ROE?
- What is debt to equity?
- What is trailing PE?

### 2. Current-value reading
Example question answered:
- Is the current value high, low, favorable, unfavorable, or simply incomplete for interpretation in the active universe?

### 3. Why it matters
Example question answered:
- Why is this parameter considered in the support reading?

### 4. Limits of interpretation
Example question answered:
- What would be a misuse of this parameter?

### 5. What this implies for me
This section must be situation-aware.
It must distinguish at least:
- user without a current position
- user with a current position
- support reading incomplete because of missing data

## Anti-drift rules for parameter explanations

### Rule 1 — no isolated recommendation truth
A single parameter explanation must never output a final product recommendation on its own.

### Rule 2 — no fake timing
A parameter explanation must never be written as if it were a technical timing engine.

### Rule 3 — no hidden frontend explanations
If parameter explanations are deterministic V1 product truth, the frontend must consume them, not invent them.

### Rule 4 — no adminless wording sprawl
If the product exposes parameter explanations widely, a governed parameter dictionary is required.

## Suggested user journey

Instrument detail -> support reading -> category -> parameter list -> parameter detail -> situation-aware implication

This journey is additive to the technical pattern journey.
It does not replace it.

## Required output examples by intent

### Allowed pedagogical wording style
- This value supports the support-reading quality, but it is not a timing signal.
- This value is weaker than the current universe median and calls for caution in a reinforcement scenario.
- This value is unavailable, so the support reading remains partial.
- This value looks favorable, but it is insufficient on its own to justify a decision.

### Forbidden wording style
- Buy because the PE is low.
- Sell because the debt ratio is high.
- This metric guarantees quality.
- This metric proves the market will rise.

## Architecture consequence

V1 chosen model:
- backend-owned parameter explanation generation from governed dictionary rules

Consequence:
- the parameter dictionary remains the governed source of wording fragments and interpretation guardrails
- the backend produces deterministic explanation payloads for the frontend
- the frontend must not synthesize explanation truth on its own
- persisted snapshots must keep enough rendered or structured support-reading truth to preserve later auditability without requerying providers

The product must not rely on ad hoc frontend wording fragments for this capability.


## Canonical perimeter note

The canonical minimal shippable admin perimeter for V1 is defined in `10_v1_resolved_product_decisions.md`.
This file must remain aligned with that canonical list and must not introduce a broader mandatory perimeter by wording drift.
