# F3 input ownership and reposition hover

- [x] Reproduce recovery → opposite F3 exposing the new attack's actions.
- [x] Reserve ordinary action availability through all F3 phases; preserve continuation afterward.
- [x] Stale ground-pass click/hover/key handlers yield to F3.
- [x] Reposition hover stays cleared through travel and nested goalkeeper decisions.
- [x] Verify real deterministic movement/recovery/tackle rolls and both directions/teams.
- [x] Compile cleanly; run only F3RepositionInputPlayModeTests and inspect native XML.

Owned fixture reuses GoalkeeperBoxEntryFixture cleanup and suppresses the legacy
runner through the native Framework gate. No live save is loaded or written.
## Verification — 2026-11-17

- Final compilation: zero errors/warnings. Runtime compilation also completed with
  zero errors and six pre-existing obsolete API warnings.
- Final native PlayMode run: **12 passed, 0 failed, 0 skipped**.
  `TestResults/GameplayRegression/Runs/20261117T173625893Z/results.xml`
  and per-case `progress.jsonl` are retained.
- Before the fix, corrected recovery cases failed the F3 availability assertion
  for all four team/direction combinations in `20261117T173149510Z/results.xml`.
  Reposition hover restored destination highlights in both directions in
  `20261117T173007164Z/results.xml`. That initial run's recovery setup omitted the
  nutmeg decline and its stale-preview setup selected a player in the wrong half
  of a double-F3 sequence; these were fixture defects and were corrected.
- Intermediate `20261117T173404297Z/results.xml` passed all four no-F3 boundaries
  and the fixed ownership assertions in the recovery/reposition cases, then found
  fixture continuation mistakes: a contested pass needed interception input and
  a GK destination click completed the decision before the test tried to skip it.
  The final setup uses a clear pass destination and an invalid GK destination;
  completion assertions remain intact.
- Coverage: real selected dribble, declined nutmeg, rigged successful steal,
  recovery ownership/statistics, rejected P/M/L/C/F and direct UI/AI entry points,
  F3 selection/movement, preserved hover highlights, normal pass selection and
  confirmation afterward, stale preview isolation, double-F3 reservation,
  midfield/no-eligible-player boundaries, real rigged tackle, reposition selection,
  hover during travel, nested GK skip and final player/ball positions.
- No gameplay dice probabilities, movement distances, save schemas, scene layout,
  post-match UI or legacy cases were changed.

## Existing regressions executed at the user's request

All XML paths below are under `TestResults/GameplayRegression/Runs/`.

| Filter | Passed | XML run directory |
| --- | ---: | --- |
| RestartSelectionFeedbackPlayModeTests.FinalThirdHover | 6 | 20261117T175329731Z |
| LegacyMovementRecoveryAuditsPlayModeTests | 3 | 20261117T175512192Z |
| tackle_reposition (GoalkeeperBoxEntryPlayModeTests cases) | 4 | 20261117T175716461Z |
| lost_foot (4 box-entry and 2 legacy duel-presentation cases) | 6 | 20261117T180258857Z |
| LegacyHighPassBoxSequencePlayModeTests | 1 | 20261117T180535709Z |

**20 existing regressions plus 12 new cases passed: 32 total, zero failed/skipped
in final runs.** This includes move/skip GK choices, both directions, all three
F3 hover difficulties and high-pass → GK move/rush → F3 → header continuation.

Initial high-pass sequence run `20261117T180411933Z` failed at C before any F3:
LoadLegacyRoster calls StartMatch, whose existing normal-kickoff rule permits only
a ground pass. The fixture now enters ordinary live play through
BroadcastSafeEndofMovementPhase(false, false) before C, keeping all original
high-pass/GK/F3/header assertions. No kickoff gameplay rule was changed.

Remaining unexecuted coverage: other GoalkeeperBoxEntryPlayModeTests cases,
other RestartSelectionFeedbackPlayModeTests methods, whole suites and hardware
mouse/raycast acceptance. No complete suites were run.
