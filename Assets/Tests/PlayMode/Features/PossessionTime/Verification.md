# Possession time

- [x] Switch regulation time immediately on recovery or a restart possession change, before the next commitment.
- [x] Regulation uses actual game-clock advancement, fast-clock scaling and the half limit.
- [x] Pauses and restart preparation add no possession time.
- [x] Every consumed extra action credits 60 seconds to its starter; continuations add none.
- [x] Save team time and the current action starter; preserve time when rebuilding player totals.
- [x] Display timed shares; old saves start with zero recorded time and display 50–50.
- [x] Compile in the open Editor and execute the focused fixtures.

EditMode: `PossessionTimeEditorTests` in `Assets/Editor/FeatureTests/PossessionTime/`.
PlayMode: `PossessionTimePlayModeTests` in this folder (both teams and attacking directions).
Native result artifacts: `TestResults/GameplayRegression/Runs/`.

Verified 2026-11-07: open-Editor recompilation completed with zero errors/warnings.
Final focused runs after the recovery/restart correction: 4 EditMode passed,
12 PlayMode passed; zero failed/skipped.
Native XML and per-case progress:

- `20261107T144309783Z/results.xml` reproduces the goal-kick bug before the fix: both directions failed because five seconds after the award still went to the shooting team.
- `20261107T144404730Z/results.xml` (4 EditMode passed, including actual runtime snapshot creation/restoration of the current regulation team and the original extra-action starter).
- `20261107T144432496Z/results.xml` (12 PlayMode passed, including immediate recovery and real off-target goal-kick awards in both directions).

Broader coverage is left for the user: `LegacyRestartClockPlayModeTests` in
`Assets/Tests/PlayMode/LegacyRestartClockPlayModeTests.cs` checks the existing
restart clock gates. No whole suite was executed.
