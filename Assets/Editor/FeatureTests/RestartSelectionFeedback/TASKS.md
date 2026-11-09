# Restart selection feedback

- [x] Audit throw-now and target timing in all three difficulties, without changing clock rules.
- [x] Verify MP budget starts on commitment after a header; automatic MP commits immediately.
- [x] Yellow valid corner high-pass targets at difficulties 1–2, orange hover/preview, no assistance at 3.
- [x] Orange valid F3 hover after selection; restore yellow and clear on invalid hover/forfeit.
- [x] Compile in the open Editor and run only explicit affected filters; retain native results.

Fixtures: RestartSelectionFeedbackPlayModeTests (category RestartSelectionFeedback).
Use the four named method filters independently. Broader gameplay suites are not authorized.

## Results (2026-11-09)

Compilation separately: live Editor recompile succeeded; actual
EditorUtility.scriptCompilationFailed is false; no compiler errors after latest reload.
Room remains loaded, clean and stopped. No full suites or ordinary gameplay ran.

Native results/progress under TestResults/GameplayRegression/Runs/:

| Run | Filter | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| 20261109T191205816Z | RestartSelectionFeedbackPlayModeTests, reproduction | 14 | 10 | 0 |
| 20261109T191413959Z | RestartSelectionFeedbackPlayModeTests, final | 24 | 0 | 0 |
| 20261109T191530551Z | LegacyHighPassSelectionPlayModeTests, outdated kickoff setup | 1 | 2 | 0 |
| 20261109T191725347Z | LegacyHighPassSelectionPlayModeTests, legal completed-MP setup | 3 | 0 | 0 |
| 20261109T191742936Z | DecisionTimingReportedIssuesPlayModeTests.FinalThirdMotion | 8 | 0 | 0 |

Final distinct scope: 35 PlayMode passed, zero remaining failed/skipped. The four
corner failures reproduced missing target-map presentation; six F3 cases reproduced
the missing hover handler. A test-only NUnit collection assertion compile error was
fixed before execution. Existing high-pass easy/hard tests tried C at standard-pass-
only kickoff. They now use the real completed-movement entry point with action
counting/F3 disabled for setup, preserving their real selection/confirmation/movement
assertions. No gameplay rule or passing assertion was weakened.

## Timing answers verified without changing rules

- Throw-now type choice and header target choice retain WaitingForThrowInTaker.
  The built-in match-clock stop means no active decision timer.
- Feet target previews at difficulties 1–2 also retain that state until commitment.
  Difficulty 3 commits immediately on P, enters StandardPass and gets its target timer.
- After BroadcastHeaderCompleted, a chosen MP at difficulties 1–2 initially uses
  SelectNextAction while uncommitted. Commitment starts its full MP budget.
  Difficulty 3 commits on M; automatically started MPs commit immediately.
- An intervening F3 gets its own active allowance; it suspends the underlying MP
  allowance through the existing registration priority. Built-in stops remain exempt.

The two visual fixes change presentation only: corner HP target eligibility and
confirmation rules are preserved; F3 hover uses the already filtered movement list.
Yellow corner targets apply at difficulties 1–2 only, and orange hover/selected
feedback restores appropriately. F3 orange hover is available at all difficulties,
clears on invalid hover/selection change/forfeit/end/disable, and preserves the
chosen destination during motion. Designer-authored scene layouts are untouched.

Broader checks, if wanted, remain user-run: Assets/Tests/PlayMode/
LegacyHighPassMovementPlayModeTests for later high-pass continuations and
LegacyRestartPreparationPlayModeTests for complete corner setup. This focused
result is not a complete gameplay regression verdict.
