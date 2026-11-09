# Decision timing PlayMode checklist

- [x] Real target input, invalid-click stability, expiry and possession exclusion.
- [x] Whole-team movement expiry and next-phase release.
- [x] First Time Pass/High Pass movement, header, snapshot, goalkeeper and final-third ownership.
- [x] Set-piece and penalty setup excluded while the built-in match clock is stopped.
- [x] Automatic mandatory rolls, pause, rigged accuracy, stale requests and real result holds.
- [x] Both attacking directions, owned fixture cleanup, serial native runs and durable XML.

Use fixture `DecisionTimingPlayModeTests` or category `DecisionTiming`.
See `Assets/Editor/FeatureTests/DecisionTiming/Verification.md` for exact counts,
result paths, limitations and broader user-run checks.

- [x] Tackle/nutmeg owner, decline, expiry, reposition, turnover and next-phase timing:
  `DecisionTimingPlayModeTests.Duel`, 16/16 passed, run `20261109T102653052Z`.
- [x] Handoff acceptance: complete jumper-list timing, manual holds/inspection overlays,
  and actual gameplay save/load restoration (see Verification.md).

- [x] JumperSelection and JumperContinuation: two-player lists, reselection, expiry,
  select-all, defense forfeit/reopening, header-at-goal offer/decline and real rolls.
- [x] ManualHeaderResult: indefinite hold beyond configured duration and real resumption.
- [x] InspectionTiming: physical input consumed, timing/automatic roll continue, pause suspends.
- [x] GameplayTimingRestore and GameplayRollRestore: actual MatchManager apply/rebuild,
  matching/expired/old choices, unsupported movement cleanup, pending roll and new-match reset.
- [x] Resolve Rules behavior: live inspection shortcut versus pre-existing pause; CPU and automatic rolls continue through live Rules.
- [x] CPU/fallback dispatch races and roll-sequence cache invalidation.
- [x] All 14 event types preempt the separate timing display without temporal overlap.
- [x] Jumper overflow/invalid clicks/optional GK and manual duel/expired-parent review cases.
- [x] StoppageManualReview, FileTimingRestore and InterruptedTimingRestore: 12/12
  passed after resolving the external-scene modal and Editor-folder host fixture issue.
- [x] Complete partial rows in Verification.md's original acceptance audit:
  GK/shot follow-up families, actual target cancellation/reopening and nonblocking results.

- [x] TargetReopening (16), KeeperFollowup (6), ShotFollowup (4), FoulFollowup (2),
  NonblockingScatter (8), ShootoutReview (6): 42 new cases passed.
- [x] Affected DuelManualReview (4) and RealAccuracyRoll (4) regressions passed.
- [x] Reproduce nonblocking scatter's incorrect result hold, fix it, verify recovery
  rolls/results reclaim their holds and blocking/manual results remain unchanged.

Final remaining-test batch: 50 passed, zero failed/skipped; compilation clean.
Broader user-run checks remain unexecuted; see Verification.md for native artifacts.

This continuation batch passed 22 cases; native evidence and compilation are recorded
separately in Verification.md. Use each named method as a single explicit filter.
