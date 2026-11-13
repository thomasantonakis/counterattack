# Online goalkeeper distribution

- [x] Reproduce the missing command boundary after a real headed catch.
- [x] Owned quick-throw / Final Third choices without remote keys or guest rules.
- [x] Quick-throw legal preview/confirm and collection through the real automatic start.
- [x] Final Third continuation into drop or high kick, both directions.
- [x] Focused native tests, compilation and affected checks.

Paired acceptance is deferred. This feature does not imply complete keeper,
shot, restart or full-match coverage.

Compilation separately clean. `OnlineKeeperDistributionPlayModeTests` /
category `OnlineKeeperDistribution`: **14 distinct PlayMode cases passed**, zero
failed/skipped, in `TestResults/GameplayRegression/Runs/`:

- `20261113T223303923Z/results.xml`: 12 Home cases covering quick throw, Final
  Third drop/kick, both directions and optional overlapping holds. Guest calls,
  wrong-team/replayed commands, old pause views and a ball that has left the
  keeper cannot choose distribution or consume a new action.
- `20261113T223552613Z/results.xml`: 2 Away cases follow real possession change,
  free header, headed keeper catch, owned distribution and completed quick throw
  in both directions. Home cannot submit the Away keeper's choice.

The initial 6 missing-distribution failures are retained in
`20261113T222837514Z/results.xml`. An intermediate 4 passed / 2 failed run in
`20261113T223013569Z/results.xml` exposed a fixture assumption: BroadcastQuickThrow
already activates targeting through OfferStandardGroundBallPass, so the test
must not require an extra start command. Its actual target/collection/roll/stat
assertions are preserved. All 6 then passed in `20261113T223140371Z/results.xml`
before expanding the hold and Away coverage.

Affected Final Third category: **6 passed**, zero failed/skipped, in
`20261113T223415458Z/results.xml`. No complete suites ran.
Affected `OnlineManualControls`: **7 passed**, zero failed/skipped, in
`20261113T223636667Z/results.xml`.
Broad user-run checks
if needed: `Assets/Tests/PlayMode/Features/HeadedBackPass/`,
`Assets/Tests/PlayMode/Features/GoalkeeperPickup/` and
`DecisionTimingPlayModeTests.KeeperFollowup` for other offline keeper paths.

Keeper high kick coverage currently reaches real high-pass targeting; subsequent
kick accuracy/landing, general keeper box moves, shot saves, loose-ball outcomes
and restarts remain separate integration work. Existing Windows exports recorded
by OnlineHeaders/Verification.md precede this distribution batch; refresh before
paired acceptance. Exact Mac commands remain in Documentation/Setup/OnlineRoomProof.md.
