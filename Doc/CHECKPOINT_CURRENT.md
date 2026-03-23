# Docs/CHECKPOINT_CURRENT.md

## Contract mode

[ARCHITECTURE_ARBITRATION]

## Resume point

- Last completed prompt message → Message 64
- Next message to execute → Message 65
- Next blocks to execute → P06C-B003

## Implementation allowed now

[NO]

## Conflict check

[NO CONFLICT]

## Binding source

[checkpoint]

## Hard stop status

[ACTIVE]

---

## Locked blocks already validated

- P04C-B004 - Must Api reject deep business rules? - LOCKED
- P04C-B005 - Can Front contain presentation, interaction, and passive mapping only? - LOCKED
- P04C-B006 - Must Front reject decision derivation? - LOCKED
- P04C-B007 - Must Front reject shadow enums and critical business computation? - LOCKED
- P05-B001 - May Application depend on Domain? - LOCKED
- P05-B002 - May Patterns depend on Domain? - LOCKED
- P05-B003 - May Scoring depend on Domain? - LOCKED
- P05-B004 - May Decisions depend on Domain? - LOCKED
- P05-B005 - May Infrastructure depend on Application abstractions? - LOCKED
- P05-B006 - May Api depend on Application? - LOCKED
- P05-B007 - Must Domain reject all outer-layer dependencies? - LOCKED
- P05-B008 - Where is dependency inversion mandatory? - LOCKED
- P07A-B001 - Is MarketSnapshot a first-class root entity? - LOCKED
- P07A-B002 - Is AnalysisBatch a first-class root entity? - LOCKED
- P07A-B003 - Must MarketSnapshot be immutable after creation? - LOCKED
- P07A-B004 - Must AnalysisBatch carry global truth beyond selected candidate? - LOCKED
- P07A-B005 - Must AnalysisBatch remain tied to exactly one frozen snapshot? - LOCKED
- P07B-B001 - Is AnalysisCandidate subordinate to batch truth rather than a root? - LOCKED
- P07B-B002 - Must AnalysisCandidate rank avoid exclusive-truth semantics? - LOCKED
- P07B-B003 - Is PatternInspection subordinate to batch truth rather than independent truth? - LOCKED
- P07B-B004 - Is DecisionSignal derived business output rather than primary analysis truth? - LOCKED
- P07B-B005 - Is AuditEvent a first-class traceability record? - LOCKED
- P07B-B006 - Is ModelArtifactReference a first-class traceability record? - LOCKED
- P09-B001 - Must Application define explicit internal commands? - LOCKED
- P09-B002 - Must Application define explicit internal results? - LOCKED
- P09-B003 - Must Application reject reusing Api DTOs and Domain entities as internal orchestration contracts? - LOCKED
- P11A-B001 - Must stable public backend codes be explicitly inventoried? - LOCKED
- P11A-B002 - Must stable internal backend-only codes be explicitly inventoried? - LOCKED
- P11A-B003 - Must backend remain sole owner of closed-domain codes? - LOCKED
- P11A-B004 - Must front reject inventing shadow closed-domain codes? - LOCKED
- P11B-B001 - Must score categories be explicitly separated? - LOCKED
- P11B-B002 - Must ambiguous single-score semantics be rejected? - LOCKED
- P11B-B003 - Must fake probability vocabulary be explicitly forbidden? - LOCKED
- P11B-B004 - Must score meaning remain explicit at contract level? - LOCKED
- P11C-B001 - Must abstention statuses remain separate from pattern codes? - LOCKED
- P11C-B002 - Must internal decision posture remain richer than simplified actions? - LOCKED
- P11C-B003 - Must simplified actions remain outward projection only? - LOCKED
- P11C-B004 - Must reverse inference from action back to business truth be forbidden? - LOCKED
- P12-B001 - Must decision engine consume explicit derived context rather than raw provider business truth? - LOCKED
- P12-B002 - Must decision engine outputs remain business-semantic rather than provider-semantic? - LOCKED
- P12-B003 - Must scoring provider stop before business decision semantics? - LOCKED
- P12-B004 - Must scoring provider outputs remain analysis/scoring only? - LOCKED
- P13-B001 - Must decision engine remain the sole interpreter of analysis into business truth? - LOCKED
- P13-B002 - Must decision engine reject provider-implied decision semantics? - LOCKED
- P13-B003 - Must business decision semantics be fully internal and non-leakable? - LOCKED
- P13-B004 - Must reviewers reject any reintroduction of Python sovereignty immediately? - LOCKED
- P13-B005 - Must reviewers reject any reintroduction of fake single-pattern truth immediately? - LOCKED
- P06A-B001 - Must global analysis start from explicit request context? - LOCKED
- P06A-B002 - Must global analysis reject implicit environmental enrichment at start? - LOCKED
- P06A-B003 - Must global analysis start context be fully sufficient without external reconstruction? - LOCKED
- P06A-B004 - Must candidate generation attach to persisted batch truth? - LOCKED
- P06A-B005 - Must batch truth remain broader than selected candidate only? - LOCKED
- P06B-B001 - Must inspection start from existing persisted batch? - LOCKED
- P06B-B002 - Must inspection derive new analytical truth from batch? - LOCKED
- P06B-B003 - Must inspection expose explicit provenance to inspected batch truth? - LOCKED
- P06B-B004 - May inspection select or filter candidates independently of batch definition? - LOCKED
- P06B-B005 - Must inspection preserve full candidate set visibility without loss or masking? - LOCKED
- P06C-B001 - Must refresh create a new batch instead of overwriting old truth? - LOCKED
- P06C-B002 - Must refresh keep explicit lineage to source batch? - LOCKED


