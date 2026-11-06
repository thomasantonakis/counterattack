# Session handoff — 2026-11-06

## Latest user instructions

Stop testing and hand off to a new session. **Do not run the full suites again.**
The next session is to discuss the gameplay review. Do not automatically restart
migration or tests merely because an older goal remains recorded.

The user resolved own-goal consistency: if a rushing GK or an outfield defender ties
the best attacking aerial score and the resulting defensive scatter enters their
own goal, it is that player's own goal. Blocked-shot and tied-save scatters follow
the same attribution. No-deflection failed saves remain attacker goals. Do not
reopen this as an unanswered question or change scoring code to award the attacker.

## Completed and pushed

- Production fixes: `4cb471c` (six files), reviewed in GameplayReview.md.
- Native fixtures/adapters and existing migration documentation are already in
  `21b0531`, `5b754cc`, `c21c5f5`, `12b5694`.
- Requested review, future-session prompt and mandatory future gameplay regression
  policy: `0d2dcb0`, pushed to origin/master.
- AGENTS.md now requires new meaningful native coverage for every gameplay code
  change, execution/inspection, affected regressions and separate compile/results
  reporting. Old instructions to have the user press Play/comment out tests were
  replaced with the native filter workflow.

## Actual validation, with limits

- Complete PlayMode: **362 passed / 0 failed / 0 skipped**, native XML:
  `TestResults/GameplayRegression/Runs/20261106T172436176Z/results.xml`.
- Last complete EditMode: **747 / 0 / 0**, native XML:
  `TestResults/GameplayRegression/Runs/20261106T144426744Z/results.xml`.
- Current discovery:748 EditMode +362 PlayMode =1110. The newer stats-preview
  EditMode test has passed in isolation, but a complete748-case run has not occurred.
- Clean live compilation after final test-only edits:0errors/0warnings. This is
  not a test verdict.
- Full PlayMode XML logs explicitly show Kuzmic own goals for generated header02
  (tied save) and09 (rushing aerial tie), and Soares own goal for shooting17
  (outfield block). These passed. Explicit personal/scorer-record assertions for
  the newly clarified rule are still a follow-up, not already written/executed.

## Filtered run ended for handoff

An additional **filtered `Shot` PlayMode audit**, not another full suite, was already
running when the user requested handoff:
`TestResults/GameplayRegression/Runs/20261106T214114486Z/`.
Its durable progress contains **15 passed and1 failed** (16 completed), last case
`LegacyFreeKickShotPlayModeTests.FreeKickShotBranch_10`. There is **no results.xml**,
so this is an incomplete/cancelled run, not a full16-test verdict. Do not count
unexecuted tests as passed or skipped.

The one failure is a new test-only assertion in FreeKickShotBranch_01:
`Final Third owner: Expected True, But was False`.
The gameplay had reached WaitingForGoalKickFinalThirds, but
FinalThirdManager.TriggerOobGoalKickFinalThirdsAfterGoalkeeperReady first waits for
the keeper to reach the goal-kick spot before activating Final Thirds. The new
assertion assumed readiness too early. Diagnose/correct the wait before asserting
that stable state; do not alter gameplay or silently remove the state assertion.
No production gameplay code was changed in this final audit.

Pipeline `cancel_tests` returned `cancelled`. Subsequent `editor_status` confirmed
**ready, PlayMode stopped, compiling false**. The temporary InitTestScene files are
no longer in git status. Before any future requested execution, recheck native UTF
and Pipeline pending state; do not start a competing run. Do not launch Unity.

## Uncommitted test-only work to preserve and review

- `Assets/Editor/FrameworkAuthoredPanelSourceFingerprint.cs` and `.meta` (new).
- `Assets/Editor/FrameworkAuthoredPanelAssets.cs` and
  `Assets/Editor/LegacyAuthoredPresentationFixture.cs`: use a hash of the three
  authored panel hierarchies rather than the entire Room scene. This avoids
  coupling fixtures to unrelated designer scene changes. Scoped hash is
  `93FCA01594AC4E8FF35D07BD0A32C2438CB54DD13E9F84A0F5BD909721EC39AB`.
- `Assets/Tests/Fixtures/RoomResolutionPanels.source.sha256.txt`: that scoped hash.
- `Assets/Editor/LegacyShootingBranchesFixture.cs`: final port of original common
  manager-owner assertions (including unrelated action owners and shot availability
  during restart). Includes the premature off-target Final Third assertion above.
- `Assets/Editor/LegacyHeaderGoalFixture.cs`: restore restart availability assertion
  immediately after corner Final Thirds as well as at final kicker selection.

These compile and the first15 affected cases passed, but this whole patch is not
fully validated or committed. Preserve it as work in progress. Room scene SHA256
remains `BE602B8B79E96EEB56873D17EE1948F557B85C237752250112EB22579703CD01`.

## Exact migration cleanup still outstanding

All147 originals have native replacements and the complete362-case suite passed.
However, only145 originals have been removed from the runtime source. Two remain:
Scenario_034_Shooting_Branch (31 cases), Scenario_038_HeaderAtGoal_Branch (13 cases).
They were deliberately retained until the last common-state assertion audit was
complete. Do not claim the runtime runner is already deleted or the audit is green.

The old helper assumed exclusive Shot/LooseBall activation during scatter. Current
ShotManager awaits ResolveLooseBall before ResetShotProcess (the reset ordering
predates this migration); parent action activation can coexist with child input
ownership. Test that actual lifecycle and all unrelated inactive managers. Document
that expectation correction, rather than changing game lifecycle to fit the old
helper. Off-target goal-kick activation also needs its actual readiness wait.

If the user later requests completion: finish that focused audit and clarified
own-goal assertions, then retire the two methods and remove DummyLoader's Tester
component plus GameTestScenarioRunner.cs/.meta. A prepared, **unexecuted** Editor
snippet is `/private/tmp/retire_gameplay_runner.cs`; inspect it before use. It guards
Room hash/dirty state and removes only the obsolete DummyLoader runner. The snapshot
helper has already moved into Assets/Editor/LegacyGameStatusSnapshot.cs and no native
test calls a legacy scenario. Respect the latest instruction against full-suite
reruns; do not run one automatically in the discussion session.

Read GameplayReview.md for six production changes, expected wider effects, baseline
keeper changes and remaining policy/API review items. FutureSessionPrompt.md contains
the copyable discussion prompt. MorningReview.md retains historical investigations.
