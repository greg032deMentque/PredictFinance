# Visual traceability gates

## Gate 1 — Current route proof

A page can be labelled `Current routed` only if it is present in `src/app/Routes/*.ts`.

## Gate 2 — Code-variant proof

A page can be labelled `Variant` if one of the following is true:

- an Angular template has a visible branch for the state;
- a current TypeScript model exposes the field that drives the state;
- the V1 screen spec explicitly requires the state.

## Gate 3 — Target proof

A page can be labelled `Target` only if it is present in V1 docs, gap register, legal/data-rights obligations, or retained benchmark pressure. Target pages must never be represented as implemented.

## Gate 4 — Rejected benchmark proof

External competitor patterns that do not fit V1 are documented as rejected target pressure, not silently added to product scope.

## Gate 5 — Link integrity

The package was checked so that local `href` references resolve inside `Doc/v1_visual`.
