# Online Draft variants

- [x] Regular/Free Regular/Arcade outfield selection with Deal/Free/Arcade keepers.
- [x] Real international rosters and preferred national squad numbers.
- [x] Authoritative consumed/reusable pools and each team's own roster ownership.
- [x] Compact available-card IDs; local browsing without rules/network commands.
- [x] Execute actual authored pool controls and affected Regular Draft regressions.
- [x] Register all new cases for deferred Mac execution and retain evidence.

Opening toss, Room handoff and Arcade pool returns remain separate work.
Run only `OnlineDraftVariants` (EditMode) / `OnlineDraftVariantScene` (PlayMode).

Live Editor compilation clean, separately from native tests. All native runs
passed with zero failures/skips:
- 38 all-mode/real-national-roster cases: `20261115T061621399Z`.
- 8 actual pool UI cases: `20261115T061953536Z`.
- 2 real catalog comparisons against existing DraftManager loaders: `20261115T062152557Z`.
- 2 full mixed-GK/outfield UI drafts: `20261115T062212546Z`.
Aggregate 50 distinct own-feature cases. All have separate deferred Mac entries.
Affected Regular authority40, serialized channels16 and actual scene20 passed at
`20261115T062317397Z`, `20261115T062326151Z`, `20261115T062341850Z`.
Native XML/progress is retained under TestResults/GameplayRegression/Runs.

All supported setup draft combinations now route through the normal online Draft
scene. Guest catalog browsing uses compatibility-hashed Resources and available
IDs; host still owns attributes/eligibility and acceptance. Available IDs keep
snapshots under the framing limit instead of resending an entire card catalog.
Arcade allows each manager to fill only their own roster with reusable candidates.
International rosters preserve national squad numbers and existing GK ranking.
Pagination controls are authored and hidden offline. Existing local Draft scripts
and save fallback remain inactive online. Frontend rules version is now v3.

The opening toss and actual Room start remain next. Arcade return-to-pool controls
are not implemented online. No paired Windows/Mac acceptance or latency result is
claimed; current exports predate these Draft slices. Broader user-run existing
Draft settings/order checks are listed in OnlineDraft/Verification.md; no full
EditMode or PlayMode suites were run.