---

## Locked decision summaries

- P04C-B004 -> Api must reject deep business rules = YES
- P04C-B005 -> Front may contain presentation, interaction, and passive mapping only = YES, WITH HARD BOUNDARY
- P04C-B006 -> Front must reject decision derivation = YES
- P04C-B007 -> Front must reject shadow enums and critical business computation = YES
- P05-B001 -> Application -> Domain = YES, WITH HARD BOUNDARY
- P05-B002 -> Patterns -> Domain = YES, WITH HARD BOUNDARY
- P05-B003 -> Scoring -> Domain = YES, WITH HARD BOUNDARY
- P05-B004 -> Decisions -> Domain = YES, WITH HARD BOUNDARY
- P05-B005 -> Infrastructure -> Application abstractions = YES, WITH HARD BOUNDARY
- P05-B006 -> Api -> Application = YES, WITH HARD BOUNDARY
- P05-B007 -> Domain must reject all outer-layer dependencies = YES
- P05-B008 -> Dependency inversion is mandatory only at seams where Domain/Application-owned semantics are realized by outer layers = YES, WITH HARD BOUNDARY
- P07A-B001 -> MarketSnapshot is a mandatory first-class root entity = YES
- P07A-B002 -> AnalysisBatch is a mandatory first-class root entity = YES
- P07A-B003 -> MarketSnapshot must be immutable after creation = YES
- P07A-B004 -> AnalysisBatch must carry global truth beyond any selected candidate = YES
- P07A-B005 -> Each AnalysisBatch must remain tied to exactly one frozen snapshot = YES
- P07B-B001 -> AnalysisCandidate must remain subordinate to batch truth rather than root-level = YES
- P07B-B002 -> AnalysisCandidate rank must avoid exclusive-truth semantics = YES
- P07B-B003 -> PatternInspection must remain subordinate to batch truth rather than independent truth = YES
- P07B-B004 -> DecisionSignal must be derived business output rather than primary analysis truth = YES
- P07B-B005 -> AuditEvent must be a first-class traceability record = YES
- P07B-B006 -> ModelArtifactReference must be a first-class traceability record = YES
- P09-B001 -> Application must define explicit internal commands = YES
- P09-B002 -> Application must define explicit internal results = YES
- P09-B003 -> Application must reject reusing Api DTOs and Domain entities as internal orchestration contracts = YES
- P11A-B001 -> Stable public backend codes must be explicitly inventoried = YES
- P11A-B002 -> Stable internal backend-only codes must be explicitly inventoried = YES
- P11A-B003 -> Backend must remain sole owner of closed-domain codes = YES
- P11A-B004 -> Front must reject inventing shadow closed-domain codes = YES
- P11B-B001 -> Score categories must be explicitly separated = YES
- P11B-B002 -> Ambiguous single-score semantics must be rejected = YES
- P11B-B003 -> Fake probability vocabulary must be explicitly forbidden = YES
- P11B-B004 -> Score meaning must remain explicit at contract level = YES
- P11C-B001 -> Abstention statuses must remain separate from pattern codes = YES
- P11C-B002 -> Internal decision posture must remain richer than simplified actions = YES
- P11C-B003 -> Simplified actions must remain outward projection only = YES
- P11C-B004 -> Reverse inference from action back to business truth must be forbidden = YES
- P12-B001 -> Decision engine must consume explicit derived context rather than raw provider business truth = YES
- P12-B002 -> Decision engine outputs must remain business-semantic rather than provider-semantic = YES
- P12-B003 -> Scoring provider must stop before business decision semantics = YES, WITH HARD BOUNDARY
- P12-B004 -> Scoring provider outputs must remain analysis/scoring only = YES
- P13-B001 -> Decision engine must remain sole interpreter of analysis into business truth = YES, WITH HARD BOUNDARY
- P13-B002 -> Decision engine must reject provider-implied decision semantics = YES
- P13-B003 -> Business decision semantics must be fully internal and non-leakable = YES, WITH HARD BOUNDARY
- P13-B004 -> Reviewers must reject any reintroduction of Python sovereignty immediately = YES
- P13-B005 -> Reviewers must reject any reintroduction of fake single-pattern truth immediately = YES
- P06A-B001 -> Global analysis must start from explicit request context = YES, WITH HARD BOUNDARY
- P06A-B002 -> Global analysis must reject implicit environmental enrichment at start = YES, WITH HARD BOUNDARY
- P06A-B003 -> Global analysis start context must be fully sufficient without external reconstruction = YES, WITH HARD BOUNDARY
- P06A-B004 -> Candidate generation must attach to persisted batch truth = YES, WITH HARD BOUNDARY
- P06A-B005 -> Batch truth must remain broader than selected candidate only = YES, WITH HARD BOUNDARY
- P06B-B001 -> Inspection must start from existing persisted batch = YES, WITH HARD BOUNDARY
- P06B-B002 -> Inspection must not derive new analytical truth from batch = NO, WITH HARD BOUNDARY
- P06B-B003 -> Inspection must expose explicit provenance to inspected batch truth = YES, MANDATORY
- P06B-B004 -> Inspection must not select or filter candidates independently of batch definition = NO, WITH HARD BOUNDARY
- P06B-B005 -> Inspection must preserve full candidate set visibility without loss or masking = YES, WITH HARD BOUNDARY
- P06C-B001 -> Refresh must create a new batch instead of overwriting old truth = YES, WITH HARD BOUNDARY
- P06C-B002 -> Refresh must keep explicit lineage to source batch = YES, MANDATORY

