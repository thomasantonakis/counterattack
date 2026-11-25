# Confirmed-goal audio

- [x] Inspect the real score, own-goal and save-restoration entry points.
- [x] User confirmed MissedChance recordings for the conceding stand.
- [x] Reproduce missing playback through the real confirmed-score entry.
- [x] Emit an optional, identifiable presentation cue once per live goal.
- [x] Subscribe/unsubscribe the existing Room audio director; keep preferences local.
- [x] Verify both scoring teams, both directions, own goals, score/state preservation,
  duplicate delivery, mute/pause, missing audio and restore behavior.
- [x] Compile cleanly and execute only focused GameplayGoalAudio fixtures.
- [x] Record native XML and actual counts; hand broader checks to the user.

No UI/layout changes or full gameplay suites. Silent owned playback tests are
separate from listening to the purchased clips.

2026-11-10: Editor compilation clean. Initial reproduction: 1 failed as expected
(confirmed score had no reaction clip), native XML:
`TestResults/GameplayRegression/Runs/20261110T153024915Z/results.xml`.
After integration: **14 passed, 0 failed, 0 skipped**, fixture
`GameplayGoalAudioPlayModeTests`, category `GameplayGoalAudio`, native XML:
`TestResults/GameplayRegression/Runs/20261110T153202490Z/results.xml`.
Both runs retain sibling progress.jsonl. Cases exercise real GameLog normal/own-goal
entries and state restoration; they do not execute a complete physical shot or
goal celebration/kickoff flow. Confirmed-score tests preserve possession, game
state, timing-roll sequence and Unity RNG; bed gain continues and chants fade.
Room returned stopped, clean, with the same 11 authored audio sources. No UI edits.

Follow-up mix update: 70/30 shares, with both goal reactions filling their stand's
remaining gain above Walla and fading chants. The 0.75 overall crowd trim remains.
Expanded fixture: **16 passed, 0 failed, 0 skipped**, including both goal sides
during Walla crossfades and mute/unmute. Native artifact:
`TestResults/GameplayRegression/Runs/20261110T155403265Z/results.xml`.
Current Room audio source count is 16 after the separate incident batch; see
`../Incidents/TASKS.md` for its full results and UI preservation evidence.

Listening acceptance remains: score for each side and hear the correct cheerful/
disappointed combination, continuous Walla and fading chants at suitable levels.
Broader optional user checks: `Assets/Tests/PlayMode/LegacyOwnGoalPlayModeTests.cs`
(`LegacyOwnGoalPlayModeTests`) and `Assets/Tests/PlayMode/LegacyShootingBranchPlayModeTests.cs`
(`LegacyShootingBranchPlayModeTests`) exercise physical goal and continuation paths
outside this score-notification batch. No broader or whole suites executed.
