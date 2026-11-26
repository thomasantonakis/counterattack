# Online outside-box goalkeeper shot decision

- [x] Reproduce the real shot reaching an unpublished keeper choice.
- [x] Publish defending-team hands/block decisions and the actual keeper actor.
- [x] Route authenticated commands and D/B keys to existing gameplay methods.
- [x] Cover both directions, ineligible/eligible blocks, goals and handball/red card.
- [x] Cover wrong team, inert guest calls, duplicate receipts, overlapping holds
  and cancelled decisions, without inventing rolls or outcomes.
- [x] Register each new/parameterized case separately for the deferred Mac run.

Full-game catalog is now **full-room-v10**; rebuild both clients. Wire remains v7.
When the only reachable keeper save hexes lie outside the keeper's own box,
the existing game asks D (dive with hands) or B (act as an outfield blocker).
Previously the adapter published `shot.pending` with no legal command. It now
publishes `shot.keeper-choice`, the defending keeper and two semantic choices.
The existing command dropdown labels describe the handball risk. Original
shot rules remain authoritative: a distant keeper cannot roll an outfield block,
an eligible keeper blocks with effective Tackling zero, and a successful hand
save outside the box follows the existing straight-red/free-kick path.

Compilation: clean through the open Editor, zero C# errors. Own-feature native
results: **14 passed, zero failed/skipped**:

- `OnlineOutsideKeeperShotEditorTests`: two at `20261116T075455273Z/results.xml`.
- `OnlineOutsideKeeperShotPlayModeTests`: 12 at `20261116T075621546Z/results.xml`.

All paths are under `TestResults/GameplayRegression/Runs/`. Tests use real shot
start/confirmation/goal targeting, a precomputed legal path, deterministic boards
and rigged dice. They assert actual continuation, rolls, goals, statistics and
sent-off outcome. The hand-save choice also asserts the offered save hex is
outside the box. These are isolated gameplay boards, not a paired full Room run.

Reproduction: `20261116T074932366Z` records four real `shot.pending` failures.
Retain fixture diagnostics separately: `074801889Z` used an origin with no legal
shooting paths; `075056835Z` reused the other team's sequence for the wrong-team
assertion; `075138089Z` exposed missing goal/scorer fixture dependencies; and
`075510542Z` expected readiness before the protocol's stale-revision check.
The fixture now asserts stale revision and fresh-revision NotReady separately.
An interrupted CLI poll did not restart the native run; its completed results
were retrieved with `test_status`. Assertions and existing shot rules are intact.

The Mac manifest has all 14 individual entries and validated fixture hashes;
its 13 tooling unit tests passed. Physical D/B input, the paired internet run,
full Room's resulting keeper-replacement UI and end-to-end latency remain
deferred acceptance work. Existing required-keeper native coverage covers that
replacement subsystem independently.

Affected native verification: **19 passed, zero failed/skipped**:
`OnlineGroundShotPlayModeTests.ShotAndSnapshotContinueThroughSave` eight at
`20261116T075654084Z`, `OnlineShotBlockPlayModeTests` ten at `20261116T075722761Z`,
and `LegacyGoalkeeperShotPlayModeTests` one at `20261116T075756232Z`.
Native XML/progress are retained in those Runs directories. No whole suite ran.

Windows exports succeeded: normal `build_51347a0652bc` at
`20261116T080033Z-normal-Windows-build`, Room `build_6038c70f3b57` at
`20261116T080124Z-Room-Windows-build`, prepared `build_06af51455b08` at
`20261116T080152Z-Gameplay-Windows-build`. Known nonfatal EOS vswhere diagnostic
and nine normal/two proof warnings remain; zero C# errors.
Packaged checks passed: `20261116T080206780Z-Room-local`, `20261116T080220737Z-Gameplay-local`, and `20261116T080228288Z-Room-login` (liveness notifications registered).