---

## Locked structural constraints

- API = boundary transport-only
- Front = présentation, interaction et passive mapping uniquement
- Front rejette toute dérivation de décision
- Front rejette shadow enums et calcul métier critique
- Application -> Domain = autorisé avec hard boundary
- Patterns -> Domain = autorisé avec hard boundary
- Scoring -> Domain = autorisé avec hard boundary
- Decisions -> Domain = autorisé avec hard boundary
- Infrastructure -> Application abstractions = autorisé avec hard boundary
- Api -> Application = autorisé avec hard boundary
- Domain rejette toute dépendance vers les couches externes
- Dependency inversion est obligatoire aux seams où des sémantiques détenues par Domain ou Application doivent être réalisées par des couches externes
- MarketSnapshot est une first-class root entity
- AnalysisBatch est une first-class root entity
- MarketSnapshot est immuable après création
- AnalysisBatch porte une vérité globale au-delà de tout candidat sélectionné
- Chaque AnalysisBatch est rattaché à exactement un frozen snapshot
- AnalysisCandidate est subordonné à la vérité de batch et n’est pas une root entity
- Le rank d’un AnalysisCandidate ne doit jamais porter une sémantique de vérité exclusive
- PatternInspection est subordonné à la vérité de batch et n’est pas une vérité indépendante
- DecisionSignal est une sortie métier dérivée et non une vérité d’analyse primaire
- AuditEvent est un first-class traceability record
- ModelArtifactReference est un first-class traceability record
- Application must define explicit internal commands as orchestration contracts for use-case entry
- Application must not reuse Api DTOs as command contracts
- Application must not reuse Domain entities as command contracts
- Application must define explicit internal results as orchestration contracts for use-case exit
- Application must not reuse Api DTOs as result contracts
- Application must not reuse Domain entities as result contracts
- Application must reject reusing Api DTOs as internal orchestration contracts
- Application must reject reusing Domain entities as internal orchestration contracts
- Application commands and results must remain Application-owned contracts
- Explicit use-case boundaries must not be blurred by transport reuse or entity reuse
- Anti-DTO leakage and anti-entity leakage remain mandatory at the Application boundary
- Stable public backend codes must be explicitly inventoried
- The inventory must preserve backend ownership of public closed-domain semantics
- Public code semantics must not be left implicit across transport, persistence, or client usage
- Explicit inventory must support auditability, anti-shadow-codes discipline, and long-term contract stability
- Public code inventory must not transfer semantic ownership away from the backend
- Stable internal backend-only codes must be explicitly inventoried
- The inventory must preserve backend ownership of internal closed-domain semantics
- Internal stable codes must not remain implicit or implementation-scattered
- Explicit inventory must support auditability, anti-shadow-codes discipline, and long-term semantic stability
- Internal code inventory must not be treated as optional merely because the codes are backend-only
- The backend must remain the sole owner of closed-domain codes
- Closed-domain semantic authority must not be shared with front clients or provider layers
- Cross-client consistency must derive from backend-owned code meaning
- Anti-shadow-domain discipline must remain explicit and reviewable
- Auditability of code meaning must depend on backend authority, not consumer inference
- The front must reject inventing shadow closed-domain codes
- The front may project backend-owned codes to labels, colors, or badges, but must not create competing code semantics
- Cross-client consistency must remain anchored in backend-owned code families
- Anti-shadow-domain discipline must remain enforced at the client boundary
- Auditability must not depend on front-invented semantic aliases
- Score categories must remain explicitly distinct wherever their meanings differ
- Analytical score meaning must not be collapsed into decision-output meaning
- Internal score meaning must not be collapsed into outward simplified projection meaning
- Separation must preserve semantic honesty, explainability, and contract clarity
- No score category may borrow authority from another category through naming ambiguity
- The backend remains the sole owner of these semantic distinctions
- A single score must not carry multiple unresolved meanings
- Score interpretation must not depend on reader inference, display context, or downstream convention
- Any score exposed or persisted must have one explicit semantic meaning
- Ambiguity must be rejected wherever it would blur analysis truth, derived business output, or outward simplification
- Semantic explicitness remains mandatory for auditability and explainability
- Fake probability vocabulary is explicitly forbidden wherever the underlying semantics are not true probabilities
- No score, posture, or derived output may be labeled in a way that implies calibrated probability without actual probability semantics being contractually established
- Contract language must remain semantically honest across all consumers
- Interpretation must remain safe against overconfidence, false certainty, and probabilistic overreading
- Semantic stability must be preserved across persistence, transport, and consumption layers
- The backend remains responsible for preventing vocabulary inflation
- Every contract-level score must carry one explicit and stable meaning
- Contract consumers must not need contextual guessing to interpret a score
- Score meaning must remain consistent across consumers and over time
- Contract wording must not collapse distinct semantic categories into one unlabeled numeric channel
- Interpretation safety must be preserved by explicit semantic binding at the contract level
- The backend must remain the authoritative source of that meaning
- Abstention statuses must remain a separate semantic category from pattern codes
- A pattern code must never be used to represent abstention
- An abstention status must never impersonate analytical pattern truth
- Taxonomy must preserve the distinction between identified pattern meaning and deliberate non-assertion
- Persistence and contracts must keep that distinction interpretable over time
- The backend remains the sole owner of this semantic separation
- Internal decision posture must remain semantically richer than simplified outward actions
- Simplified actions must not replace or redefine internal posture meaning
- Outward projection must remain a reduction of richer internal truth, not its semantic equal
- Persistence and reviewability must preserve internal posture richness
- Consumers must not treat simplified actions as the full business state
- The backend remains the sole owner of the richer internal semantics
- Simplified actions must remain outward projection only
- Simplified action labels must not become the authoritative form of business truth
- Richer internal decision posture must remain semantically upstream from simplified actions
- Persistence and reviewability must preserve richer internal meaning beyond projection
- Consumers must not be allowed to treat simplified actions as full internal state
- The backend remains the sole owner of the richer meaning from which projection is derived
- Reverse inference from simplified action back to richer business truth is forbidden
- A simplified action must not be used to reconstruct internal posture, analytical strength, or omitted semantic nuance
- Projection must remain one-way from richer internal truth toward outward simplification
- Consumers must not treat action labels as evidence of the full underlying business state
- Persistence and auditability must rely on explicit richer semantics, not reconstruction from projection
- The backend remains the sole authoritative owner of business truth
- The decision engine must consume explicit derived context, not raw provider business truth
- Provider-originating semantics must not cross the engine boundary as authoritative business meaning
- Business-semantic ownership at the engine input boundary must remain backend-controlled
- Engine input meaning must remain stable even if scoring providers are replaced
- The engine boundary must preserve derived-context discipline and anti-provider sovereignty
- No provider vocabulary may become implicit business truth through engine input shape
- Decision engine outputs must remain business-semantic
- Provider semantics must not define the meaning of engine outputs
- Output meaning must remain stable across provider replacement
- Derived business output must remain clearly distinct from upstream provider analysis/scoring semantics
- Downstream consumers must receive backend-owned decision meaning, not provider-owned interpretation
- Anti-provider sovereignty must remain enforced at the engine output boundary
- .NET backend = unique owner de la vérité métier
- Python = non souverain
- Python ne décide jamais Buy/Sell/Hold
- Front = passif sur la logique métier critique
- Frozen market snapshot est obligatoire
- Silent recomputation est interdite
- Source of truth = frozen snapshot + persisted analysis batch
- Multi-candidate analysis ne doit pas être réduit à une fausse vérité mono-pattern
- Forced fake 100% distribution est interdite
- Abstention n’est pas un business pattern
- Score semantics must be explicit
- Internal decision semantics must be richer than Buy/Sell/Hold
- Buy/Sell/Hold est uniquement une projection simplifiée outward
- Full auditability is mandatory
- pas d’implémentation avant le switch contractuel
- pas de réouverture des blocs LOCKED