# Online goals and kick-off

- [x] Reproduce missing goal state with actual on-target shots in both directions.
- [x] Stream goal celebration, formation reset and ball return through existing presentation frames.
- [x] Prevent normal actions during goal reset; publish score and conceding possession.
- [x] Typed formation selection/movement, team confirmation and taker selection.
- [x] Reuse existing own-half, center-circle and kick-off pass rules.
- [x] Execute expanded focused tests and inspect native artifacts.
- [ ] Refresh Windows exports and packaged local checks.
- [ ] Paired Windows/Mac acceptance (deferred).

Fixture `OnlineGoalPlayModeTests`, category `OnlineGoals`, in
`Assets/Tests/PlayMode/Features/OnlineGoals/`. Native runs remain under
`TestResults/GameplayRegression/Runs/`.

Initial reproduction: **2 failed** in `20261114T095013556Z/results.xml`:
real scored goals published `continuation` rather than a goal reset state.
First implementation: **2 passed**, zero failed/skipped, in
`20261114T095244791Z/results.xml`, through actual celebration, reset, both
confirmations, conceding taker and completed kick-off pass, in both directions.
Expanded category: **6 passed**, zero failed/skipped, in
`20261114T095649920Z/results.xml`. Includes real goal/ball/player holds,
initial and post-goal setup moves, both directions, own-half and occupancy
restrictions, guest rejection, duplicate/stale/wrong-team commands and replica
admission. Compilation separately clean. The preceding expanded run at
`20261114T095521951Z/results.xml` had 4 passes and 2 fixture timing failures:
the hold measurement began before the current frame finished. The final tests
measure every token and ball after the first paused frame; no gameplay rule or
assertion was weakened.

Online setup has one team decision at a time: Home arranges and confirms,
then Away arranges and confirms; the team in possession selects the taker.
This serializes authority admission without changing placement or pass rules.
Selecting a teammate selects that player; online setup swaps are not yet exposed.
No wire schema change is needed: stages, legal commands, scores and existing
player/ball frames carry the flow. This remains the controlled six-token proof;
continuous full-match and paired acceptance are not implied.

Broader user-run verification when available:
`Assets/Tests/PlayMode/Features/KickoffGroundPass/`, fixture
`KickoffGroundPassPlayModeTests`, for opening choices, distance rules and saved
restart state; `Assets/Tests/PlayMode/LegacyCodexKickoffPlayModeTests.cs`, fixture
`LegacyCodexKickoffPlayModeTests`, for the prepared first pass. No whole suites.
