# Prompt — finalize the V1 documentation baseline

Treat this prompt as a binding documentation-closing contract.

Your mission is to perform a strict finalization pass on the real documentation pack of this repository and return a corrected zip containing all updated files.

You must not stay at advisory level.
You must perform the real documentation work on the real files.

## Objective

Close the V1 documentation baseline so it becomes:
- coherent across files,
- explicit about what is canonical,
- explicit about what is closed versus still repository-misaligned,
- safe for coding agents,
- readable for humans,
- traceable for product decisions.

## Non-negotiable rules

- Repository truth wins over assumptions.
- Do not invent implemented architecture that does not exist in the repository.
- Do not silently reconcile contradictions.
- If two documents overlap, define one canonical owner and make the others align to it.
- If a notion is not fully closed, either close it explicitly or mark it as intentionally not closed.
- Do not keep duplicate normative statements across multiple files unless one is clearly declared as derived.
- Do not broaden V1 scope beyond French listed equities.
- Do not imply ETF runtime support in V1.
- Keep all prompts and agent-facing instructions in English.
- Keep product docs readable for humans.
- Keep backend-owned truth clearly separated from UI wording.

## Mandatory deliverables

1. A final pass over all V1 documentation files.
2. A canonical-map document that states:
   - which files are canonical,
   - which files are derived summaries,
   - which files are agent-operating docs,
   - which files are historical or roadmap-oriented,
   - which known gaps are code/doc mismatches rather than spec ambiguities.
3. A final baseline-status document that says:
   - what is frozen for V1,
   - what is out of scope,
   - what remains a real repository mismatch,
   - whether the V1 documentation baseline is considered closed.
4. All corrected documentation files inside one zip.

## Minimum checks to perform

- cross-check PatternStatus wording versus canonical status codes
- cross-check recommendation wording versus holding-context rules
- cross-check PRU wording versus derived-versus-stored truth
- cross-check AnalysisOutcome visibility across high-level product docs
- cross-check admin perimeter consistency across all docs
- cross-check support-reading role versus recommendation composition rules
- cross-check batch-nightly wording so V1/V2 remains unambiguous
- cross-check AI/Codex orchestration files so they do not claim unfinished closures as still open
- cross-check README reading order and canonical anchors
- cross-check that UI wording docs do not contradict product contracts

## Output contract

Return:
- the final corrected zip,
- the exact files changed,
- the reason for each change,
- any remaining repository-truth mismatches that are intentionally documented rather than hidden.

If the documentation is closed, say so explicitly.
If it is not closed, state exactly why.

Do not provide generic advice.
Do not provide partial summaries without editing the real files.
