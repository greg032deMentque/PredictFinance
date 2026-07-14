# PredictFinance V1 — UX/UI Evolution Matrix

| Priority | Area | Evolution idea | Why it helps | Risk of drift | Recommendation |
|---|---|---|---|---|---|
| P0 | Entry flow | Keep `index.html` as login-only and make role selection explicit | Prevents IA leakage before auth and clarifies admin vs user pathway | Low | Keep |
| P0 | Role architecture | Split future implementation into `AnonymousShell`, `UserShell`, `AdminShell` | Cleaner architecture than a single conditional shell | Low | Strongly recommended |
| P0 | Recommendation consistency | Central reusable component for held / not-held recommendation rendering | Avoids wording drift across pages | Low | Strongly recommended |
| P0 | State semantics | Central component family for outcome, PEA, support, availability and freshness chips | Enforces same business state = same wording/chip/icon everywhere | Low | Strongly recommended |
| P1 | Home dashboard | Replace navigation-heavy home with real data summary cards | Better compliance with v3 mandatory home blocks | Medium | Next pass |
| P1 | Empty/loading/error states | Add standardized page-state patterns for each canonical page | Required by implementation checklist and improves real UX quality | Low | Next pass |
| P1 | Alternative patterns | Normalize a reusable “compatible alternatives” block | Required by anti-drift rule and improves analysis understanding | Low | Next pass |
| P1 | History UX | Add stronger persisted snapshot timeline language and cards | Makes the persistence model clearer and more trustworthy | Low | Next pass |
| P1 | Snapshot compare | Add explicit non-comparability banners and changed-field emphasis | Improves trust and avoids fake deltas | Low | Next pass |
| P1 | Admin users | Add edit drawer / modal pattern for role and status changes | Better admin usability without crowding the table | Medium | Recommended |
| P1 | Account UX | Add segmented settings sections: profile, preferences, security | Improves self-service clarity for users | Low | Recommended |
| P1 | Notifications | Add filter tabs by category and unread status | Better discoverability and less clutter | Low | Recommended |
| P2 | Onboarding | Add “first value” guidance based on empty watchlist / empty portfolio | Better beginner experience | Low | Good addition |
| P2 | Help center | Add cross-links from results, history and account into help content | Better guided learning and lower confusion | Low | Good addition |
| P2 | Design system | Create explicit reusable Bootstrap component naming conventions | Easier implementation and lower duplication | Low | Good addition |
| P2 | Admin tables | Standardize table toolbar pattern: search + filters + export + refresh | Stronger admin consistency | Low | Good addition |
| P2 | Mobile nav | Show current section title more strongly on mobile | Explicitly aligned with v3 nav rules | Low | Good addition |
| P2 | Account security | Add recent sessions / device history mock page | Increases perceived security and realism | Medium | Optional |
| P2 | User profile | Add profile completion / first-run progress card | Helpful for onboarding and retention | Medium | Optional |
| P3 | Accessibility | Add accessibility review matrix: contrast, keyboard, focus, semantics | Improves implementation quality and auditability | Low | Good future step |
| P3 | Analytics UX | Add instrumentation notes per page for product review | Helps product iteration without altering UI semantics | Medium | Optional |
| P3 | Admin auditability | Add dedicated “why this state exists” explainability side panel | Strong governance value for admin reviewers | Medium | Optional |
