# Online high passes and landing control

- [x] Typed target preview/confirmation, attacker/defender preparation and owned rolls.
- [x] Accuracy/scatter, host observations and real high-pass ball-control continuation.
- [x] Freeze frame-based flight under overlapping holds; cancel stale flight callbacks.
- [x] Typed automatic deadlines for accuracy/scatter/control.
- [x] Dedicated OnlineHighPass focused tests in both directions.
- [x] Real Room manager coverage.
- [ ] Updated Windows builds.

Contested headers, header passes/shots, loose-ball spills, general goalkeeper box
moves, special restarts and native paired checks are subsequent integration work.
Goalkeeper rush move/stay/decline has six focused passing cases. Contested
header rolls and outcomes remain the next feature; these rush cases stop at
that real handoff. Defensive control and multiple-controller nomination still
need dedicated coverage.

Compilation separately clean. Category OnlineHighPass: **21 PlayMode passed,
zero failed/skipped** in
`TestResults/GameplayRegression/Runs/20261113T214426081Z/results.xml`.
Fixtures: OnlineHighPassPlayModeTests (19) and OnlineRoomHighPassPlayModeTests (2).
Coverage includes accuracy/scatter/control, typed automatic deadlines and late
manual input, mandatory single/multiple-attacker preparation, defending movement,
keeper move/stay/decline, stale occupancy, guest rejection, overlapping holds in
frame-based flight, cancelled flight, bounded codec/replica observations and
actual Room manager continuation.

The Room run reproduced a dependency issue in
`TestResults/GameplayRegression/Runs/20261113T212853734Z/results.xml` (16 passed,
2 failed). ShotManager.headerManager is a lazy optional cache in Room. The
adapter now uses the high-pass manager's existing authoritative header reference,
with a fallback for older wiring. The two failed Room cases then passed in
`TestResults/GameplayRegression/Runs/20261113T213312506Z/results.xml` before the
final 21-case green run. No scene asset was rewritten for this dependency fix.

Existing rules are preserved: ball control records a separate resolved action
after the aerial pass, and a sole eligible attacker automatically prepares an
empty target. The tests assert those real phase transitions and counts.

Affected checks: OnlineManualControls **7/7** in
`TestResults/GameplayRegression/Runs/20261113T213918584Z/results.xml`;
LegacyHighPassBoxSequencePlayModeTests.BoxMoveThenRushThenFinalThirdThenHeader
**1/1** in `TestResults/GameplayRegression/Runs/20261113T214249988Z/results.xml`.
The latter first failed at kickoff (213948456Z); its aerial fixture now enters
open play through BroadcastSafeEndofMovementPhase before requesting a high pass.
The kickoff ground-pass-only rule and downstream assertions remain intact.
No full suites ran. Paired acceptance remains deferred.

Broader user-run checks if wanted: category DecisionTiming in
`Assets/Tests/PlayMode/Features/DecisionTiming/` for other timing/interruptions;
legacy aerial/header fixtures in `Assets/Tests/PlayMode/` for remaining offline
header and restart sequences. Run only reported failing cases afterward.
