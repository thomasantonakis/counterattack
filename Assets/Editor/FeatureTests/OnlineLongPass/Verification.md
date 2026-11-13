# Online long pass

- [x] Typed authority start, preview/confirm, accuracy and scatter.
- [x] Mandatory defending goalkeeper Pace move/claim/decline.
- [x] Real landing and continuation with immutable roll observations.
- [x] Reject wrong teams, stale targets, guest execution and held commands.
- [x] Own authored semantic action dropdown and execute button in both proofs.
- [x] Focused OnlineLongPass PlayMode verification in both directions.
- [x] Affected ground/manual exchange checks.
- [ ] Updated Windows export/local check.

Separate penalty-box free goalkeeper moves, header/loose-ball continuations and
full-match lifecycle remain further integration tasks. Paired checks deferred.

Live Editor compilation clean, separately from tests. Focused category
OnlineLongPass: **16 passed, 0 failed, 0 skipped** in
`TestResults/GameplayRegression/Runs/20261113T174237625Z/results.xml`.
Fixtures: OnlineLongPassPlayModeTests (13), OnlineActionControlsPlayModeTests (1),
OnlineRoomLongPassPlayModeTests (2). Real selection/confirmation, deterministic
host dice, accuracy/scatter, defending keeper move/claim/decline, successful and
failed landing interception, duplicate receipts, stale occupancy, wrong teams,
connection holds in flight/at keeper response and inert guest controls are covered.
Target publications also round-trip through bounded v5 framing.

The flight check initially exposed that the real long-ball coroutine did not set
Ball.isMoving. Its own versioned flight observation now publishes active flight,
selected target and presentation height without changing offline movement rules.
The initial fixture also incorrectly expected optional movement availability;
existing rules automatically commit movement for an aerial ball in space. The
correct continuation asserts active movement and exactly one completed action.

Affected focused final evidence: OnlineManualControls PlayMode 7/7 in
`TestResults/GameplayRegression/Runs/20261113T174349751Z/results.xml`;
OnlineRollResults EditMode 11/11 (174100679Z), OnlineRoomAction EditMode 6/6
(174108374Z), OnlineGroundPassNetwork EditMode 13/13 (174116274Z),
OnlineSharedPause EditMode 11/11 (174124232Z), all under the same
`TestResults/GameplayRegression/Runs/20261113T` prefix and `/results.xml` suffix.
Zero failures/skips. No full suites ran.

Broader user-run coverage if wanted: category LongBallGoalkeeperPace in
`Assets/Tests/PlayMode/Features/LongBallGoalkeeperPace/` for offline aerial recovery;
category DecisionTiming in `Assets/Tests/PlayMode/Features/DecisionTiming/` for
other timers and continuations. Keep paired Mac acceptance for the user.
