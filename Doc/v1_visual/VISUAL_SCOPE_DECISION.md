# Visual scope decision

`v1_visual` is the current V1 visual specification. Its scope is controlled by route proof, V1 product documentation, state coverage, and explicit target status.

## Final rule

`v1_visual` now contains four levels:

1. **Current routed screens** — proven by `src/app/Routes/*.ts`.
2. **State/context variants** — no new route, but required by Angular branches, form states, empty states, recoverable errors, and business non-executable cases.
3. **Reusable component specs** — necessary because several pages are assemblies of finance components.
4. **Non-routed product targets** — preserved from the V1 screen spec, explicitly marked as not currently built.

## Guardrail

A target page must never be presented as implemented unless the route exists in the Angular route table. A variant page must never imply a backend capability that is not already represented by the code or the product spec.

## Evidence classification

| Statement | Classification |
|---|---|
| Current routed screens are proven by `FinanceFront/src/app/Routes/*.ts`. | PROVEN |
| State/context variants are allowed when the route, model, template branch, or V1 screen spec requires the state. | DECIDED |
| Non-routed product targets stay in the visual spec only when they answer a V1 need or legal/data-rights constraint. | DECIDED |
| Visual target coverage is not implementation proof. | PROVEN |
