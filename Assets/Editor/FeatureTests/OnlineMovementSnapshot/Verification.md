# Online movement snapshot offers

- [x] Reproduce missing running-player shoot and exhausted-pace answer commands.
- [x] Route offered snapshots through shared source helpers; preserve offline S/X rules.
- [x] Offer shoot/continue at exhausted pace and preserve running movement/forfeit choices.
- [x] Cover both teams/directions, guest rejection, stale decisions, cancellation and overlapping holds.
- [x] Complete actual snapshot/blocker/shooter/save/handling/distribution continuations.
- [x] Prove one mover/shot registration and retain offline accounting parity.
- [x] Register every parameterized case separately for the deferred Mac run.

Full Room is now **v12**, wire remains v7. A dribbler can choose S while retaining
the published continuation hexes. At exhausted pace, the explicit snapshot/continue
decision releases the waiting movement coroutine. The online offer uses the same
snapshot acceptance and exhausted-decline helpers as offline input; running X
continues through the existing dribble-forfeit handler. Final-extra-action shot
suppression and possession/origin validation remain in force.

Compilation: zero C# errors through the open Editor. Own-feature native results:
**49 passed, zero failed/skipped**:

- Five `OnlineMovementSnapshotEditorTests`: `20261116T170631426Z/results.xml`.
- 44 `OnlineMovementSnapshotPlayModeTests`: `20261116T170645384Z/results.xml`.

The native feature uses actual movement, legal shooting paths, role-owned commands
round-tripped through the wire, real snapshot blocker movement, and rigged dice.
Eight complete shot/save/handling cases verify possession/distribution and no
double movement continuation. Other cases cover real extra steps, stale previews,
holds/cancellation and guest inactivity. No live save or designer-owned layout is
used as a mutable fixture. The 16-case real missing-command reproduction is at
`20261116T170118933Z`; the first 16 green cases are retained at `170346893Z`
without adding them to the final distinct count.

Affected verification: **12 passed, zero failed/skipped**:
eight offline `DecisionTimingReportedIssuesPlayModeTests.SnapshotConsumesOneMove`
at `20261116T170815400Z`, two exact keeper-wall loss continuations at
`170907308Z`/`170923993Z`, and two exact existing safe/held dribble cases at
`170940669Z`/`170957330Z` (all have prefix `20261116T`). Native XML/progress are
under `TestResults/GameplayRegression/Runs/`. No complete suite was run.

All 49 new cases are individually registered in the deferred Mac manifest;
definition hashes/inventory and 13 verifier tooling unit tests pass. Exports
currently precede v11/v12 and must be refreshed before paired use. Physical S/X
and paired full-Room acceptance/latency remain unexecuted.

For future broader user-run checks, use
`Assets/Tests/PlayMode/Features/DecisionTiming` fixture
`DecisionTimingReportedIssuesPlayModeTests` (snapshot accounting), and
`Assets/Tests/PlayMode/Features/OnlineDribble` fixture/category
`OnlineDribblePlayModeTests`/`OnlineDribble` (continuation ownership).
