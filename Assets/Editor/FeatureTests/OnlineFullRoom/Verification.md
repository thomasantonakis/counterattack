# Full online Room

- [x] Author normal Room-derived pitch, 36 inspectable real tokens and controls.
- [x] Bind real drafted attributes, goalkeeper/bench roles and opening formation.
- [x] Keep guest authority inactive while projecting the complete roster.
- [x] Execute focused host/guest actual-scene tests.
- [x] Connect persistent native frontend to continuous full Room transport.
- [ ] Normal pause/substitution/shootout menus and full-match acceptance.

This owned scene derives from Room; original designer-owned Room edits/layout
are preserved. No fixed six-token initialization is used. Mac paired testing is
still deferred, and unexecuted work is not an acceptance claim.

Focused native evidence: `TestResults/GameplayRegression/Runs/20261115T073502356Z/results.xml`
contains 16 passed, zero failed/skipped: eight host/guest roster/formation cases,
four real kickoff cases and four detached full guest projection cases. Compilation
passed separately. This tests both squad sizes and attacking directions.

Earlier retained failures exposed a real startup event-history leak (formation
and initial ball placement now suppress logging) and guest banner construction
of an authority timer (guest UI no longer reads the authority service). Fixture
fixes install the required online authority scope and assert the existing rule
that kickoff automatically starts its mandatory standard pass.

Guest state updates actual hex occupancy, rendered actors, team score and match
clock without enabling rules, rolling dice or appending events. Bench actors are
included in snapshots; complete substitution/status/direction replication and
normal match menus remain work in progress.

Final isolation rerun: `TestResults/GameplayRegression/Runs/20261115T074316240Z/results.xml`: 16 passed, zero failed/skipped. Prior application intro/save context, logging caches, match/ball references, random state and time scale are restored. All 16 cases are registered separately for the deferred Mac run.

Native handoff and four additional encoded real Room kickoff cases are recorded in `../OnlineRoomHandoff/Verification.md`; the full fixture now contains 20 passing cases. Internet pairing remains deferred.
