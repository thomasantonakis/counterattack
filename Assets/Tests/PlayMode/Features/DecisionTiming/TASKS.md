# Decision timing PlayMode checklist

## Seven reported issues

- [x] Investigate missing MP slot first using active Editor log; reproduce two advances.
- [x] Fix exhausted snapshot handoff; real shot/tied-save/scatter counts one mover.
- [x] Keep F3 countdown/budget during actual animation, both teams and directions.
- [x] Render seconds with " and omit snapshot when unavailable during dribbling.
- [x] Display event and decision panels simultaneously; preserve disjoint saved layout.
- [x] Suppress next-slot pulse until current mover releases tackle/roll ownership.
- [x] Offer normal-penalty keeper roll immediately, preserving shootout/tie behavior.
- [x] 17 EditMode + 48 distinct PlayMode cases passed; compilation clean separately.

New fixture: DecisionTimingReportedIssuesPlayModeTests, category
DecisionTimingReportedIssues. Use explicit method filters for subsequent changes.

## Immediate-display and mid-dribble correction

- [x] Preserve user-saved timing/event panel positions without authoring overrides.
- [x] Set prominent CountdownDisplaySpentFraction to 0f (0.5f restores halfway).
- [x] Reproduce/fix move/X missing its own budget outside snapshot range.
- [x] Add DribblePaceChoice: 12 cases, all difficulties/directions and inside/outside box.
- [x] Verify concurrent charging, priority, invalid input, expiry, next movement/X.
- [x] 17 EditMode + 44 PlayMode distinct passes; compile clean separately.

## Concurrent-choice and snapshot-panel correction

- [x] Own 10/10/5 choice allowance plus owning MP time for snapshot/tackle/nutmeg.
- [x] Halfway choice priority, MP fallback and resumption; opponent budget excluded.
- [x] Independent expiry, retained expired MP, pause and service snapshot restoration.
- [x] Real N/T challenges and continuation; real S declaration through blocker move.
- [x] Completed dribbler retained, next slot non-pulsing during snapshot resolution.
- [x] 38 EditMode + 45 PlayMode distinct cases passed across final/rerun evidence.
- [x] Compile clean separately; no full suites or ordinary gameplay.

## Latest live-clock revision

- [x] Diff 1–2 shared preview allowance through switching/expiry and real commitment.
- [x] Diff 3 immediate commitment on P/C/L/F/S and M; fix Long Ball commitment defect.
- [x] Roll/automatic review live clock; manual Enter/pause freeze; CPU races preserved.
- [x] Actual MP animation consumes phase time through expiry; snapshot uses MP budget.
- [x] Halfway countdown boundary; announcements retain priority across all 14 kinds.
- [x] Scene panels authored/saved under HOME/AWAY; no loaded-scene disk rewrite.
- [x] Supported save restoration and unsupported committed-action stale-timing cleanup.
- [x] 33 EditMode + 136 PlayMode passed, 0 failed/skipped; compile clean separately.
- [ ] Broader user-run regression suites and Load Game browser/bootstrap (not run here).

Run new fixtures `DecisionTimingLiveClockEditorTests` (EditMode) and
`DecisionTimingLiveClockPlayModeTests` (PlayMode), category `DecisionTimingLiveClock`.
Exact affected filters/native artifacts are in Verification.md. Historical checklist
below predates the latest roll/preview rules; current policy is recorded above.


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
