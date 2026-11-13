# Online Final Third

- [x] Typed player selection, destinations, forfeit and goalkeeper continuation choices.
- [x] Final Third priority before ordinary movement, with authenticated team ownership.
- [x] Move and forfeit continuations after real long passes, both directions.
- [x] Connection-held movement, guest guards and duplicate receipt protection.
- [x] Actual Room manager coverage and focused OnlineFinalThird execution.

Goal-kick/save-and-hold mandatory-clearance and keeper-choice branches reuse
existing rules but still need focused native online integration coverage.
High-pass/shot/restart continuations and paired checks remain pending.

Live Editor compilation clean. Focused category OnlineFinalThird: **6 PlayMode
passed, zero failed/skipped** in
`TestResults/GameplayRegression/Runs/20261113T180720716Z/results.xml`.
Fixtures: OnlineFinalThirdPlayModeTests (4) and
OnlineRoomFinalThirdPlayModeTests (2). Actual long-pass entry, both-team
ownership, real moves/forfeits, duplicate receipts, guest guards, held movement
and return to ordinary movement are covered in both attacking directions.
Affected OnlineMovement PlayMode **7/7** passed in
`TestResults/GameplayRegression/Runs/20261113T180800077Z/results.xml`.
No full suites ran.

The first run reproduced a selection/continuation race (4 failed, 2 passed),
retained in `TestResults/GameplayRegression/Runs/20261113T180413895Z/results.xml`.
The existing coroutine must observe selection before a destination can clear
it. A read-only acknowledgement now controls when the authority exposes target
commands; immediate premature destination execution is explicitly rejected.
Offline input and movement rules are unchanged.

Unexecuted branches: the goalkeeper drop/kick/goal-kick-pass commands and special
mandatory clearances need later goal-kick/save-and-hold integration tests.
Broader user-run offline check if wanted: fixture
DecisionTimingReportedIssuesPlayModeTests.FinalThirdMotion in
`Assets/Tests/PlayMode/Features/DecisionTiming/`; legacy aerial sequence fixture
LegacyHighPassBoxSequencePlayModeTests in
`Assets/Tests/PlayMode/LegacyHighPassBoxSequencePlayModeTests.cs`.
Paired native acceptance remains deferred.

Windows Room export `build_e4c872018cdf` succeeded (eight warnings and the
known missing-vswhere.exe diagnostic, no script compile errors). Existing
packaged ground/pause/recovery check passed in
`TestResults/GameplayRegression/Runs/20261113T181112789Z-Room-local/result.json`;
build report retained alongside. Final Third-specific evidence is the focused
Room framework coverage above, not a paired native acceptance claim.
