# Documentation guardrails for agents

## Purpose

This file defines the stable documentation discipline that prevents drift, duplicated normative truth, and silent scope expansion.

## Distinguish repository truth from target state

When writing or updating documentation, agents must keep the following distinctions explicit.

### Repository truth today
- solution contains `BackPredictFinance.Patterns`
- solution does not contain `BackPredictFinance.Contracts`
- several V1 contracts still live in `BackPredictFinance.Common/AnalysisV1`
- `BackPredictFinance.Services` still depends on `BackPredictFinance.ViewModels`
- legacy compatibility around `DOUBLE_TOP` still exists in the codebase

### Target-state architecture intent
- business-core contracts should move out of transport concerns
- Services should no longer depend on ViewModels
- compatibility residue should stop constraining the active continuation-pattern runtime

Agents must not present target-state architecture as already implemented repository truth.

## Canonical ownership rules

### Product truth
Product truth lives only in:
- `Doc/contract_freeze.md`
- `Doc/product/*`

### Stable agent governance
Stable agent governance lives only in:
- `AGENTS.md`
- `Doc/ai/*`

### Execution prompts
Execution prompts may live in:
- `Doc/codex/*`

Execution prompts are never canonical product or architecture owners.

## Anti-duplication rules

- one normative rule must have one canonical owner
- do not duplicate frozen product truth in agent-operating documents
- do not duplicate architecture rules across multiple stable documents without a clear ownership reason
- do not duplicate business logic truth between frontend and backend documentation layers
- do not duplicate scoring logic between frontend and backend
- do not duplicate PEA eligibility truth between provider heuristics and product registry facts

## Forbidden documentation patterns

- no stable `NEXT-STEP` or equivalent file in the framing layer
- no execution-prompt folder owning frozen product truth
- no summary document restating canonical product truth as an independent normative layer
- no silent broadening from French equities to ETFs in V1
- no documentation of ETF support as active V1 runtime behavior
- no documentation presenting PEA status as legal certification
- no documentation presenting one metric as final recommendation truth by itself

## Explanation guardrails

If the product exposes metric explanations, agents must preserve all of the following distinctions:
- market timing versus support quality
- category score versus individual metric explanation
- support-reading implication versus final recommendation
- deterministic wording versus free-form commentary
