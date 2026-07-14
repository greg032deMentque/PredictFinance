PredictFinance V1 - role-entry full site

Key architecture updates
- index.html is now the login page only
- before login, the user sees no app shell, no sidebar and no internal pages
- two mock credentials are visible on the login page:
  - user@mock.local / UserDemo!23 -> user-home.html
  - admin@mock.local / AdminDemo!23 -> admin-overview.html
- separated post-login pathways:
  - user space
  - admin space

New UX pages added
- onboarding-empty.html
- help-center.html
- admin-users.html

Why these additions
- onboarding-empty: first-run UX without replacing core business pages
- help-center: guided explanations and product limits in one place
- admin-users: admin manages users, while user only manages self information in account.html

Important consistency checks
- kept held / not-held recommendation split where recommendation appears
- kept market reading, support reading, PEA, recommendation and data completeness visually separate
- admin and user now feel like two coherent product spaces after login, while login itself remains a clean isolated entry


Additional review files
- ARCHITECTURE_REVIEW.md
- UI_DISPLAY_OBJECTS.ts.md
- UX_UI_EVOLUTION_MATRIX.md


Stable version updates included
- strengthened user home into a real data dashboard
- standardized page states: empty, loading, error, business non-executable
- normalized alternative compatible patterns visibility on result page
- strengthened persisted history semantics with timeline cards
- improved comparison page with explicit non-comparability and change summaries
- stabilized admin users page with normalized table and coherent filters
- added page-states.html
- added account-security.html

Matrix evolutions integrated in this stable pass
- role entry kept strict
- recommendation consistency preserved
- state semantics standardized
- home dashboard improved
- admin table toolbar standardized
- history UX improved
- snapshot compare improved
- account security page added


UX corrections added in this package
- user-home.html corrected into a more explicit command center
- help-center.html made more contextual and action-oriented
- account.html segmented more clearly for self-management
- admin-data-quality.html linked more strongly to user impact
- PERSONAS.md added with full user/admin personas and simulation findings


Professional UX package additions
- PERSONAS.md
- USER_JOURNEYS.md
- ADMIN_JOURNEYS.md
- UX_DEFECT_LOG.md
- UX_CORRECTION_TRACEABILITY.md
- UX_TEST_SCENARIOS.md
- UX_PRIORITY_MATRIX.md
- UX_ACCEPTANCE_CRITERIA.md

Corrected pages in this package
- user-home.html
- help-center.html
- account.html
- admin-data-quality.html


Additional professional update in this package
- DIRECT_CORRECTIONS_APPLIED.md
- UX_SCORED_MATRIX.md

Pages directly corrected in this pass
- index.html
- login.html
- user-home.html
- watchlist.html
- portfolio.html
- analysis-entry.html
- analysis-result.html
- snapshot-comparison.html
- design-system.html
