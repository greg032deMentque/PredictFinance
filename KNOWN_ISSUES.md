# Known issues and technical debt

Tracked deliberately rather than left implicit. Each item is a known deviation from the
architecture contract in [`AGENTS.md`](AGENTS.md), not a defect discovered in the wild.

## Architecture

**`BackPredictFinance.Services` still depends on `BackPredictFinance.ViewModels`.**
A layering violation: the service layer should not know about transport DTOs. Mapping
belongs at the API boundary. Left in place because unwinding it touches a broad surface;
it is contained and does not leak into the pattern engine.

**Part of the analysis-domain contract still lives under `BackPredictFinance.Common/AnalysisV1`.**
`Common` is meant to stay minimal and free of domain concepts. This contract should move
into the analysis capability that owns it.

**Legacy `DOUBLE_TOP` residue remains in the backend.**
A retired pattern whose compatibility shims have not been fully removed. It is not part of
the four supported continuation patterns.

## Documentation

**`FinanceFront/README.md` is obsolete.**
It still documents `GET /api/Trading/predict/{symbol}` as the active flow. That endpoint is
retired — see `BackPredictFinance.Tests/Api/TradingRetiredApiFeatureTests.cs`, which pins
the retirement.

## Security

**Development secrets remain in Git history.**
A JWT signing secret, a server-side pepper and seeded account passwords were committed in
the initial commit (`3dff86f`) and removed later. They are *development* values —
production injects its own configuration and never read them — and they have been rotated.
History has deliberately not been rewritten: on a public repository a rewrite leaves
orphaned commits reachable in GitHub's cache, so revocation, not rewriting, is what
actually removes the risk.

## Frontend testing

**The Angular app has minimal test coverage** (5 spec files). This is a standing project
decision recorded in `AGENTS.md`, not neglect: test effort is concentrated in the backend,
where the business truth lives.
