# Agent operating contract

## Purpose

This file defines only the stable operating posture expected from coding agents.
It is not a product-specification owner.
It must never compete with `Doc/contract_freeze.md` or `Doc/product/*`.

## Canonical reading rule

When product truth is needed, agents must read the canonical product documents first.
This file may define how to work safely from those documents.
It must not restate or replace frozen product scope, business contracts, or screen-level truth.

## Non-negotiable operating rules

- never invent repository state
- never present a target-state intention as implemented repository truth
- never create a second source of truth for product behavior
- never reopen frozen product decisions without explicit contradictory evidence or a new human decision
- never turn an execution prompt into a canonical specification source
- never let a temporary task document become a stable framing document

## Mismatch handling rule

If repository reality and target-state documentation differ, agents must:
1. classify repository truth explicitly
2. classify target-state intent explicitly
3. keep both visible without silent reconciliation
4. propose only the smallest safe correction path in the touched scope

## Ownership rule

This file owns operating posture only.
It does not own:
- product scope
- business contracts
- UI wording truth
- route architecture truth
- next-step prioritization
