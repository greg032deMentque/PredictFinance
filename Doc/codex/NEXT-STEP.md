# Current status

## Done
- Product need clarified for a pedagogical investment-analysis product
- V1/V2/V3 planning defined
- V1 architecture direction validated: API as source of truth, AI optional
- Repository-aware `AGENTS.md` prepared
- Initial repository audit completed
- V1 contract freeze produced
- Minimal corrective pass applied to the contract freeze

## Next action
Start the implementation only from the frozen V1 contracts.

Recommended first implementation increment:
1. fix DI/foundation issues
2. extract API-side analysis contracts and service boundaries
3. avoid schema migration in the first increment
4. thin `ClientFinanceService` before changing business behavior

## Important rules
- no new broad audit before implementation
- API is the source of truth
- V1 must work without AI
- no mono-pattern hardcoding
- no frontend business truth
- no code comments unless explicitly required
- no schema cutover in the first increment

## Open confirmations before later steps
- whether frontend can switch immediately from `symbol` to `instrumentId`
- rollout sequence for legacy recommendation/history cutover
- provider strategy for French equities if Yahoo coverage is insufficient
- Angular local validation environment once dependencies are installed
