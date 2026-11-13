# Online result continuation holds

- [x] Reproduce unscaled result countdown escaping connection hold.
- [x] Freeze countdown, local dismissal and continuation across overlapping holds.
- [x] Preserve an existing result hold and remaining countdown after recovery.
- [x] Execute focused OnlineResultHold PlayMode cases in both directions.
- [x] Confirm offline result countdown behavior is preserved.

Paired/manual checks are deferred. Header and stoppage panels require their own
integration; this feature covers the common single-roll/ground-duel panel.

Compilation separately clean. Focused category OnlineResultHold / fixture
OnlineResultHoldPlayModeTests: **6 passed, 0 failed, 0 skipped** in
`TestResults/GameplayRegression/Runs/20261113T172129451Z/results.xml`.
The pre-fix reproduction failed all four overlapping-hold cases in
`TestResults/GameplayRegression/Runs/20261113T171940198Z/results.xml`.
The focused fixture uses owned authored panel assets and restores singleton,
scene, input subscriptions, random state and time through the existing fixture.

Affected focused checks: OnlineSharedPause PlayMode **13/13** in
`TestResults/GameplayRegression/Runs/20261113T172218697Z/results.xml` and
DecisionTimingPlayModeTests.DuelManualReview **4/4** in
`TestResults/GameplayRegression/Runs/20261113T172311811Z/results.xml`.
Zero failed/skipped. No whole suites ran. Broader user-run review if needed:
`Assets/Tests/PlayMode/Features/DecisionTiming/` (DecisionTiming category) for
other offline result/timing continuations, and
`Assets/Tests/PlayMode/Features/LongBallGoalkeeperPace/`
(LongBallGoalkeeperPace category) when the long-pass gateway is integrated.
