# Continuation pattern reference pack

This folder stores the business-reference material for the four continuation patterns targeted by the V1 multi-pattern direction.

These files are intentionally documentation-only.
They are not runtime proof.
They do not mean the active API path already supports these patterns end to end.
They exist to:
- stabilize vocabulary
- document the expected reading of each pattern
- keep detection / validation / invalidation / advice semantics coherent across future implementation steps
- give the specification a durable reference id for each pattern

## Pattern reference ids
- `PATTERN-REF-RECTANGLE-CONTINUATION` → `PATTERN-REF-RECTANGLE-CONTINUATION.md`
- `PATTERN-REF-SYMMETRICAL-TRIANGLE-CONTINUATION` → `PATTERN-REF-SYMMETRICAL-TRIANGLE-CONTINUATION.md`
- `PATTERN-REF-BULL-FLAG-CONTINUATION` → `PATTERN-REF-BULL-FLAG-CONTINUATION.md`
- `PATTERN-REF-BEAR-FLAG-CONTINUATION` → `PATTERN-REF-BEAR-FLAG-CONTINUATION.md`

## Anti-drift rules
- A pattern reference file defines business and interpretation intent only.
- No file in this folder is allowed to silently claim runtime support.
- Runtime support is considered real only when the full API path is wired and tested.
- If implementation behavior conflicts with one of these files, the mismatch must be reported explicitly.
- Detection semantics, validation semantics, invalidation semantics, target logic, and advice wording must remain separated.
- `mainPattern` remains a display-primary selection only. Alternatives must remain preserved separately.
