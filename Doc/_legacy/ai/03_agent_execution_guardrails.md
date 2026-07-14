# Agent execution guardrails

## Purpose

This file defines how a coding agent must behave while executing a task.
It does not define what the next task should be.

## Execution posture

- work only on the touched scope
- keep changes as narrow as possible
- classify every important claim using repository evidence
- stop before inventing missing architectural facts
- keep repository truth and target-state intent separately visible
- report residual risks explicitly

## When a repository mismatch is encountered

If a touched path depends on a known repository mismatch:
1. state the repository truth
2. state the target-state intent
3. explain why the mismatch matters for the touched scope
4. propose the smallest safe correction path
5. avoid broad opportunistic redesign

## When a product ambiguity is suspected

Agents must not reopen frozen V1 decisions by default.
They may escalate only if:
- repository evidence contradicts the current documentation, or
- a new explicit human decision replaces the current baseline

## Prompt usage rule

Files under `Doc/codex/*` may help structure execution prompts.
They must never be treated as canonical owners of:
- product scope
- business contracts
- UI wording truth
- route architecture truth
- next-step prioritization
