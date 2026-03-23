# CONTEXT RELOAD — STRICT CHECKPOINT

You are resuming an architecture arbitration session.

## Session state

- Source prompt set: refont_by_bloc.txt
- Last completed prompt message: Message 29
- Last completed blocks:
  - P04C-B001
  - P04C-B002
  - P04C-B003
- Next message to execute: Message 30
- Next blocks:
  - P04C-B004
  - P04C-B005

## Binding rule

All previously completed blocks are LOCKED and must be treated as immutable architectural decisions.

You must NOT:
- reinterpret past decisions
- weaken them
- reopen them
- contradict them
- introduce redesign
- introduce implementation

## Core invariants (MANDATORY)

- .NET backend is the sole owner of business truth
- Python is replaceable and non-sovereign
- Python must never decide Buy/Sell/Hold
- Front must remain passive on critical business logic
- Frozen market snapshot is mandatory
- Silent recomputation is forbidden
- Source of truth = frozen snapshot + persisted analysis batch
- Multi-candidate analysis must not collapse into fake single-pattern truth
- Forced fake 100% distribution is forbidden
- Abstention is not a business pattern
- Score semantics must be explicit
- Internal decision semantics must be richer than Buy/Sell/Hold
- Buy/Sell/Hold is only an outward projection
- Full auditability is mandatory

## Locked conclusions (CRITICAL MEMORY)

- P01-B001 → NO → system is not truly multi-pattern
- P01-B002 → YES → system structurally depends on DOUBLE_TOP

## Derived structural truths (DO NOT CONTRADICT)

- Runtime is mono-pattern despite multi-pattern contracts
- DOUBLE_TOP is structurally central
- Contracts ≠ business truth
- Persistence ≠ authority
- Runtime authority belongs to inner layers

## Anti-drift rules

You must NOT:
- reintroduce Python sovereignty
- reintroduce front-side business logic
- collapse multi-candidate analysis into fake single-pattern
- mix transport contracts with business truth
- solve future blocks early
- reference blocks not yet executed
- generate code
- propose redesign

## Resume protocol

Before executing:
1. Restate key constraints briefly
2. Confirm no contradiction
3. Execute ONLY Message 30
4. Do not jump to Message 31

If any required context is missing:
→ STOP and ask for it
→ DO NOT GUESS

---

# EXECUTE NEXT MESSAGE

# PACK 04C — OUTER LAYERS CHARTER

## BLOCKS

P04C-B004 — Must Api reject deep business rules?
P04C-B005 — Can Front contain presentation, interaction, and passive mapping only?

Decision required:
For each block, decide whether the boundary rule is mandatory.

Mandatory evaluation criteria:
- transport-only discipline
- front passivity
- anti-business leakage
- reviewable ownership

Forbidden:
- no screen design
- no component design
- no endpoint implementation details

## Execute

P04C-B004
P04C-B005

You must strictly follow all rules below.

### Batch execution rule
- Answer each block independently
- Keep order
- Do not merge blocks
- Do not let one block absorb another

### Step 1 — Scope lock
- Restate decision question
- No scope expansion
- No anticipation

### Step 2 — Preliminary verdict
- YES / NO / YES, WITH HARD BOUNDARY / NO, FORBIDDEN
- No justification yet

### Step 3 — Structural verification
Evaluate ALL:
- structure
- ownership
- contracts
- flow semantics
- persistence semantics
- naming semantics
- coupling
- runtime authority

### Step 4 — Contradictions
- List contradictions
- Resolve or downgrade verdict

### Step 5 — Final verdict
- Confirm or revise
- No ambiguity

### Step 6 — Output format (STRICT)

## Block ID
## Block title
## Verdict
## Goal
## Decision required
## Structural evidence
## Contradictions
## Non-negotiable constraints
## Explicit exclusions
## Rejected alternatives
## Closure

### Step 7 — Anti-drift validation
- no future references
- no redesign
- no implementation
- no invariant weakening

If invalid → regenerate block.

Apply Global Master Charter strictly.