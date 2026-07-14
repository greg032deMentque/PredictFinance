# PredictFinance V1 — Personas and Simulated Usage

## Method

Each persona was simulated in three passes:
1. first-time discovery
2. return usage after first learning
3. friction or doubt scenario

The simulation is limited to the current static prototype and therefore evaluates:
- clarity
- IA quality
- UX friction
- semantic consistency
- admin/user pathway quality

It does not prove:
- real authentication
- authorization enforcement
- persistence
- performance
- runtime reliability

---

# User personas

## 1. Camille — cautious beginner
- Finance knowledge: low
- Goal: understand whether the app can help without overwhelming her
- Expectations:
  - simple language
  - clear next action
  - reassurance
  - low risk of misunderstanding
- Strengths:
  - disciplined
  - reads explanations
  - follows steps
- Weaknesses:
  - quickly overloaded
  - fears making mistakes
  - slow to decide

### Main friction observed
Vocabulary load remains noticeable even after the help surfaces were improved.

---

## 2. Julien — intermediate retail investor
- Finance knowledge: medium
- Goal: move quickly toward instruments to review
- Expectations:
  - clear prioritization
  - fast filtering
  - useful comparison
  - recent activity visibility
- Strengths:
  - autonomous
  - action oriented
  - comfortable with market reading
- Weaknesses:
  - impatient
  - skips help
  - wants immediate answers

### Main friction observed
Comparison is clearer now, but he still wants a stronger “why changed” explanation.

---

## 3. Sofia — portfolio-first user
- Finance knowledge: medium
- Goal: understand what to do with existing positions
- Expectations:
  - held / not-held distinction
  - strong portfolio focus
  - decision support
- Strengths:
  - disciplined
  - risk-aware
  - decision oriented
- Weaknesses:
  - over-focus on final recommendation
  - may ignore analytical nuance

### Main friction observed
The split is strong, but the link between result, simulation and historical decision path could still be tighter.

---

## 4. Marc — skeptical advanced user
- Finance knowledge: high
- Goal: test product credibility
- Expectations:
  - traceability
  - consistency
  - explicit limitations
  - no vague language
- Strengths:
  - very analytical
  - detects inconsistency quickly
  - useful challenger
- Weaknesses:
  - low tolerance for approximation
  - strongly critical

### Main friction observed
Some pages still feel more like advanced mockups than fully explained analytical workspaces.

---

## 5. Inès — mobile-first fragmented user
- Finance knowledge: low to medium
- Goal: consult quickly with minimal effort
- Expectations:
  - clean mobile reading
  - short navigation paths
  - concise information
- Strengths:
  - realistic stress test for mobile UX
  - exposes density issues fast
- Weaknesses:
  - low patience
  - rarely reads long explanations

### Main friction observed
Mobile is much better than before, but some analytical pages still benefit from a more compact summary mode.

---

# Admin personas

## 1. Thomas — operations support admin
- Finance knowledge: low
- Goal: help users without touching deep market logic
- Expectations:
  - find a user quickly
  - understand user status
  - resolve support issues fast
- Strengths:
  - pragmatic
  - support oriented
  - fast triage
- Weaknesses:
  - weak finance literacy
  - can misread overly business-heavy screens

### Main friction observed
The users page is much better, but support resolution workflows are still more implied than explicit.

---

## 2. Claire — product governance admin
- Finance knowledge: medium
- Goal: verify wording / policy / registry coherence
- Expectations:
  - traceability
  - version clarity
  - structured admin pages
- Strengths:
  - rigorous
  - good cross-reading ability
  - strong for governance
- Weaknesses:
  - low tolerance for duplication
  - expects very explicit structure

### Main friction observed
Admin surfaces are now coherent, but cross-surface consistency audit could still be stronger.

---

## 3. Karim — data quality admin
- Finance knowledge: medium
- Goal: detect and prioritize data issues
- Expectations:
  - severity ordering
  - impact clarity
  - prioritization support
- Strengths:
  - analytical
  - prioritization oriented
  - quality focused
- Weaknesses:
  - impatient with descriptive-only pages
  - wants operational ranking

### Main friction observed
The updated data quality page now links issues to user impact, but central prioritization views could still go further.

---

## 4. Élodie — audit / compliance admin
- Finance knowledge: low to medium
- Goal: verify what was persisted and under which rule context
- Expectations:
  - versions
  - snapshots
  - explicit explanations
  - auditability
- Strengths:
  - methodical
  - procedural
  - good traceability reviewer
- Weaknesses:
  - can get lost if navigation is too broad
  - prefers explicit causal explanations

### Main friction observed
Snapshot and wording pages are much better, but a stronger “why this state exists” family would still help.

---

## 5. Nicolas — product super-admin
- Finance knowledge: high
- Goal: validate overall product coherence
- Expectations:
  - global consistency
  - reusable patterns
  - low UX debt
  - credible information architecture
- Strengths:
  - system vision
  - strong product sensitivity
  - spots weak architecture quickly
- Weaknesses:
  - expects near-production coherence
  - sees mock limitations immediately

### Main friction observed
The product is now a strong stable prototype baseline, but the gap with real componentized implementation is still visible.

---

# Consolidated weaknesses found during simulation

## Major
1. User home still has room to become an even stronger daily command center.
2. Standardized page states now exist, but should ultimately appear natively on every canonical page.
3. Comparison is improved, but advanced users still want stronger causal explanation.
4. Admin is now strong on reading and governance, but still lighter on action workflows.
5. There is still a gap between stable visual prototype and truly implementation-ready frontend architecture.

## Secondary
1. Pedagogy is better, but not yet adaptive by user sophistication level.
2. Mobile is solid, but dense analytical pages still need tighter compact summaries.
3. Some admin-to-user impact links could go even further.
4. Help content is now more contextual, but could be injected more aggressively from in-page doubts.
